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

namespace PoseSlotExtensionMAStable.Editor
{
    internal enum BuddyWorksInstallMode
    {
        None,
        ModularAvatar,
        VrcFury,
        Conflict
    }

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
                var controllerName = Controller == null ? "Controller未設定" : Controller.name;
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
        internal const string InstalledObjectName = "PoseSlotExtensionMAStable";
        private const string PrefabPath = "Assets/PoseSlotExtensionMAStable/Generated/Prefab/PoseSlotExtension.prefab";
        private const string AnimatorPath = "Assets/PoseSlotExtensionMAStable/Generated/Animator/PoseSlotExtension.controller";
        private const string PoseSlotsMenuPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Pose Slots.asset";
        private const string PosesExtensionMaPrefabPath = "Packages/wtf.buddyworks.posesextension/Data/BUDDYWORKS Poses Extension [MA].prefab";
        private const string PosesExtensionRootMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Poses Extension - Root Menu.asset";
        private const string PosesExtensionMainMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Poses Extension - Main Menu.asset";
        private const string PosesExtensionMoreMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Submenus/Poses Extension - More.asset";
        private const string PosesExtensionDancesMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Submenus/Poses Extension - Dances.asset";
        private const string ClonedRootMenuPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - Root Menu [PSE].asset";
        private const string ClonedMainMenuPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - Main Menu [PSE].asset";
        private const string ClonedMoreMenuPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - More [PSE].asset";
        private const string VrcFuryParametersPath = "Assets/PoseSlotExtensionMAStable/Generated/PoseSlotExtensionVrcFuryParameters.asset";
        private const string PrivateParametersFolder = "Assets/PoseSlotExtensionMAStable/Generated/PrivateParameters";

        [MenuItem("Tools/Pose Slot Extension MA/Setup", false, 0)]
        public static void OpenSetup() => PoseSlotSetupWindow.Open();

        [MenuItem("Tools/Pose Slot Extension MA/ONE CLICK - Rebuild Install Validate", false, 10)]
        public static void RebuildInstallValidate()
        {
            var descriptor = ResolveTargetAvatar();
            if (descriptor == null)
            {
                ShowTargetSelectionError();
                return;
            }
            PoseSlotFixedSpecification.ValidateOrThrow();
            PoseSlotExtensionGenerator.Generate();
            if (!Install(descriptor)) return;
            PoseSlotBuildValidator.Validate(descriptor);
        }

        [MenuItem("Tools/Pose Slot Extension MA/Install to Selected Avatar", false, 20)]
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
                throw new InvalidOperationException("シーン上の有効なAvatar Descriptorを選択してください。");
            const BuddyWorksInstallMode installMode = BuddyWorksInstallMode.ModularAvatar;
            if (!HasBuddyWorksModularAvatar(descriptor.gameObject))
                throw new InvalidOperationException(
                    "BuddyWorks Poses Extension [MA] が対象アバターに見つかりません。この安定版はMA版専用です。");

            PoseSlotFixedSpecification.ValidateOrThrow();
            var existingObjects = descriptor.transform.Cast<Transform>()
                .Where(x => IsReplaceableInstalledObjectName(x.name))
                .ToArray();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException(
                    "生成Prefabが見つかりません。Setupの「生成・導入・検証」をもう一度実行してください。");

