#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace PoseSlotExtensionMAStable.Editor
{
    public static class PoseSlotExtensionGenerator
    {
        public static bool Silent;
        private const string Root = "Assets/PoseSlotExtensionMAStable";
        private const string Generated = Root + "/Generated";
        private const string AnimatorPath = Generated + "/Animator/PoseSlotExtension.controller";
        private const string MenuRootPath = Generated + "/Menus/Pose Slots.asset";
        private const string PrefabPath = Generated + "/Prefab/PoseSlotExtension.prefab";
        private const string ReportFolder = Generated + "/Reports";
        private const string Command = PoseSlotFixedSpecification.CommandParameter;

        [MenuItem("Tools/Pose Slot Extension MA/Generate 50 Save Load Slots")]
        public static void Generate()
        {
            try
            {
                PoseSlotFixedSpecification.ValidateOrThrow();
                EnsureFolders();
                ValidateDependencies();
                GeneratePersistentBuddyController();
                var controller = GenerateController();
                var menu = GenerateMenus();
                GeneratePrefab(controller);
                WriteDocumentation(controller, menu);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[PoseSlotExtension] Generated 50 overwrite Save/Load slots: " + PrefabPath);
                if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension", "Generated Save 01-50 / Load 01-50.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension - Error", exception.Message, "OK");
                if (Silent) throw;
            }
        }

        private static void ValidateDependencies()
        {
            const string maPrefab =
                "Packages/wtf.buddyworks.posesextension/Data/BUDDYWORKS Poses Extension [MA].prefab";
            const string vrcFuryPrefab =
                "Packages/wtf.buddyworks.posesextension/Data/BUDDYWORKS Poses Extension [VRCF].prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(maPrefab) == null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(vrcFuryPrefab) == null)
                throw new InvalidOperationException("The BUDDYWORKS Poses Extension package was not found.");
        }

        private static void GeneratePersistentBuddyController()
        {
            var source = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                PoseSlotPosePersistence.BuddyActionSourcePath);
            if (source == null)
                throw new InvalidOperationException("BUDDYWORKS Poses Extension Action controller was not found.");

            DeleteIfPresent(PoseSlotPosePersistence.PersistentActionPath);
            if (!AssetDatabase.CopyAsset(PoseSlotPosePersistence.BuddyActionSourcePath,
                    PoseSlotPosePersistence.PersistentActionPath))
                throw new InvalidOperationException("BUDDYWORKS Action controller could not be copied safely.");
            AssetDatabase.ImportAsset(PoseSlotPosePersistence.PersistentActionPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                PoseSlotPosePersistence.PersistentActionPath);
            if (controller == null)
                throw new InvalidOperationException("Persistent BUDDYWORKS Action controller could not be loaded.");

            var patched = PoseSlotPosePersistence.RemoveAutomaticReleaseTransitions(controller);
            if (patched.GroundedTransitions != PoseSlotFixedSpecification.ExpectedGroundedReleaseTransitions ||
                patched.GoEmoteTransitions != PoseSlotFixedSpecification.ExpectedGoEmoteReleaseTransitions ||
                patched.FavoriteShowcaseTransitions != PoseSlotFixedSpecification.ExpectedFavoriteShowcaseReleaseTransitions ||
                patched.EyelookMigrationTransitions != PoseSlotFixedSpecification.ExpectedEyelookMigrationReleaseTransitions ||
                patched.OtherTransitions != PoseSlotFixedSpecification.ExpectedOtherReleaseTransitions ||
                PoseSlotPosePersistence.CountAutomaticReleaseTransitions(controller) != 0 ||
                PoseSlotPosePersistence.CountExplicitResetTransitions(controller) < 1)
                throw new InvalidOperationException(
                    $"BUDDYWORKS pose persistence patch did not match the verified structure. " +
                    $"Grounded={patched.GroundedTransitions}, GoEmote={patched.GoEmoteTransitions}, " +
                    $"FavoriteShowcase={patched.FavoriteShowcaseTransitions}, " +
                    $"EyelookMigration={patched.EyelookMigrationTransitions}, " +
                    $"Other={patched.OtherTransitions}. " +
                    string.Join(" | ", patched.OtherTransitionDetails));
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorController GenerateController()
        {
            DeleteIfPresent(AnimatorPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
            AddParameter(controller, Command, AnimatorControllerParameterType.Int, 0);
            AddParameter(controller, PoseSlotFixedSpecification.PoseSetParameter, AnimatorControllerParameterType.Int, 1);
            AddParameter(controller, PoseSlotFixedSpecification.PoseFloatParameter, AnimatorControllerParameterType.Float, 0);
            for (var slot = 1; slot <= PoseSlotFixedSpecification.SlotCount; slot++)
            {
                AddParameter(controller, SnapshotSet(slot), AnimatorControllerParameterType.Int, 1);
                AddParameter(controller, SnapshotPose(slot), AnimatorControllerParameterType.Float, 0);
            }

            controller.RemoveLayer(0);
            BuildCommandLayer(controller);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildCommandLayer(AnimatorController controller)
        {
            var machine = new AnimatorStateMachine { name = "PSE_Command", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(machine, controller);
            controller.AddLayer(new AnimatorControllerLayer { name = "PSE Command", defaultWeight = 1, stateMachine = machine });
            var idle = AddState(machine, "PSE_Idle", Vector3.zero);
            machine.defaultState = idle;

            for (var slot = 1; slot <= PoseSlotFixedSpecification.SlotCount; slot++)
            {
                var save = AddState(machine, $"PSE_Save_{slot:00}", new Vector3(300, slot * 45));
                var saveTransition = idle.AddTransition(save);
                AddCondition(saveTransition, AnimatorConditionMode.Equals, PoseSlotFixedSpecification.SaveCommand(slot), Command);
                AddCondition(saveTransition, AnimatorConditionMode.Greater, 0, PoseSlotFixedSpecification.PoseSetParameter);
                AddDriver(save,
                    Copy(PoseSlotFixedSpecification.PoseSetParameter, SnapshotSet(slot)),
                    Copy(PoseSlotFixedSpecification.PoseFloatParameter, SnapshotPose(slot)));
                var saveReturn = save.AddTransition(idle);
                AddCondition(saveReturn, AnimatorConditionMode.Equals, 0, Command);

                var load = AddState(machine, $"PSE_Load_{slot:00}", new Vector3(700, slot * 45));
                var loadTransition = idle.AddTransition(load);
                AddCondition(loadTransition, AnimatorConditionMode.Equals, PoseSlotFixedSpecification.LoadCommand(slot), Command);
                AddDriver(load,
                    Copy(SnapshotSet(slot), PoseSlotFixedSpecification.PoseSetParameter),
                    Copy(SnapshotPose(slot), PoseSlotFixedSpecification.PoseFloatParameter));
                var loadReturn = load.AddTransition(idle);
                AddCondition(loadReturn, AnimatorConditionMode.Equals, 0, Command);
            }
        }

        private static VRCExpressionsMenu GenerateMenus()
        {
            DeleteMenuAssets();
            var root = CreateMenu(MenuRootPath);
            var save = CreateMenu(Generated + "/Menus/Save.asset");
            var load = CreateMenu(Generated + "/Menus/Load.asset");
            AddSubMenu(root, "Save", save);
            AddSubMenu(root, "Load", load);

            foreach (var range in PoseSlotFixedSpecification.MenuRanges)
            {
                AddSubMenu(save, $"Save {range.Start:00}-{range.End:00}", CreateSlotRangeMenu(true, range.Start, range.End));
                AddSubMenu(load, $"Load {range.Start:00}-{range.End:00}", CreateSlotRangeMenu(false, range.Start, range.End));
            }
            return root;
        }

        private static VRCExpressionsMenu CreateSlotRangeMenu(bool saving, int start, int end)
        {
            var kind = saving ? "Save" : "Load";
            var menu = CreateMenu($"{Generated}/Menus/{kind} {start:00}-{end:00}.asset");
            for (var slot = start; slot <= end; slot++) AddSlotButton(menu, kind, saving, slot);
            // CreateAsset writes the initial empty object immediately. Persist each
            // completed leaf before creating the next asset so an intervening asset
            // import/build cannot reload it from that empty on-disk representation.
            AssetDatabase.SaveAssetIfDirty(menu);
            return menu;
        }

        private static void AddSlotButton(VRCExpressionsMenu menu, string kind, bool saving, int slot)
        {
            AddButton(menu, $"{kind} {slot:00}", saving
                ? PoseSlotFixedSpecification.SaveCommand(slot)
                : PoseSlotFixedSpecification.LoadCommand(slot));
        }

        private static void GeneratePrefab(AnimatorController controller)
        {
            var root = new GameObject("PoseSlotExtensionMAStable");
            try
            {
                var merge = root.AddComponent<ModularAvatarMergeAnimator>();
                merge.animator = controller;
                // BUDDYWORKS pose selection and its working Save/Load logic live in
                // the Action playable layer. Keep the shortcut driver in that same
                // layer so PE/Set and PE/Float are read and written in the identical
                // animator context.
                merge.layerType = VRCAvatarDescriptor.AnimLayerType.Action;
                merge.mergeAnimatorMode = MergeAnimatorMode.Append;
                merge.pathMode = MergeAnimatorPathMode.Absolute;
                merge.matchAvatarWriteDefaults = true;
                var parameters = root.AddComponent<ModularAvatarParameters>();
                parameters.parameters = BuildParameterConfigs();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static List<ParameterConfig> BuildParameterConfigs()
        {
            // The prefab carries only the 100 saved slot values. The installer adds
            // local PSE/Command for [MA], while [VRCF] supplies it through its private
            // VRCFury parameters asset. Keeping it mode-specific avoids duplicates.
            var result = new List<ParameterConfig>();
            for (var slot = 1; slot <= PoseSlotFixedSpecification.SlotCount; slot++)
            {
                result.Add(Config(SnapshotSet(slot), ParameterSyncType.Int, true, true, 1));
                result.Add(Config(SnapshotPose(slot), ParameterSyncType.Float, true, true, 0));
            }
            return result;
        }

        private static ParameterConfig Config(string name, ParameterSyncType type, bool saved, bool localOnly, float defaultValue)
        {
            return new ParameterConfig
            {
                nameOrPrefix = name, syncType = type, saved = saved, localOnly = localOnly,
                defaultValue = defaultValue, hasExplicitDefaultValue = true
            };
        }

        private static void WriteDocumentation(AnimatorController controller, VRCExpressionsMenu menu)
        {
            var report = new StringBuilder();
            report.AppendLine("# Pose Slot Extension - Generated Specification");
            report.AppendLine();
            report.AppendLine("- Fixed contract version: " + PoseSlotFixedSpecification.ContractVersion);
            report.AppendLine("- Slots: 50 (single bank)");
            report.AppendLine("- Save behavior: direct overwrite");
            report.AppendLine("- Stored fields: `PE/Set` (Int) and `PE/Float` (Float)");
            report.AppendLine("- Slot data: saved locally, not network synchronized");
            report.AppendLine("- Load target: existing network-synchronized `PE/Set` and `PE/Float`");
            report.AppendLine("- Standard Slot A/B/C adjustment fields are not copied");
            report.AppendLine("- Active pose persists until the explicit BUDDYWORKS Reset command");
            report.AppendLine("- BUDDYWORKS package controller remains untouched; a generated copy is used at build time");
            report.AppendLine("- Delete/history/A-B-C banks: none");
            report.AppendLine("- Animator layers: " + controller.layers.Length);
            report.AppendLine("- Root menu controls: " + menu.controls.Count);
            File.WriteAllText(ReportFolder + "/GeneratedSpecification.md", report.ToString(), new UTF8Encoding(false));
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            var state = machine.AddState(name, position);
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddCondition(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold, string parameter)
        {
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddDriver(AnimatorState state, params VRCAvatarParameterDriver.Parameter[] parameters)
        {
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.localOnly = true;
            driver.parameters = parameters.ToList();
        }

        private static VRCAvatarParameterDriver.Parameter Copy(string source, string destination) =>
            new VRCAvatarParameterDriver.Parameter { source = source, name = destination, type = VRCAvatarParameterDriver.ChangeType.Copy };
        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
        {
            if (controller.parameters.Any(x => x.name == name)) return;
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = name, type = type, defaultFloat = value, defaultInt = (int)value, defaultBool = value != 0
            });
        }

        private static VRCExpressionsMenu CreateMenu(string path)
        {
            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(menu, path);
            return menu;
        }

        private static void AddSubMenu(VRCExpressionsMenu parent, string name, VRCExpressionsMenu child)
        {
            parent.controls.Add(new VRCExpressionsMenu.Control
            {
                name = name, type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = child
            });
            EditorUtility.SetDirty(parent);
        }

        private static void AddButton(VRCExpressionsMenu menu, string name, int command)
        {
            menu.controls.Add(new VRCExpressionsMenu.Control
            {
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.Button,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = Command },
                value = command
            });
            EditorUtility.SetDirty(menu);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root); EnsureFolder(Root + "/Editor"); EnsureFolder(Generated);
            EnsureFolder(Generated + "/Animator"); EnsureFolder(Generated + "/Menus");
            EnsureFolder(Generated + "/Prefab"); EnsureFolder(ReportFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }

        private static void DeleteMenuAssets()
        {
            if (!AssetDatabase.IsValidFolder(Generated + "/Menus")) return;
            foreach (var guid in AssetDatabase.FindAssets("t:VRCExpressionsMenu", new[] { Generated + "/Menus" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("[PSE]")) continue;
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void DeleteIfPresent(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }

        private static string SnapshotSet(int slot) => PoseSlotFixedSpecification.SnapshotSet(slot);
        private static string SnapshotPose(int slot) => PoseSlotFixedSpecification.SnapshotPose(slot);
    }
}
#endif
