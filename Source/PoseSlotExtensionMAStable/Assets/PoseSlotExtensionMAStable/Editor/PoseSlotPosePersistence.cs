#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace PoseSlotExtensionMAStable.Editor
{
    /// <summary>
    /// Removes BUDDYWORKS' automatic pose-release routes from a generated copy of
    /// its Action controller. The package controller itself is never modified;
    /// explicit PE/Reset remains the sole route that clears an active pose.
    /// </summary>
    internal static class PoseSlotPosePersistence
    {
        internal const string BuddyActionSourcePath =
            "Packages/wtf.buddyworks.posesextension/Data/Poses Extension - Action.controller";
        internal const string PersistentActionPath =
            "Assets/PoseSlotExtensionMAStable/Generated/Animator/BUDDYWORKS Poses Extension - Action [Persistent].controller";

        internal readonly struct PatchResult
        {
            internal readonly int GroundedTransitions;
            internal readonly int GoEmoteTransitions;
            internal readonly int FavoriteShowcaseTransitions;
            internal readonly int EyelookMigrationTransitions;
            internal readonly int OtherTransitions;
            internal readonly string[] OtherTransitionDetails;

            internal PatchResult(int groundedTransitions, int goEmoteTransitions,
                int favoriteShowcaseTransitions, int eyelookMigrationTransitions,
                int otherTransitions, string[] otherTransitionDetails)
            {
                GroundedTransitions = groundedTransitions;
                GoEmoteTransitions = goEmoteTransitions;
                FavoriteShowcaseTransitions = favoriteShowcaseTransitions;
                EyelookMigrationTransitions = eyelookMigrationTransitions;
                OtherTransitions = otherTransitions;
                OtherTransitionDetails = otherTransitionDetails ?? Array.Empty<string>();
            }

            internal int Total => GroundedTransitions + GoEmoteTransitions +
                                  FavoriteShowcaseTransitions + EyelookMigrationTransitions +
                                  OtherTransitions;
        }

        internal static PatchResult RemoveAutomaticReleaseTransitions(AnimatorController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            var grounded = 0;
            var goEmote = 0;
            var favoriteShowcase = 0;
            var eyelookMigration = 0;
            var other = 0;
            var otherDetails = new List<string>();
            foreach (var layer in controller.layers)
                PatchMachine(layer.stateMachine, ref grounded, ref goEmote,
                    ref favoriteShowcase, ref eyelookMigration, ref other, otherDetails);
            return new PatchResult(grounded, goEmote, favoriteShowcase, eyelookMigration, other,
                otherDetails.ToArray());
        }

        internal static int CountAutomaticReleaseTransitions(AnimatorController controller)
        {
            if (controller == null) return 0;
            return EnumerateTransitions(controller)
                .Count(entry => IsAutomaticRelease(entry.transition));
        }

        internal static int CountExplicitResetTransitions(AnimatorController controller)
        {
            if (controller == null) return 0;
            return EnumerateTransitions(controller).Count(entry =>
                WritesPoseSetZero(entry.transition.destinationState) &&
                HasCondition(entry.transition, AnimatorConditionMode.If, 0, "PE/Reset"));
        }

        private static void PatchMachine(AnimatorStateMachine machine, ref int grounded, ref int goEmote,
            ref int favoriteShowcase, ref int eyelookMigration, ref int other,
            List<string> otherDetails)
        {
            foreach (var child in machine.states)
            {
                foreach (var transition in child.state.transitions.ToArray())
                {
                    if (!IsAutomaticRelease(transition)) continue;
                    Classify(child.state, transition, ref grounded, ref goEmote,
                        ref favoriteShowcase, ref eyelookMigration, ref other, otherDetails);
                    child.state.RemoveTransition(transition);
                }
            }

            foreach (var transition in machine.anyStateTransitions.ToArray())
            {
                if (!IsAutomaticRelease(transition)) continue;
                Classify(null, transition, ref grounded, ref goEmote,
                    ref favoriteShowcase, ref eyelookMigration, ref other, otherDetails);
                machine.RemoveAnyStateTransition(transition);
            }

            foreach (var childMachine in machine.stateMachines)
                PatchMachine(childMachine.stateMachine, ref grounded, ref goEmote,
                    ref favoriteShowcase, ref eyelookMigration, ref other, otherDetails);
        }

        private static void Classify(AnimatorState source, AnimatorStateTransition transition,
            ref int grounded, ref int goEmote, ref int favoriteShowcase,
            ref int eyelookMigration, ref int other, List<string> otherDetails)
        {
            if (IsGroundedRelease(transition)) grounded++;
            else if (IsGoEmoteRelease(transition)) goEmote++;
            else if (IsFavoriteShowcaseRelease(transition)) favoriteShowcase++;
            else if (IsEyelookMigrationRelease(source, transition)) eyelookMigration++;
            else
            {
                other++;
                var conditions = string.Join(", ", transition.conditions.Select(condition =>
                    $"{condition.parameter}:{condition.mode}:{condition.threshold}"));
                otherDetails.Add($"{(source == null ? "Any State" : source.name)} -> " +
                                 $"{transition.destinationState.name} [{conditions}]");
            }
        }

        private static IEnumerable<(AnimatorStateTransition transition, AnimatorState source)> EnumerateTransitions(
            AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            foreach (var entry in EnumerateTransitions(layer.stateMachine))
                yield return entry;
        }

        private static IEnumerable<(AnimatorStateTransition transition, AnimatorState source)> EnumerateTransitions(
            AnimatorStateMachine machine)
        {
            foreach (var child in machine.states)
            foreach (var transition in child.state.transitions)
                yield return (transition, child.state);
            foreach (var transition in machine.anyStateTransitions)
                yield return (transition, null);
            foreach (var childMachine in machine.stateMachines)
            foreach (var entry in EnumerateTransitions(childMachine.stateMachine))
                yield return entry;
        }

        private static bool IsAutomaticRelease(AnimatorStateTransition transition) =>
            WritesPoseSetZero(transition == null ? null : transition.destinationState) &&
            !IsExplicitResetRelease(transition);

        private static bool IsExplicitResetRelease(AnimatorStateTransition transition) =>
            HasCondition(transition, AnimatorConditionMode.If, 0, "PE/Reset");

        private static bool IsGroundedRelease(AnimatorStateTransition transition) =>
            HasCondition(transition, AnimatorConditionMode.IfNot, 0, "Grounded") &&
            HasCondition(transition, AnimatorConditionMode.Greater, 0, "PE/Set");

        private static bool IsGoEmoteRelease(AnimatorStateTransition transition) =>
            HasCondition(transition, AnimatorConditionMode.Equals, 255, "Go/VRCEmote");

        private static bool IsFavoriteShowcaseRelease(AnimatorStateTransition transition) =>
            HasCondition(transition, AnimatorConditionMode.IfNot, 0, "PE/Favorite/Showcase");

        private static bool IsEyelookMigrationRelease(AnimatorState source,
            AnimatorStateTransition transition) =>
            source != null &&
            (source.name == "Migrate Eyelook State Off" ||
             source.name == "Migrate Eyelook State On") &&
            HasCondition(transition, AnimatorConditionMode.IfNot, 0, "PE/Exit");

        private static bool HasCondition(AnimatorStateTransition transition, AnimatorConditionMode mode,
            float threshold, string parameter) =>
            transition != null && transition.conditions.Any(condition =>
                condition.mode == mode && condition.parameter == parameter &&
                Mathf.Approximately(condition.threshold, threshold));

        private static bool WritesPoseSetZero(AnimatorState state) =>
            state != null && state.behaviours.OfType<VRCAvatarParameterDriver>()
                .SelectMany(driver => driver.parameters)
                .Any(parameter => parameter.type == VRCAvatarParameterDriver.ChangeType.Set &&
                                  parameter.name == "PE/Set" && Mathf.Approximately(parameter.value, 0));
    }
}
#endif
