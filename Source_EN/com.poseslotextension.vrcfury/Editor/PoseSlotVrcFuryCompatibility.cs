#if UNITY_EDITOR
using System;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PoseSlotExtensionVRCFuryStable.Editor.PoseSlotVrcFuryCompatibilityPlugin))]

namespace PoseSlotExtensionVRCFuryStable.Editor
{
    internal sealed class PoseSlotVrcFuryCompatibilityPlugin : Plugin<PoseSlotVrcFuryCompatibilityPlugin>
    {
        public override string QualifiedName => "jp.joaki.pose-slot-extension-vrcfury.compatibility";
        public override string DisplayName => "Pose Slot Extension VRCFury Stable - Compatibility";

        protected override void Configure()
        {
            // NDMF's Resolving phase runs on the temporary build avatar before
            // VRCFury's SDK preprocessor. This keeps the source BUDDYWORKS prefab,
            // controller and scene configuration untouched.
            InPhase(BuildPhase.Resolving)
                .Run("Keep BUDDYWORKS pose shortcut parameters global", context =>
                    PoseSlotVrcFuryCompatibility.Apply(context.AvatarRootObject));
        }
    }

    internal static class PoseSlotVrcFuryCompatibility
    {
        internal const string PoseFloat = PoseSlotFixedSpecification.PoseFloatParameter;
        internal const string Command = PoseSlotFixedSpecification.CommandParameter;

        internal static bool Apply(GameObject avatar)
        {
            if (avatar == null || !HasPoseSlotExtension(avatar)) return false;

            var buddyController = avatar.GetComponentsInChildren<MonoBehaviour>(true)
                .FirstOrDefault(IsBuddyWorksFullController);
            if (buddyController == null)
                throw new InvalidOperationException(
                    "Pose Slot Extension is installed, but the BUDDYWORKS VRCFury Full Controller was not found. " +
                    "Build was stopped before upload.");

            var persistentController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                PoseSlotPosePersistence.PersistentActionPath);
            if (persistentController == null)
                throw new InvalidOperationException(
                    "Persistent BUDDYWORKS Action controller was not generated. Run the Pose Slot generator first.");
            if (PoseSlotPosePersistence.CountAutomaticReleaseTransitions(persistentController) != 0 ||
                PoseSlotPosePersistence.CountExplicitResetTransitions(persistentController) < 1)
                throw new InvalidOperationException(
                    "Persistent BUDDYWORKS Action controller failed its pose-release safety check.");

            var serialized = new SerializedObject(buddyController);
            if (!ReplaceBuddyActionController(serialized, persistentController))
                throw new InvalidOperationException(
                    "BUDDYWORKS Action controller reference could not be replaced on the temporary build avatar.");
            EnsureStringArrayContains(serialized, ".globalParams", PoseFloat);
            EnsureStringArrayContains(serialized, ".globalParams", Command);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static bool ReplaceBuddyActionController(SerializedObject serialized,
            AnimatorController persistentController)
        {
            var found = false;
            var property = serialized.GetIterator();
            if (!property.Next(true)) return false;
            do
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var current = property.objectReferenceValue as RuntimeAnimatorController;
                if (current == null) continue;
                var path = AssetDatabase.GetAssetPath(current).Replace('\\', '/');
                if (path != PoseSlotPosePersistence.BuddyActionSourcePath &&
                    path != PoseSlotPosePersistence.PersistentActionPath &&
                    !path.EndsWith(
                        "/Generated/Animator/BUDDYWORKS Poses Extension - Action [Persistent].controller",
                        StringComparison.OrdinalIgnoreCase)) continue;
                property.objectReferenceValue = persistentController;
                found = true;
            } while (property.Next(true));
            return found;
        }

        internal static bool HasGlobalParameter(GameObject avatar, string parameterName)
        {
            if (avatar == null) return false;
            return avatar.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(IsBuddyWorksFullController)
                .Any(component => StringArrayContains(new SerializedObject(component), ".globalParams", parameterName));
        }

        private static bool HasPoseSlotExtension(GameObject avatar) =>
            avatar.GetComponentsInChildren<Transform>(true)
                .Any(transform => transform != null && transform.name == "PoseSlotExtensionVRCFuryStable");

        private static bool IsBuddyWorksFullController(MonoBehaviour component)
        {
            if (component == null ||
                component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            var serialized = new SerializedObject(component);
            // These entries are part of BUDDYWORKS' own Full Controller and form a
            // stable signature without depending on prefab names or VF### prefixes.
            return StringArrayContains(serialized, ".globalParams", "PE/Set") &&
                   StringArrayContains(serialized, ".globalParams", "PE/Favorite/*");
        }

        private static bool StringArrayContains(SerializedObject serialized, string propertySuffix, string value)
        {
            var property = serialized.GetIterator();
            if (!property.Next(true)) return false;
            do
            {
                if (!IsMatchingArray(property, propertySuffix)) continue;
                for (var i = 0; i < property.arraySize; i++)
                    if (property.GetArrayElementAtIndex(i).stringValue == value) return true;
            } while (property.Next(true));
            return false;
        }

        private static void EnsureStringArrayContains(SerializedObject serialized, string propertySuffix, string value)
        {
            var property = serialized.GetIterator();
            if (!property.Next(true))
                throw new InvalidOperationException("VRCFury serialized data could not be inspected.");
            do
            {
                if (!IsMatchingArray(property, propertySuffix)) continue;
                for (var i = 0; i < property.arraySize; i++)
                    if (property.GetArrayElementAtIndex(i).stringValue == value) return;
                var index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).stringValue = value;
                return;
            } while (property.Next(true));
            throw new InvalidOperationException("BUDDYWORKS VRCFury globalParams was not found.");
        }

        private static bool IsMatchingArray(SerializedProperty property, string propertySuffix) =>
            property.isArray && property.propertyType != SerializedPropertyType.String &&
            property.propertyPath.EndsWith(propertySuffix, StringComparison.Ordinal);
    }
}
#endif
