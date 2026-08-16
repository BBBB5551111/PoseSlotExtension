#if UNITY_EDITOR
using System;
using System.Linq;

namespace PoseSlotExtensionVRCFuryStable.Editor
{
    /// <summary>
    /// Verified, intentionally fixed Pose Slot Extension contract.
    /// Do not derive these values from BUDDYWORKS assets or scene state.
    /// </summary>
    internal static class PoseSlotFixedSpecification
    {
        internal const int ContractVersion = 2;
        internal const int SlotCount = 50;
        internal const int LoadCommandOffset = 100;
        internal const int ControlsPerRange = 7;

        // Verified against the current BUDDYWORKS Action controller.  A mismatch
        // means the upstream package changed and the generated copy must not be
        // patched or uploaded without a new investigation.
        internal const int ExpectedGroundedReleaseTransitions = 2;
        internal const int ExpectedGoEmoteReleaseTransitions = 1;
        internal const int ExpectedFavoriteShowcaseReleaseTransitions = 3;
        internal const int ExpectedEyelookMigrationReleaseTransitions = 2;
        internal const int ExpectedOtherReleaseTransitions = 0;

        internal const string CommandParameter = "PSE/Command";
        internal const string PoseSetParameter = "PE/Set";
        internal const string PoseFloatParameter = "PE/Float";

        // Seven independent folders. VRChat supplies Back automatically;
        // no custom Next button is used. The last folder holds eight slots.
        internal static readonly (int Start, int End)[] MenuRanges =
        {
            (1, 7),
            (8, 14),
            (15, 21),
            (22, 28),
            (29, 35),
            (36, 42),
            (43, 50)
        };

        internal static int SaveCommand(int slot) => slot;
        internal static int LoadCommand(int slot) => LoadCommandOffset + slot;
        internal static string SnapshotSet(int slot) => $"PSE/{slot:00}/Set";
        internal static string SnapshotPose(int slot) => $"PSE/{slot:00}/Pose";
        internal static string SnapshotValid(int slot) => $"PSE/{slot:00}/Valid";

        internal static void ValidateOrThrow()
        {
            var slots = MenuRanges.SelectMany(range => Enumerable.Range(
                range.Start, range.End - range.Start + 1)).ToArray();
            if (!slots.SequenceEqual(Enumerable.Range(1, SlotCount)))
                throw new InvalidOperationException("Fixed menu ranges must cover slots 01-50 exactly once.");
            if (MenuRanges.Any(range => range.Start > range.End || range.End - range.Start + 1 > 8))
                throw new InvalidOperationException("Every fixed leaf menu must contain at most eight controls.");
            if (SaveCommand(1) <= 0 || SaveCommand(SlotCount) >= LoadCommand(1))
                throw new InvalidOperationException("Save and Load command ranges overlap.");
            if (LoadCommand(SlotCount) > 255)
                throw new InvalidOperationException("The fixed Load command range exceeds the Int menu value limit.");
        }
    }
}
#endif