            var animator = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (animator == null)
                throw new InvalidOperationException(
                    "生成Animatorを読み込めませんでした。Setupの「生成・導入・検証」をもう一度実行してください。");

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
                    "生成PrefabからModular Avatar Merge Animatorを読み込めませんでした。" +
                    "Generatedフォルダが途中状態の可能性があります。もう一度生成してください。");
            }
            merge.animator = animator;
            merge.layerType = VRCAvatarDescriptor.AnimLayerType.Action;
            EditorUtility.SetDirty(merge);
            ConfigureExtensionParameters(instance, installMode);

            var clonedRoot = BuildPrivatePosesExtensionMenus();
            try
            {
                InstallModularAvatarIntegration(descriptor.gameObject, clonedRoot);
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
                      " using BuddyWorks MA Stable and saved scene: " +
                      descriptor.gameObject.scene.path);
            if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension",
                descriptor.gameObject.name + " へ最新版を導入し、シーンを保存しました。", "OK");
            return true;
        }

        internal static VRCAvatarDescriptor ResolveTargetAvatar()
        {
            var selected = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (selected != null && selected.gameObject.scene.IsValid()) return selected;

            var compatible = FindCompatibleAvatars().ToArray();
            return compatible.Length == 1 ? compatible[0] : null;
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
            var binding = HasBuddyWorksModularAvatar(descriptor.gameObject) &&
                          HasModularAvatarMenuOverride(descriptor.gameObject);
            return merge != null && merge.animator != null && binding;
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
                "VRCFuryのBase（Locomotion）が複数導入されています。Baseは1つだけにしてください。\n" +
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
            return HasBuddyWorksModularAvatar(avatar);
        }

        internal static BuddyWorksInstallMode DetectBuddyWorksInstallMode(GameObject avatar)
        {
            return HasBuddyWorksModularAvatar(avatar)
                ? BuddyWorksInstallMode.ModularAvatar
                : BuddyWorksInstallMode.None;
        }

        private static bool IsReplaceableInstalledObjectName(string objectName)
        {
            return objectName == InstalledObjectName ||
                   objectName == "PoseSlotExtension" ||
                   objectName == "PoseSlotExtension [MA]" ||
                   objectName == "PoseSlotExtension [VRCFury]";
        }

        internal static bool HasBuddyWorksModularAvatar(GameObject avatar)
        {
            if (avatar == null) return false;
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            return avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                .Any(component => IsBuddyWorksModularAvatarMenuInstaller(
                    component, originalRoot, clonedRoot));
        }

        private static bool IsBuddyWorksModularAvatarMenuInstaller(
            ModularAvatarMenuInstaller component,
            VRCExpressionsMenu originalRoot,
            VRCExpressionsMenu clonedRoot)
        {
            if (component == null) return false;
            if ((originalRoot != null && component.menuToAppend == originalRoot) ||
                (clonedRoot != null && component.menuToAppend == clonedRoot))
                return true;

            // The public package intentionally excludes Generated assets. If an
            // avatar still points to a previous PSE clone and that folder is
            // removed, menuToAppend becomes Missing/null. Recover the MA identity
            // from the original BuddyWorks prefab instead of reporting it absent.
            var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(component);
            if (prefabSource != null && prefabSource != component &&
                ((originalRoot != null && prefabSource.menuToAppend == originalRoot) ||
                 (clonedRoot != null && prefabSource.menuToAppend == clonedRoot)))
                return true;

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                component.gameObject);
            if (string.Equals(prefabPath, PosesExtensionMaPrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                return true;

            // Also support an unpacked BuddyWorks MA prefab. Its runtime marker and
            // MA Menu Installer are colocated; the VRCF variant has no MA installer.
            return component.GetComponents<MonoBehaviour>().Any(value =>
                value != null && string.Equals(value.GetType().FullName,
                    "BUDDYWORKS.PosesExtension.BWPosesExtension",
                    StringComparison.Ordinal));
        }

        internal static bool HasBuddyWorksVrcFury(GameObject avatar)
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
                        ((originalRoot != null && property.objectReferenceValue == originalRoot) ||
                         (clonedRoot != null && property.objectReferenceValue == clonedRoot)))
                        return true;
                } while (property.Next(true));
            }
            return false;
        }

        private static void ShowTargetSelectionError()
        {
            if (Silent) return;
            EditorUtility.DisplayDialog("Pose Slot Extension",
                "Hierarchyで対象アバター（またはその子）を選択してください。\n" +
                "BuddyWorks Poses Extensionを持つアバターが1体だけの場合は自動選択されます。", "OK");
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
                    throw new InvalidOperationException("既存Expression Parametersの安全な複製に失敗しました。");
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

        private static void ConfigureExtensionParameters(GameObject instance,
            BuddyWorksInstallMode installMode)
        {
            var component = instance.GetComponent<ModularAvatarParameters>();
            if (component == null)
                throw new InvalidOperationException(
                    "生成PrefabからModular Avatar Parametersを読み込めませんでした。");

            var parameters = component.parameters == null
                ? new List<ParameterConfig>()
                : component.parameters.Where(value =>
                    value.nameOrPrefix != PoseSlotFixedSpecification.CommandParameter).ToList();
            if (installMode == BuddyWorksInstallMode.ModularAvatar)
            {
                parameters.Add(new ParameterConfig
                {
                    nameOrPrefix = PoseSlotFixedSpecification.CommandParameter,
                    syncType = ParameterSyncType.Int,
                    saved = false,
                    localOnly = true,
                    defaultValue = 0,
                    hasExplicitDefaultValue = true
                });
            }
            component.parameters = parameters;
            EditorUtility.SetDirty(component);
        }

        private static void InstallModularAvatarIntegration(GameObject avatar,
            VRCExpressionsMenu clonedRoot)
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var existingClone = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            var menuInstaller = avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                .FirstOrDefault(component => IsBuddyWorksModularAvatarMenuInstaller(
                    component, originalRoot, existingClone ?? clonedRoot));
            if (menuInstaller == null)
                throw new InvalidOperationException(
                    "BuddyWorks Poses Extension [MA] のMA Menu Installerを検出できませんでした。");

            var persistentController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                PoseSlotPosePersistence.PersistentActionPath);
            if (persistentController == null)
                throw new InvalidOperationException(
                    "BuddyWorksのポーズ保持用Action Controllerが生成されていません。");

            var colocatedMerge = menuInstaller.GetComponent<ModularAvatarMergeAnimator>();
            // Generate recreates the private persistent controller. During an
            // idempotent reinstall, the already-installed Merge Animator can
            // therefore temporarily hold a missing/null controller reference.
            // A Merge Animator colocated with the verified BuddyWorks menu
            // installer is still the correct component and must be reconnected.
            var buddyMerge = colocatedMerge != null &&
                (colocatedMerge.animator == null || IsBuddyWorksActionController(colocatedMerge.animator))
                ? colocatedMerge
                : null;
            if (buddyMerge == null)
            {
                buddyMerge = avatar.GetComponentsInChildren<ModularAvatarMergeAnimator>(true)
                    .FirstOrDefault(component => component != null &&
                        IsBuddyWorksActionController(component.animator));
            }
            if (buddyMerge == null)
                throw new InvalidOperationException(
                    "BuddyWorks Poses Extension [MA] のMA Merge Animatorを検出できませんでした。");

            Undo.RecordObject(menuInstaller, "Install Pose Slot Extension menu into BuddyWorks MA");
            menuInstaller.menuToAppend = clonedRoot;
            EditorUtility.SetDirty(menuInstaller);
            if (PrefabUtility.IsPartOfPrefabInstance(menuInstaller))
                PrefabUtility.RecordPrefabInstancePropertyModifications(menuInstaller);

            Undo.RecordObject(buddyMerge, "Keep BuddyWorks pose active until Reset");
            buddyMerge.animator = persistentController;
            buddyMerge.layerType = VRCAvatarDescriptor.AnimLayerType.Action;
            EditorUtility.SetDirty(buddyMerge);
            if (PrefabUtility.IsPartOfPrefabInstance(buddyMerge))
                PrefabUtility.RecordPrefabInstancePropertyModifications(buddyMerge);
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
                var iterator = serialized.GetIterator();
                if (!iterator.Next(true)) continue;
                do
                {
                    if (!iterator.isArray || !iterator.propertyPath.EndsWith(".prms", StringComparison.Ordinal)) continue;
                    var alreadyPresent = false;
                    for (var i = 0; i < iterator.arraySize; i++)
                    {
                        var objRef = iterator.GetArrayElementAtIndex(i).FindPropertyRelative("parameters.objRef");
                        if (objRef != null && objRef.objectReferenceValue == parametersAsset) alreadyPresent = true;
                    }
                    if (alreadyPresent) return;
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
                    return;
                } while (iterator.Next(true));
            }
            throw new InvalidOperationException("BuddyWorks VRCFury Full ControllerのParameters欄を検出できませんでした。");
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
                if (!ReferencesObject(serialized, originalRoot) && !ReferencesObject(serialized, clonedRoot)) continue;
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

        private static VRCExpressionsMenu BuildPrivatePosesExtensionMenus()
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            var originalMain = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionMainMenuPath);
            var originalMore = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionMoreMenuPath);
            var originalDances = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionDancesMenuPath);
            var poseSlots = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PoseSlotsMenuPath);
            if (originalRoot == null || originalMain == null || originalMore == null ||
                originalDances == null || poseSlots == null)
                throw new InvalidOperationException("Pose Extension の元メニューまたは Pose Slots メニューが見つかりません。");

            var clonedMore = CloneMenuAsset(originalMore, ClonedMoreMenuPath);
            var clonedMain = CloneMenuAsset(originalMain, ClonedMainMenuPath);

            // Keep BuddyWorks package assets read-only. In the generated private
            // copies only, exchange the top-level Dances slot with Pose Slots and
            // move the untouched Dances control under More. This preserves the
            // original icon/name/submenu reference and never increases either menu
            // beyond VRChat's eight-control limit.
            clonedMore.controls.RemoveAll(c => c.name == "Pose Slots" ||
                (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == originalDances));
            clonedMain.controls.RemoveAll(c => c.name == "Pose Slots");
            var dancesIndex = clonedMain.controls.FindIndex(c =>
                c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == originalDances);
            if (dancesIndex < 0)
                throw new InvalidOperationException("BuddyWorks最上位メニュー内のDancesを検出できません。");

            var dancesControl = clonedMain.controls[dancesIndex];
            clonedMain.controls[dancesIndex] = new VRCExpressionsMenu.Control
            {
                name = "Pose Slots",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = poseSlots
            };
            clonedMore.controls.Add(dancesControl);
            if (clonedMain.controls.Count > 8 || clonedMore.controls.Count > 8)
                throw new InvalidOperationException("メニュー入れ替え後の項目数がVRChatの上限8を超えています。");

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
            if (control == null) throw new InvalidOperationException(menu.name + " 内のサブメニュー参照を置換できません。");
            control.subMenu = newMenu;
            EditorUtility.SetDirty(menu);
        }

        private static bool ReplaceVrcFuryRootMenuReference(GameObject avatar, VRCExpressionsMenu clonedRoot)
        {
            var originalRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PosesExtensionRootMenuPath);
            // Property overrides are valid even when BuddyWorks is nested inside an
            // avatar prefab. Keep both prefab instances intact and write an override
            // on the live scene component; unpacking a nested instance can target a
            // non-root object and either fail or destructively flatten the avatar.
            var replaced = false;
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!IsVrcFuryComponent(component)) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                var componentReplaced = false;
                if (!property.Next(true)) continue;
                do
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var isExpectedRoot = property.objectReferenceValue == originalRoot;
                    var isCurrentClone = property.objectReferenceValue == clonedRoot;
                    var isBrokenPreviousOverride = property.objectReferenceValue == null &&
                        property.propertyPath.EndsWith(".menus.Array.data[0].menu.objRef", StringComparison.Ordinal);
                    if (!isExpectedRoot && !isCurrentClone && !isBrokenPreviousOverride) continue;
                    property.objectReferenceValue = clonedRoot;
                    var parentPath = property.propertyPath.Substring(0, property.propertyPath.Length - ".objRef".Length);
                    var cloneGuid = AssetDatabase.AssetPathToGUID(ClonedRootMenuPath);
                    var idProperty = serialized.FindProperty(parentPath + ".id");
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

        private static bool IsVrcFuryComponent(MonoBehaviour component) =>
            component != null && !string.IsNullOrEmpty(component.GetType().FullName) &&
            component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) >= 0;

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

        internal static bool HasModularAvatarMenuOverride(GameObject avatar)
        {
            if (avatar == null) return false;
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            var poseSlots = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PoseSlotsMenuPath);
            var installerBinding = avatar
                .GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                .Any(component => component != null &&
                    ((clonedRoot != null && component.menuToAppend == clonedRoot) ||
                     (poseSlots != null && MenuGraphContains(component.menuToAppend, poseSlots)) ||
                     MenuGraphHasPoseSlotCommands(component.menuToAppend)));
            if (installerBinding) return true;

            // A real NDMF build can already have consumed the MA installer and
            // written its controls into the temporary Avatar Descriptor. Treat the
            // actual 50+50 command graph as an equivalent, verified binding.
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            return descriptor != null &&
                   MenuGraphHasPoseSlotCommands(descriptor.expressionsMenu);
        }

        internal static string DescribeModularAvatarMenuBinding(GameObject avatar)
        {
            if (avatar == null) return "avatar=null";
            var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootMenuPath);
            var poseSlots = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(PoseSlotsMenuPath);
            var installers = avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true);
            var entries = installers.Select(component =>
            {
                if (component == null) return "<missing component>";
                var menu = component.menuToAppend;
                var path = menu == null ? "<null>" : AssetDatabase.GetAssetPath(menu);
                if (string.IsNullOrEmpty(path) && menu != null) path = "<memory>:" + menu.name;
                return GetHierarchyPath(component.transform, avatar.transform) +
                       " menu=" + path +
                       " commands=" + MenuGraphHasPoseSlotCommands(menu);
            });
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            var descriptorCommands = descriptor != null &&
                                     MenuGraphHasPoseSlotCommands(descriptor.expressionsMenu);
            return "generatedRoot=" + (clonedRoot != null) +
                   ", poseSlots=" + (poseSlots != null) +
                   ", installers=" + installers.Length +
                   ", descriptorCommands=" + descriptorCommands +
                   ", entries=[" + string.Join("; ", entries) + "]";
        }

        private static bool MenuGraphContains(VRCExpressionsMenu root, VRCExpressionsMenu target)
        {
            if (root == null || target == null) return false;
            var pending = new Stack<VRCExpressionsMenu>();
            var visited = new HashSet<VRCExpressionsMenu>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var menu = pending.Pop();
                if (menu == null || !visited.Add(menu)) continue;
                if (menu == target) return true;
                foreach (var control in menu.controls)
                    if (control != null &&
                        control.type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                        control.subMenu != null)
                        pending.Push(control.subMenu);
            }
            return false;
        }

        private static bool MenuGraphHasPoseSlotCommands(VRCExpressionsMenu root)
        {
            if (root == null) return false;
            var pending = new Stack<VRCExpressionsMenu>();
            var visited = new HashSet<VRCExpressionsMenu>();
            var saveCommands = new HashSet<int>();
            var loadCommands = new HashSet<int>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var menu = pending.Pop();
                if (menu == null || !visited.Add(menu)) continue;
                foreach (var control in menu.controls)
                {
                    if (control == null) continue;
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                        control.subMenu != null)
                    {
                        pending.Push(control.subMenu);
                        continue;
                    }
                    if (control.type != VRCExpressionsMenu.Control.ControlType.Button ||
                        control.parameter == null ||
                        control.parameter.name != PoseSlotFixedSpecification.CommandParameter)
                        continue;
                    var command = (int)control.value;
                    if (command >= 1 && command <= PoseSlotFixedSpecification.SlotCount)
                        saveCommands.Add(command);
                    if (command >= PoseSlotFixedSpecification.LoadCommand(1) &&
                        command <= PoseSlotFixedSpecification.LoadCommand(PoseSlotFixedSpecification.SlotCount))
                        loadCommands.Add(command);
                }
            }
            return saveCommands.Count == PoseSlotFixedSpecification.SlotCount &&
                   loadCommands.Count == PoseSlotFixedSpecification.SlotCount;
        }

        internal static bool HasModularAvatarCommandParameter(GameObject avatar)
        {
            if (avatar == null) return false;
            return avatar.GetComponentsInChildren<ModularAvatarParameters>(true)
                .Where(component => component != null && component.parameters != null)
                .SelectMany(component => component.parameters)
                .Any(parameter => parameter.nameOrPrefix == PoseSlotFixedSpecification.CommandParameter &&
                                  parameter.syncType == ParameterSyncType.Int &&
                                  parameter.localOnly && !parameter.saved);
        }

        internal static bool HasModularAvatarPersistentController(GameObject avatar)
        {
            if (avatar == null) return false;
            return avatar.GetComponentsInChildren<ModularAvatarMergeAnimator>(true)
                .Any(component => component != null &&
                                  AssetDatabase.GetAssetPath(component.animator) ==
                                  PoseSlotPosePersistence.PersistentActionPath);
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
