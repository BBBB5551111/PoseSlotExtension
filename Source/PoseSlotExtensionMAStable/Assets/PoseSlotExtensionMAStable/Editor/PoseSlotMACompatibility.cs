#if UNITY_EDITOR
using System;
using System.Linq;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PoseSlotExtensionMAStable.Editor.PoseSlotMACompatibilityPlugin))]

namespace PoseSlotExtensionMAStable.Editor
{
    internal sealed class PoseSlotMACompatibilityPlugin : Plugin<PoseSlotMACompatibilityPlugin>
    {
        public override string QualifiedName => "jp.joaki.pose-slot-extension-ma.compatibility";
        public override string DisplayName => "Pose Slot Extension MA Stable - Compatibility";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
                .Run("Keep BuddyWorks MA pose active until Reset", context =>
                    PoseSlotMACompatibility.Apply(context.AvatarRootObject));
        }
    }

    internal static class PoseSlotMACompatibility
    {
        internal static bool Apply(GameObject avatar)
        {
            if (avatar == null || !HasPoseSlotExtension(avatar)) return false;

            var persistentController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                PoseSlotPosePersistence.PersistentActionPath);
            if (persistentController == null)
                throw new InvalidOperationException(
                    "Persistent BuddyWorks Action controller was not generated. Run MA Stable Setup again.");
            if (PoseSlotPosePersistence.CountAutomaticReleaseTransitions(persistentController) != 0 ||
                PoseSlotPosePersistence.CountExplicitResetTransitions(persistentController) < 1)
                throw new InvalidOperationException(
                    "Persistent BuddyWorks Action controller failed its pose-release safety check.");

            var buddyMerge = avatar.GetComponentsInChildren<ModularAvatarMergeAnimator>(true)
                .FirstOrDefault(component => component != null &&
                    IsBuddyWorksActionController(component.animator));
            if (buddyMerge == null)
                throw new InvalidOperationException(
                    "BuddyWorks [MA] Merge Animator was not found on the build avatar.");

            buddyMerge.animator = persistentController;
            EditorUtility.SetDirty(buddyMerge);

            if (!PoseSlotExtensionInstaller.HasModularAvatarMenuOverride(avatar))
                throw new InvalidOperationException(
                    "BuddyWorks [MA] Menu Installer is not bound to the generated Pose Slots menu. " +
                    "Run Tools > Pose Slot Extension MA > Setup again. Diagnostic: " +
                    PoseSlotExtensionInstaller.DescribeModularAvatarMenuBinding(avatar));
            if (!PoseSlotExtensionInstaller.HasModularAvatarCommandParameter(avatar))
                throw new InvalidOperationException(
                    "PSE/Command was not declared by Modular Avatar for the MA installation path.");
            return true;
        }

        private static bool HasPoseSlotExtension(GameObject avatar)
        {
            return avatar.GetComponentsInChildren<Transform>(true)
                .Any(transform => transform != null &&
                                  transform.name == PoseSlotExtensionInstaller.InstalledObjectName);
        }

        private static bool IsBuddyWorksActionController(RuntimeAnimatorController controller)
        {
            if (controller == null) return false;
            var path = AssetDatabase.GetAssetPath(controller).Replace('\\', '/');
            return path == PoseSlotPosePersistence.BuddyActionSourcePath ||
                   path == PoseSlotPosePersistence.PersistentActionPath ||
                   path.EndsWith(
                       "/Generated/Animator/BuddyWorks Poses Extension - Action [Persistent].controller",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
