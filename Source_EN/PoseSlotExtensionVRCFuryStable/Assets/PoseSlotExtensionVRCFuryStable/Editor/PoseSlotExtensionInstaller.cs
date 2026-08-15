#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using nadena.dev.modular_avatar.core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace PoseSlotExtensionVRCFuryStable.Editor
{
    internal sealed class VrcFuryPlayableLayerSource
    {
        internal readonly MonoBehaviour Component;
        internal readonly RuntimeAnimatorController Controller;
        internal readonly string HierarchyPath;
        internal readonly string SerializedPropertyPath;

        internal VrcFuryPlayableLayerSource(
            MonoBehaviour component,
            RuntimeAnimatorController controller,
            string hierarchyPath,
            string serializedPropertyPath)
        {
            Component = component;
            Controller = controller;
            HierarchyPath = hierarchyPath;
            SerializedPropertyPath = serializedPropertyPath;
        }

        internal string DisplayName
        {
            get
            {
                var controllerName = Controller == null ? "Controller not set" : Controller.name;
                var controllerPath = Controller == null ? string.Empty : AssetDatabase.GetAssetPath(Controller);
                return string.IsNullOrEmpty(controllerPath)
                    ? HierarchyPath + " -> " + controllerName
                    : HierarchyPath + " -> " + controllerName + " (" + controllerPath + ")";
            }
        }
    }

    public static class PoseSlotExtensionInstaller
    {
        public static bool Silent;
        internal const string InstalledObjectName = "PoseSlotExtensionVRCFuryStable";
        private const string PrefabPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Prefab/PoseSlotExtension.prefab";
        private const string AnimatorPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Animator/PoseSlotExtension.controller";
        private const string PoseSlotsMenuPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Menus/Pose Slots.asset";
        private const string BuddyWorksVrcFuryPrefabPath =
            "Packages/wtf.buddyworks.posesextension/Data/BUDDYWORKS Poses Extension [VRCF].prefab";
        private const string PosesExtensionRootMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Poses Extension - Root Menu.asset";
        private const string PosesExtensionMainMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Poses Extension - Main Menu.asset";
        private const string PosesExtensionMoreMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Submenus/Poses Extension - More.asset";
        private const string PosesExtensionDancesMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Submenus/Poses Extension - Dances.asset";
        private const string ClonedRootMenuPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Menus/Poses Extension - Root Menu [PSE].asset";
        private const string ClonedMainMenuPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Menus/Poses Extension - Main Menu [PSE].asset";
        private const string ClonedMoreMenuPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/Menus/Poses Extension - More [PSE].asset";
        private const string VrcFuryParametersPath = "Assets/PoseSlotExtensionVRCFuryStable/Generated/PoseSlotExtensionVrcFuryParameters.asset";
        private const string PrivateParametersFolder = "Assets/PoseSlotExtensionVRCFuryStable/Generated/PrivateParameters";

        [MenuItem("Tools/Pose Slot Extension VRCFury/Setup", false, 0)]
        public static void OpenSetup() => PoseSlotSetupWindow.Open();

        [MenuItem("Tools/Pose Slot Extension VRCFury/ONE CLICK - Rebuild Install Validate", false, 10)]
        public static void RebuildInstallValidate()
        {
            var descriptor = ResolveTargetAvatar();
            if (descriptor == null)
            {
                ShowTargetSelectionError();
                return;
            }
            EnsureNoDuplicateVrcFuryBaseLayer(descriptor);
            PoseSlotFixedSpecification.ValidateOrThrow();
            PoseSlotExtensionGenerator.Generate();
            if (!Install(descriptor)) return;
            if (!PoseSlotBuildValidator.Validate(descriptor)) return;
            var avatarName = descriptor.gameObject.name;
            var scenePath = descriptor.gameObject.scene.path;
            EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
            EditorSceneManager.SaveScene(descriptor.gameObject.scene);
            AssetDatabase.SaveAssets();

            // Unity can silently drop edits when it serializes the scene (for
            // example SerializeReference data on prefab instances). Reload the
            // saved scene from disk and validate again, so a state that only
            // existed in memory fails here instead of after a real upload.
            var reloadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var reloadedDescriptor = Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>()
                .FirstOrDefault(value => value != null && value.gameObject.scene == reloadedScene &&
                                         value.gameObject.name == avatarName);
            if (reloadedDescriptor == null)
                throw new InvalidOperationException(
                    "Could not find the target avatar again after reloading the saved scene: " + avatarName);
            if (!PoseSlotBuildValidator.Validate(reloadedDescriptor))
                throw new InvalidOperationException(
                    "NDMF validation FAILED after saving and reloading the scene. The installation was not saved into the scene. " +
                    "Do not upload in this state.");
        }

        [MenuItem("Tools/Pose Slot Extension VRCFury/Install to Selected Avatar", false, 20)]
        public static void Install()
        {
            var descriptor = ResolveTargetAvatar();
            if (descriptor == null)
            {
                ShowTargetSelectionError();
                return;
            }
            Install(descriptor);
        }

        public static bool Install(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null || !descriptor.gameObject.scene.IsValid())
                throw new InvalidOperationException("Select a valid Avatar Descriptor in the scene.");
            EnsureBuddyWorksVrcFuryInstalled(descriptor);
            if (!HasVrcFuryParameterList(descriptor.gameObject))
                throw new InvalidOperationException(
                    "Could not find the Parameters field on the BUDDYWORKS VRCFury Full Controller. " +
                    "Reinstall a compatible BUDDYWORKS Poses Extension.");

            EnsureNoDuplicateVrcFuryBaseLayer(descriptor);

            PoseSlotFixedSpecification.ValidateOrThrow();
            var existingObjects = descriptor.transform.Cast<Transform>()
                .Where(x => IsReplaceableInstalledObjectName(x.name))
                .ToArray();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException(
                    "The generated prefab was not found. Run \"Generate, Install and Validate\" in Setup again.");

            var animator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (animator == null)
                throw new InvalidOperationException(
                    "The generated Animator could not be loaded. Run \"Generate, Install and Validate\" in Setup again.");

            MigrateLegacyPseParametersWithoutTouchingSource(descriptor);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, descriptor.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(instance, "Install Pose Slot Extension");
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = InstalledObjectName + " [Installing]";
            instance.transform.SetParent(descriptor.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var merge = instance.GetComponent<ModularAvatarMergeAnimator>();
            if (merge == null)
            {
                Undo.DestroyObjectImmediate(instance);
                throw new InvalidOperationException(
                    "Could not load the Modular Avatar Merge Animator from the generated prefab." +
                    "The Generated folder may be in a partial state. Generate again.");
            }
            merge.animator = animator;
            merge.layerType = VRCAvatarDescriptor.AnimLayerType.Action;
            EditorUtility.SetDirty(merge);

            var clonedRoot = BuildPrivatePosesExtensionMenus();
            if (!ReplaceVrcFuryRootMenuReference(descriptor.gameObject, clonedRoot))
            {
                Undo.DestroyObjectImmediate(instance);
                throw new InvalidOperationException(
                    "Could not replace the menu reference on BUDDYWORKS Poses Extension [VRCF]. " +
                    "The target avatar needs the [VRCF] variant, not the [MA] variant. " +
                    "Check that the [VRCF] variant is enabled in the Hierarchy.");
            }
            try
            {
                InstallVrcFuryParameters(descriptor.gameObject, clonedRoot);
            }
            catch
            {
                Undo.DestroyObjectImmediate(instance);
                throw;
            }
            foreach (var existing in existingObjects)
                if (existing != null && existing.gameObject != instance)
                    Undo.DestroyObjectImmediate(existing.gameObject);
            instance.name = InstalledObjectName;
            EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
            EditorSceneManager.SaveScene(descriptor.gameObject.scene);
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log("[PoseSlotExtension] Installed under " + descriptor.gameObject.name +
                      " and saved scene: " + descriptor.gameObject.scene.path);
            if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension",
                descriptor.gameObject.name + " has been updated to the latest version and the scene was saved.", "OK");
            return true;
        }

        internal static VRCAvatarDescriptor ResolveTargetAvatar()
        {
            var selected = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (selected != null && selected.gameObject.scene.IsValid()) return selected;

            var compatible = FindCompatibleAvatars().ToArray();
            if (compatible.Length == 1) return compatible[0];

            // A fresh avatar has no BUDDYWORKS VRCFury prefab yet. If only one avatar
            // exists in loaded scenes, it is still an unambiguous install target.
            var sceneAvatars = Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>()
                .Where(value => value != null && value.gameObject.scene.IsValid())
                .OrderBy(value => value.gameObject.scene.path)
                .ThenBy(value => value.gameObject.name)
                .ToArray();
            return sceneAvatars.Length == 1 ? sceneAvatars[0] : null;
        }

        internal static bool CanAutoInstallBuddyWorksVrcFury()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(BuddyWorksVrcFuryPrefabPath) != null;
        }

        private static void EnsureBuddyWorksVrcFuryInstalled(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            var existing = descriptor.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value != null &&
                    value.name.IndexOf("BUDDYWORKS Poses Extension [VRCF]",
                        StringComparison.OrdinalIgnoreCase) >= 0);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuddyWorksVrcFuryPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException(
                    "BUDDYWORKS Poses Extension [VRCF] prefab was not found." +
                    "Install BUDDYWORKS Poses Extension 7.2.1 through VCC.");

            var instance = PrefabUtility.InstantiatePrefab(prefab, descriptor.gameObject.scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Automatic installation of BUDDYWORKS Poses Extension [VRCF] failed.");

            Undo.RegisterCreatedObjectUndo(instance, "Install BUDDYWORKS Poses Extension [VRCF]");
            instance.name = prefab.name;
            instance.transform.SetParent(descriptor.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(instance);
            EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
        }

        internal static IEnumerable<VRCAvatarDescriptor> FindCompatibleAvatars()
        {
            return Resources.FindObjectsOfTypeAll<VRCAvatarDescriptor>()
                .Where(value => value != null && value.gameObject.scene.IsValid() &&
                                HasBuddyWorksPoseExtension(value.gameObject))
                .OrderBy(value => value.gameObject.scene.path)
                .ThenBy(value => value.gameObject.name);
        }

        internal static bool IsInstalled(VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return false;
            var child = descriptor.transform.Cast<Transform>()
                .FirstOrDefault(value => value.name == InstalledObjectName);
            var merge = child == null ? null : child.GetComponent<ModularAvatarMergeAnimator>();
            return merge != null && merge.animator != null && HasVrcFuryMenuOverride(descriptor.gameObject);
        }

        private static bool IsReplaceableInstalledObjectName(string objectName)
        {
            return objectName == InstalledObjectName ||
                   objectName == "PoseSlotExtension" ||
                   objectName == "PoseSlotExtension [MA]" ||
                   objectName == "PoseSlotExtension [VRCFury]";
        }

        internal static VrcFuryPlayableLayerSource[] FindVrcFuryBaseLayerSources(
            VRCAvatarDescriptor descriptor)
        {
            if (descriptor == null) return Array.Empty<VrcFuryPlayableLayerSource>();

            const int baseLayerType = 0;
            var sources = new List<VrcFuryPlayableLayerSource>();
            foreach (var component in descriptor.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || !component.isActiveAndEnabled) continue;
                var typeName = component.GetType().FullName;
                if (string.IsNullOrEmpty(typeName) ||
                    typeName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;

                SerializedObject serialized;
                try
                {
                    serialized = new SerializedObject(component);
                }
                catch
                {
                    continue;
                }

                var iterator = serialized.GetIterator();
                if (!iterator.Next(true)) continue;
                do
                {
                    if (iterator.name != "type" ||
                        iterator.propertyPath.IndexOf(".controllers.Array.data[", StringComparison.Ordinal) < 0 ||
                        (iterator.propertyType != SerializedPropertyType.Integer &&
                         iterator.propertyType != SerializedPropertyType.Enum)) continue;

                    var layerType = iterator.propertyType == SerializedPropertyType.Enum
                        ? iterator.enumValueIndex
                        : iterator.intValue;
                    if (layerType != baseLayerType) continue;

                    var suffixIndex = iterator.propertyPath.LastIndexOf(".type", StringComparison.Ordinal);
                    if (suffixIndex < 0) continue;
                    var entryPath = iterator.propertyPath.Substring(0, suffixIndex);
                    var controllerProperty = serialized.FindProperty(entryPath + ".controller.objRef");
                    var controller = controllerProperty == null
                        ? null
                        : controllerProperty.objectReferenceValue as RuntimeAnimatorController;
                    if (controller == null) continue;

                    sources.Add(new VrcFuryPlayableLayerSource(
                        component,
                        controller,
                        GetHierarchyPath(component.transform, descriptor.transform),
                        entryPath));
                } while (iterator.Next(true));
            }

            return sources
                .OrderBy(value => value.HierarchyPath, StringComparer.Ordinal)
                .ThenBy(value => value.Controller == null ? string.Empty : value.Controller.name,
                    StringComparer.Ordinal)
                .ToArray();
        }

        internal static void EnsureNoDuplicateVrcFuryBaseLayer(VRCAvatarDescriptor descriptor)
        {
            var sources = FindVrcFuryBaseLayerSources(descriptor);
            if (sources.Length <= 1) return;

            throw new InvalidOperationException(
                "Multiple VRCFury Base (Locomotion) layers are installed. Keep only one Base.\n" +
                string.Join("\n", sources.Select(value => "- " + value.DisplayName)));
        }

        private static string GetHierarchyPath(Transform transform, Transform avatarRoot)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
                if (current == avatarRoot) break;
            }
            return string.Join("/", names.ToArray());
        }

        internal static bool HasBuddyWorksPoseExtension(GameObject avatar)
        {
            if (avatar == null) return false;
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var property = new SerializedObject(component).GetIterator();
                if (!property.Next(true)) continue;
                do
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        (property.objectReferenceValue == originalRoot ||
                         property.objectReferenceValue == clonedRoot ||
                         IsLegacyPseRootMenu(property.objectReferenceValue as VRCExpressionsMenu)))
                        return true;
                } while (property.Next(true));
            }
            return false;
        }

        private static void ShowTargetSelectionError()
        {
            if (Silent) return;
            EditorUtility.DisplayDialog("Pose Slot Extension",
                "Select the target avatar (or one of its children) in the Hierarchy.\n" +
                "If exactly one avatar has BUDDYWORKS Poses Extension it is selected automatically.", "OK");
        }

        private static void MigrateLegacyPseParametersWithoutTouchingSource(VRCAvatarDescriptor descriptor)
        {
            var source = descriptor.expressionParameters;
            if (source == null || source.parameters == null ||
                !source.parameters.Any(value => value != null &&
                    value.name.StartsWith("PSE/", StringComparison.Ordinal))) return;

            EnsureAssetFolder(PrivateParametersFolder);
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            var key = string.IsNullOrEmpty(sourceGuid) ? "scene" : sourceGuid.Substring(0, 8);
            var fileName = MakeSafeFileName(descriptor.gameObject.name) + "-" + key + ".asset";
            var privatePath = PrivateParametersFolder + "/" + fileName;
            var privateAsset = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(privatePath);
            if (privateAsset == null && !string.IsNullOrEmpty(sourcePath))
            {
                if (!AssetDatabase.CopyAsset(sourcePath, privatePath))
                    throw new InvalidOperationException("Failed to safely duplicate the existing Expression Parameters.");
                privateAsset = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(privatePath);
            }
            else if (privateAsset == null)
            {
                privateAsset = UnityEngine.Object.Instantiate(source);
                privateAsset.name = Path.GetFileNameWithoutExtension(privatePath);
                AssetDatabase.CreateAsset(privateAsset, privatePath);
            }
            else if (source != privateAsset)
            {
                EditorUtility.CopySerialized(source, privateAsset);
            }
            privateAsset.parameters = privateAsset.parameters
                .Where(value => value != null && !value.name.StartsWith("PSE/", StringComparison.Ordinal))
                .ToArray();
            descriptor.expressionParameters = privateAsset;
            EditorUtility.SetDirty(privateAsset);
            EditorUtility.SetDirty(descriptor);
            AssetDatabase.SaveAssets();
            Debug.Log("[PoseSlotExtension] Migrated legacy PSE parameters to a private copy. Source kept unchanged: " + sourcePath);
        }

        private static void InstallVrcFuryParameters(GameObject avatar, VRCExpressionsMenu clonedRoot)
        {
            var parametersAsset = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(VrcFuryParametersPath);
            if (parametersAsset == null)
            {
                parametersAsset = ScriptableObject.CreateInstance<VRCExpressionParameters>();
                AssetDatabase.CreateAsset(parametersAsset, VrcFuryParametersPath);
            }
            parametersAsset.name = "PoseSlotExtensionVrcFuryParameters";
            parametersAsset.parameters = new[]
            {
                new VRCExpressionParameters.Parameter { name = "PSE/Command", valueType = VRCExpressionParameters.ValueType.Int, defaultValue = 0, saved = false, networkSynced = false }
            };
            EditorUtility.SetDirty(parametersAsset);
            AssetDatabase.SaveAssets();

            var assetGuid = AssetDatabase.AssetPathToGUID(VrcFuryParametersPath);
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var serialized = new SerializedObject(component);
                if (!ReferencesObject(serialized, clonedRoot)) continue;
                EnsureVrcFuryComponentEditable(component);
                serialized = new SerializedObject(component);
                var iterator = serialized.GetIterator();
                if (!iterator.Next(true)) continue;
                do
                {
                    if (!iterator.isArray || !iterator.propertyPath.EndsWith(".prms", StringComparison.Ordinal)) continue;
                    var alreadyPresent = false;
                    for (var i = iterator.arraySize - 1; i >= 0; i--)
                    {
                        var existingEntry = iterator.GetArrayElementAtIndex(i);
                        var existingObjRef = existingEntry.FindPropertyRelative("parameters.objRef");
                        var existingId = existingEntry.FindPropertyRelative("parameters.id");
                        var existingGuid = existingEntry.FindPropertyRelative("parameters.guid");
                        var existingFileId = existingEntry.FindPropertyRelative("parameters.fileID");
                        var existingVersion = existingEntry.FindPropertyRelative("parameters.version");
                        var referencedAsset = existingObjRef == null
                            ? null
                            : existingObjRef.objectReferenceValue as VRCExpressionParameters;
                        var referencedPath = referencedAsset == null
                            ? string.Empty
                            : AssetDatabase.GetAssetPath(referencedAsset);
                        var serializedId = existingId == null ? string.Empty : existingId.stringValue;
                        var isCurrent = referencedAsset == parametersAsset ||
                                        string.Equals(referencedPath, VrcFuryParametersPath,
                                            StringComparison.OrdinalIgnoreCase) ||
                                        serializedId.IndexOf(VrcFuryParametersPath,
                                            StringComparison.OrdinalIgnoreCase) >= 0;
                        if (isCurrent)
                        {
                            if (existingObjRef != null) existingObjRef.objectReferenceValue = parametersAsset;
                            if (existingId != null) existingId.stringValue = assetGuid + "|" + VrcFuryParametersPath;
                            if (existingGuid != null) existingGuid.stringValue = assetGuid;
                            if (existingFileId != null) existingFileId.longValue = 11400000;
                            if (existingVersion != null) existingVersion.intValue = 1;
                            alreadyPresent = true;
                            continue;
                        }

                        var referencedName = referencedAsset == null ? string.Empty : referencedAsset.name;
                        var isObsoletePoseSlotEntry =
                            string.Equals(referencedName, "PoseSlotExtensionVrcFuryParameters",
                                StringComparison.OrdinalIgnoreCase) ||
                            LooksLikePoseSlotParameterPath(referencedPath) ||
                            LooksLikePoseSlotParameterPath(serializedId);
                        if (isObsoletePoseSlotEntry)
                            iterator.DeleteArrayElementAtIndex(i);
                    }
                    if (alreadyPresent)
                    {
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(component);
                        if (PrefabUtility.IsPartOfPrefabInstance(component))
                            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                        return;
                    }
                    var index = iterator.arraySize;
                    iterator.InsertArrayElementAtIndex(index);
                    var entry = iterator.GetArrayElementAtIndex(index);
                    var prefix = "parameters.";
                    var obj = entry.FindPropertyRelative(prefix + "objRef");
                    var id = entry.FindPropertyRelative(prefix + "id");
                    var guid = entry.FindPropertyRelative(prefix + "guid");
                    var fileId = entry.FindPropertyRelative(prefix + "fileID");
                    var version = entry.FindPropertyRelative(prefix + "version");
                    if (obj != null) obj.objectReferenceValue = parametersAsset;
                    if (id != null) id.stringValue = assetGuid + "|" + VrcFuryParametersPath;
                    if (guid != null) guid.stringValue = assetGuid;
                    if (fileId != null) fileId.longValue = 11400000;
                    if (version != null) version.intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                    if (PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    return;
                } while (iterator.Next(true));
            }
            throw new InvalidOperationException("Could not detect the Parameters field on the BUDDYWORKS VRCFury Full Controller.");
        }

        private static bool LooksLikePoseSlotParameterPath(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var normalized = value.Replace('\\', '/');
            return normalized.IndexOf("PoseSlotExtension", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   normalized.EndsWith("/PoseSlotExtensionVrcFuryParameters.asset",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool ReferencesObject(SerializedObject serialized, UnityEngine.Object expected)
        {
            var iterator = serialized.GetIterator();
            if (!iterator.Next(true)) return false;
            do
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.objectReferenceValue == expected) return true;
            } while (iterator.Next(true));
            return false;
        }

        private static bool HasVrcFuryParameterList(GameObject avatar)
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var serialized = new SerializedObject(component);
                if (!ReferencesObject(serialized, originalRoot) &&
                    !ReferencesObject(serialized, clonedRoot) &&
                    !ReferencesLegacyPseRootMenu(serialized)) continue;
                var iterator = serialized.GetIterator();
                if (!iterator.Next(true)) continue;
                do
                {
                    if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String &&
                        iterator.propertyPath.EndsWith(".prms", StringComparison.Ordinal)) return true;
                } while (iterator.Next(true));
            }
            return false;
        }

        private static bool ReferencesLegacyPseRootMenu(SerializedObject serialized)
        {
            var property = serialized.GetIterator();
            if (!property.Next(true)) return false;
            do
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    IsLegacyPseRootMenu(property.objectReferenceValue as VRCExpressionsMenu))
                    return true;
            } while (property.Next(true));
            return false;
        }

        private static bool IsLegacyPseRootMenu(VRCExpressionsMenu menu)
        {
            if (menu == null) return false;
            return AssetDatabase.GetAssetPath(menu)
                .Replace('\\', '/')
                .EndsWith("/Generated/Menus/Poses Extension - Root Menu [PSE].asset",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static VRCExpressionsMenu BuildPrivatePosesExtensionMenus()
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var originalMain = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionMainMenuPath);
            var originalMore = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionMoreMenuPath);
            var originalDances = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionDancesMenuPath);
            var poseSlots = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PoseSlotsMenuPath);
            if (originalRoot == null || originalMain == null || originalMore == null ||
                originalDances == null || poseSlots == null)
                throw new InvalidOperationException("The original Pose Extension menu or the Pose Slots menu was not found.");

            var clonedMore = CloneMenuAsset(originalMore, ClonedMoreMenuPath);
            var clonedMain = CloneMenuAsset(originalMain, ClonedMainMenuPath);

            // The only feature added by the VRCFury stable line. The BUDDYWORKS
            // original is never edited; only the top-level Dances and Pose Slots
            // entries are swapped inside the duplicated menus.
            clonedMore.controls.RemoveAll(c => c.name == "Pose Slots" ||
                (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == originalDances));
            clonedMain.controls.RemoveAll(c => c.name == "Pose Slots");
            var dancesIndex = clonedMain.controls.FindIndex(c =>
                c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == originalDances);
            if (dancesIndex < 0)
                throw new InvalidOperationException("Could not find Dances in the BUDDYWORKS top-level menu.");

            var dancesControl = clonedMain.controls[dancesIndex];
            clonedMain.controls[dancesIndex] = new VRCExpressionsMenu.Control
            {
                name = "Pose Slots",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = poseSlots
            };
            clonedMore.controls.Add(dancesControl);
            if (clonedMain.controls.Count > 8 || clonedMore.controls.Count > 8)
                throw new InvalidOperationException("After the menu swap the control count exceeds VRChat's limit of 8 controls.");

            EditorUtility.SetDirty(clonedMore);
            EditorUtility.SetDirty(clonedMain);
            ReplaceSubMenu(clonedMain, originalMore, clonedMore);
            var clonedRoot = CloneMenuAsset(originalRoot, ClonedRootMenuPath);
            ReplaceSubMenu(clonedRoot, originalMain, clonedMain);
            AssetDatabase.SaveAssets();
            return clonedRoot;
        }

        private static VRCExpressionsMenu CloneMenuAsset(VRCExpressionsMenu source, string path)
        {
            var target = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);
            if (target == null)
            {
                target = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AssetDatabase.CreateAsset(target, path);
            }
            EditorUtility.CopySerialized(source, target);
            target.name = System.IO.Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static void ReplaceSubMenu(VRCExpressionsMenu menu, VRCExpressionsMenu oldMenu, VRCExpressionsMenu newMenu)
        {
            var control = menu.controls.FirstOrDefault(c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == oldMenu);
            if (control == null) throw new InvalidOperationException(menu.name + " could not have its submenu reference replaced.");
            control.subMenu = newMenu;
            EditorUtility.SetDirty(menu);
        }

        private static bool ReplaceVrcFuryRootMenuReference(GameObject avatar, VRCExpressionsMenu clonedRoot)
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var replaced = false;
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;
                // Other BUDDYWORKS/VRCFury products (Toolbox etc.) also live on the
                // avatar as prefab instances. Only the component that actually holds
                // a Pose Extension menu reference may be unpacked and patched.
                if (!NeedsPoseSlotMenuPatch(component, originalRoot, clonedRoot)) continue;
                EnsureVrcFuryComponentEditable(component);
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                var componentReplaced = false;
                if (!property.Next(true)) continue;
                do
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var isExpectedRoot = property.objectReferenceValue == originalRoot;
                    var isCurrentClone = property.objectReferenceValue == clonedRoot;
                    var isLegacyPseClone = IsLegacyPseRootMenu(
                        property.objectReferenceValue as VRCExpressionsMenu);
                    var hasObjectReferenceSuffix = property.propertyPath.EndsWith(".objRef", StringComparison.Ordinal);
                    var parentPath = hasObjectReferenceSuffix
                        ? property.propertyPath.Substring(0, property.propertyPath.Length - ".objRef".Length)
                        : string.Empty;
                    var idProperty = hasObjectReferenceSuffix
                        ? serialized.FindProperty(parentPath + ".id")
                        : null;
                    var serializedId = idProperty == null ? string.Empty : idProperty.stringValue;
                    var isBrokenPreviousOverride = property.objectReferenceValue == null &&
                        (property.propertyPath.EndsWith(".menus.Array.data[0].menu.objRef", StringComparison.Ordinal) ||
                         LooksLikePoseSlotRootMenuPath(serializedId));
                    if (!isExpectedRoot && !isCurrentClone && !isLegacyPseClone && !isBrokenPreviousOverride) continue;
                    property.objectReferenceValue = clonedRoot;
                    var cloneGuid = AssetDatabase.AssetPathToGUID(ClonedRootMenuPath);
                    var guidProperty = serialized.FindProperty(parentPath + ".guid");
                    var fileIdProperty = serialized.FindProperty(parentPath + ".fileID");
                    if (idProperty != null) idProperty.stringValue = cloneGuid + "|" + ClonedRootMenuPath;
                    if (guidProperty != null) guidProperty.stringValue = cloneGuid;
                    if (fileIdProperty != null) fileIdProperty.longValue = 11400000;
                    componentReplaced = true;
                    replaced = true;
                } while (property.Next(true));
                if (componentReplaced)
                {
                    // VRCFury namespaces parameters owned by a Full Controller unless
                    // they are explicitly global. The Pose Slots menu is installed by
                    // that Full Controller, while the MA animator intentionally reads
                    // the avatar-global PSE/Command. Without this entry the menu writes
                    // VF###_PSE/Command and the animator waits forever on PSE/Command.
                    EnsureStringArrayContains(serialized, ".globalParams", "PSE/Command");
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                    if (PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
            return replaced;
        }

        private static bool LooksLikePoseSlotRootMenuPath(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            var normalized = value.Replace('\\', '/');
            return normalized.IndexOf("PoseSlotExtension", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   normalized.EndsWith("/Generated/Menus/Poses Extension - Root Menu [PSE].asset",
                       StringComparison.OrdinalIgnoreCase);
        }

        // Read-only precheck mirroring the patch conditions in
        // ReplaceVrcFuryRootMenuReference, so unrelated VRCFury components are
        // never unpacked or rejected.
        private static bool NeedsPoseSlotMenuPatch(MonoBehaviour component,
            VRCExpressionsMenu originalRoot, VRCExpressionsMenu clonedRoot)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            if (!property.Next(true)) return false;
            do
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue == originalRoot && originalRoot != null) return true;
                if (property.objectReferenceValue == clonedRoot && clonedRoot != null) return true;
                if (IsLegacyPseRootMenu(property.objectReferenceValue as VRCExpressionsMenu)) return true;
                if (property.objectReferenceValue == null)
                {
                    var hasObjectReferenceSuffix = property.propertyPath.EndsWith(".objRef", StringComparison.Ordinal);
                    var parentPath = hasObjectReferenceSuffix
                        ? property.propertyPath.Substring(0, property.propertyPath.Length - ".objRef".Length)
                        : string.Empty;
                    var idProperty = hasObjectReferenceSuffix
                        ? serialized.FindProperty(parentPath + ".id")
                        : null;
                    var serializedId = idProperty == null ? string.Empty : idProperty.stringValue;
                    if (LooksLikePoseSlotRootMenuPath(serializedId)) return true;
                    if (property.propertyPath.EndsWith(".menus.Array.data[0].menu.objRef", StringComparison.Ordinal) &&
                        IsBuddyWorksPosesObject(component))
                        return true;
                }
            } while (property.Next(true));
            return false;
        }

        private static bool IsBuddyWorksPosesObject(MonoBehaviour component)
        {
            for (var current = component.transform; current != null; current = current.parent)
                if (current.name.IndexOf("BUDDYWORKS Poses Extension",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        // VRCFury components serialize their model through SerializeReference.
        // Unity cannot persist SerializeReference edits as prefab-instance
        // overrides: the change survives in memory (so same-session validation
        // passes) but is silently dropped on scene save, and the next session
        // builds the unmodified package prefab. Editing therefore requires the
        // component to live outside any prefab instance. Unpack only when the
        // outermost instance root is the BUDDYWORKS prefab itself; a component
        // nested inside the avatar's own prefab must stop with an explicit error
        // because unpacking there would flatten the whole avatar.
        private static void EnsureVrcFuryComponentEditable(MonoBehaviour component)
        {
            if (component == null || !PrefabUtility.IsPartOfPrefabInstance(component)) return;
            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(component.gameObject);
            if (prefabRoot == null) return;
            if (prefabRoot.name.IndexOf("BUDDYWORKS Poses Extension",
                    StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(
                    "The BUDDYWORKS VRCFury component is nested inside the avatar prefab. " +
                    "Menu and Parameters settings cannot be saved in this state. " +
                    "Move BUDDYWORKS [VRCF] outside the avatar prefab (directly under the avatar in the scene) " +
                    "and run this again. Prefab root: " + prefabRoot.name);
            PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }

        private static void EnsureStringArrayContains(SerializedObject serialized, string propertySuffix, string value)
        {
            var property = serialized.GetIterator();
            if (!property.Next(true))
                throw new InvalidOperationException("VRCFury serialized data could not be inspected.");
            do
            {
                if (!property.isArray || property.propertyType == SerializedPropertyType.String ||
                    !property.propertyPath.EndsWith(propertySuffix, StringComparison.Ordinal)) continue;
                for (var i = 0; i < property.arraySize; i++)
                    if (property.GetArrayElementAtIndex(i).stringValue == value) return;
                var index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).stringValue = value;
                return;
            } while (property.Next(true));
            throw new InvalidOperationException("VRCFury globalParams was not found on the menu Full Controller.");
        }

        private static bool HasVrcFuryMenuOverride(GameObject avatar)
        {
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            if (clonedRoot == null) return false;
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var property = new SerializedObject(component).GetIterator();
                if (!property.Next(true)) continue;
                do
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == clonedRoot) return true;
                } while (property.Next(true));
            }
            return false;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }

        private static string MakeSafeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string((value ?? "Avatar").Select(character =>
                invalid.Contains(character) ? '_' : character).ToArray());
        }

    }
}
#endif
