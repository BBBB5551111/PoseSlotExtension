#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace PoseSlotExtensionMAStable.Editor
{
    public static class PoseSlotBuildValidator
    {
        public static bool Silent;
        public static bool LastResult { get; private set; }
        private const string ReportPath = "Assets/PoseSlotExtensionMAStable/Generated/Reports/NDMFBuildValidation.md";
        private const string SlotMenuPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Pose Slots.asset";
        private const string ClonedRootPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - Root Menu [PSE].asset";
        private const string ClonedMainPath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - Main Menu [PSE].asset";
        private const string ClonedMorePath = "Assets/PoseSlotExtensionMAStable/Generated/Menus/Poses Extension - More [PSE].asset";
        private const string DancesMenuPath = "Packages/wtf.buddyworks.posesextension/Data/Submenus/Poses Extension - Dances.asset";

        [MenuItem("Tools/Pose Slot Extension MA/Build and Verify Before Upload")]
        public static void Validate()
        {
            var descriptor = PoseSlotExtensionInstaller.ResolveTargetAvatar();
            if (descriptor == null)
            {
                if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension",
                    "Select the avatar to validate (or one of its children) in the Hierarchy.", "OK");
                return;
            }
            Validate(descriptor);
        }

        public static bool Validate(VRCAvatarDescriptor descriptor)
        {
            LastResult = false;
            GameObject builtAvatar = null;
            GameObject buildSourceAvatar = null;
            var temporaryMenus = new List<VRCExpressionsMenu>();
            try
            {
                if (descriptor == null || !descriptor.gameObject.scene.IsValid())
                    throw new InvalidOperationException("Select a valid Avatar Descriptor in the scene.");
                PoseSlotFixedSpecification.ValidateOrThrow();

                var slotRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(SlotMenuPath);
                var clonedRoot = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedRootPath);
                var clonedMain = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedMainPath);
                var clonedMore = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(ClonedMorePath);
                var dancesMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(DancesMenuPath);
                if (slotRoot == null || clonedRoot == null || clonedMain == null ||
                    clonedMore == null || dancesMenu == null)
                    throw new InvalidOperationException("Pose Slots source menus were not found.");
                if (!PoseSlotExtensionInstaller.HasBuddyWorksModularAvatar(descriptor.gameObject))
                    throw new InvalidOperationException(
                        "Install BuddyWorks Poses Extension [MA]. This stable line supports the [MA] variant only.");

                // Validate only the fixed Modular Avatar path on an in-memory clone.
                // A deep menu copy protects source assets from direct API use.
                buildSourceAvatar = UnityEngine.Object.Instantiate(descriptor.gameObject);
                buildSourceAvatar.name = descriptor.gameObject.name + " [MA Stable NDMF Validation]";
                var missingMaMenuRecoveryOk = true;
                var recoveryInstaller = buildSourceAvatar
                    .GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                    .FirstOrDefault(component => component != null &&
                        component.menuToAppend != null);
                if (recoveryInstaller == null)
                {
                    missingMaMenuRecoveryOk = false;
                }
                else
                {
                    var savedMenu = recoveryInstaller.menuToAppend;
                    recoveryInstaller.menuToAppend = null;
                    missingMaMenuRecoveryOk =
                        PoseSlotExtensionInstaller.HasBuddyWorksModularAvatar(buildSourceAvatar);
                    recoveryInstaller.menuToAppend = savedMenu;
                }
                var compatibilityApplied = PoseSlotMACompatibility.Apply(buildSourceAvatar);
                var floatGlobalOnBuildClone = HasModularAvatarParameter(buildSourceAvatar,
                    PoseSlotFixedSpecification.PoseFloatParameter, false, false);
                var menuMap = new Dictionary<VRCExpressionsMenu, VRCExpressionsMenu>();
                var temporaryRoot = CloneMenuGraph(clonedRoot, menuMap, temporaryMenus);
                ReplaceModularAvatarMenuReference(buildSourceAvatar, clonedRoot, temporaryRoot);
                builtAvatar = AvatarProcessor.ManualProcessAvatar(buildSourceAvatar);
                var builtDescriptor = builtAvatar.GetComponent<VRCAvatarDescriptor>();
                if (builtDescriptor == null) throw new InvalidOperationException("The Avatar Descriptor was not found after the NDMF build.");

                var controls = Flatten(slotRoot).ToList();
                var menus = CollectMenus(slotRoot).ToList();
                var saveCommands = controls.Where(x => x.control.name.StartsWith("Save ") && x.control.type == VRCExpressionsMenu.Control.ControlType.Button)
                    .Select(x => (int)x.control.value).OrderBy(x => x).ToArray();
                var loadCommands = controls.Where(x => x.control.name.StartsWith("Load ") && x.control.type == VRCExpressionsMenu.Control.ControlType.Button)
                    .Select(x => (int)x.control.value).OrderBy(x => x).ToArray();
                var menuOk = slotRoot != null && slotRoot.controls.Count == 2 &&
                             slotRoot.controls.Any(x => x.name == "Save") && slotRoot.controls.Any(x => x.name == "Load") &&
                             saveCommands.SequenceEqual(Enumerable.Range(1, PoseSlotFixedSpecification.SlotCount)) &&
                             loadCommands.SequenceEqual(Enumerable.Range(
                                 PoseSlotFixedSpecification.LoadCommand(1), PoseSlotFixedSpecification.SlotCount)) &&
                             menus.All(x => x.controls.Count <= 8) &&
                             !controls.Any(x => x.control.name == "Next") &&
                             controls.Count(x => x.path.StartsWith("Pose Slots/Save/Save ") && x.control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) == PoseSlotFixedSpecification.MenuRanges.Length &&
                             controls.Count(x => x.path.StartsWith("Pose Slots/Load/Load ") && x.control.type == VRCExpressionsMenu.Control.ControlType.SubMenu) == PoseSlotFixedSpecification.MenuRanges.Length;

                // Validate the processed avatar, not just the source menu assets.
                // VRCFury may namespace menu parameters during the build. The final
                // buttons must target the exact parameter consumed by the MA animator.
                var finalControls = Flatten(builtDescriptor.expressionsMenu).ToList();
                var finalSaveButtons = finalControls.Where(x => x.control.type == VRCExpressionsMenu.Control.ControlType.Button &&
                    IsNumberedSlotButton(x.control.name, "Save ")).Select(x => x.control).ToArray();
                var finalLoadButtons = finalControls.Where(x => x.control.type == VRCExpressionsMenu.Control.ControlType.Button &&
                    IsNumberedSlotButton(x.control.name, "Load ")).Select(x => x.control).ToArray();
                var finalMenuCommandBindingOk = finalSaveButtons.Length == PoseSlotFixedSpecification.SlotCount &&
                    finalLoadButtons.Length == PoseSlotFixedSpecification.SlotCount &&
                    finalSaveButtons.All(x => x.parameter != null && x.parameter.name == PoseSlotFixedSpecification.CommandParameter) &&
                    finalLoadButtons.All(x => x.parameter != null && x.parameter.name == PoseSlotFixedSpecification.CommandParameter) &&
                    finalSaveButtons.Select(x => (int)x.value).OrderBy(x => x).SequenceEqual(
                        Enumerable.Range(1, PoseSlotFixedSpecification.SlotCount)) &&
                    finalLoadButtons.Select(x => (int)x.value).OrderBy(x => x).SequenceEqual(
                        Enumerable.Range(PoseSlotFixedSpecification.LoadCommand(1), PoseSlotFixedSpecification.SlotCount));

                var action = builtDescriptor.baseAnimationLayers
                    .FirstOrDefault(x => x.type == VRCAvatarDescriptor.AnimLayerType.Action).animatorController as AnimatorController;
                var pseLayers = action == null ? new string[0] : action.layers.Where(x => x.name.StartsWith("PSE ")).Select(x => x.name).ToArray();
                var fxNames = action == null ? new HashSet<string>() : new HashSet<string>(action.parameters.Select(x => x.name));
                var renamedFloatInAction = fxNames.Any(IsRenamedPoseFloat);
                var animatorOk = pseLayers.SequenceEqual(new[] { "PSE Command" }) &&
                                 fxNames.Contains("PSE/Command") && fxNames.Contains("PSE/01/Set") &&
                                 fxNames.Contains("PSE/01/Pose") && fxNames.Contains("PSE/50/Set") && fxNames.Contains("PSE/50/Pose") &&
                                 !fxNames.Any(x => x.StartsWith("PSE/A/") || x.StartsWith("PSE/B/") || x.StartsWith("PSE/C/") || x == "PSE/DeleteCommand");
                var returnTransitionsOk = action != null && HasCommandReturnTransitions(action);
                var driverSemanticsOk = action != null && HasExpectedCopyDrivers(action);
                var randomPoseWritesPublicFloat = action != null &&
                    HasRandomPoseFloatWriter(action, PoseSlotFixedSpecification.PoseFloatParameter);
                var automaticPoseReleaseTransitions = action == null ? -1 :
                    PoseSlotPosePersistence.CountAutomaticReleaseTransitions(action);
                var explicitPoseResetTransitions = action == null ? 0 :
                    PoseSlotPosePersistence.CountExplicitResetTransitions(action);
                var posePersistenceOk = automaticPoseReleaseTransitions == 0 &&
                                        explicitPoseResetTransitions >= 1;

                var expressionParameters = builtDescriptor.expressionParameters == null
                    ? new VRCExpressionParameters.Parameter[0]
                    : builtDescriptor.expressionParameters.parameters;
                var commandParameter = expressionParameters.FirstOrDefault(x => x.name == "PSE/Command");
                var renamedCommandPresent = expressionParameters.Any(x => x.name != null &&
                    x.name.EndsWith("_" + PoseSlotFixedSpecification.CommandParameter, StringComparison.Ordinal));
                var renamedFloatParameterPresent = expressionParameters.Any(x => x.name != null && IsRenamedPoseFloat(x.name));
                var parametersOk = commandParameter != null && !commandParameter.saved && !commandParameter.networkSynced &&
                                   expressionParameters.Count(x => x.name.StartsWith("PSE/")) == 101 &&
                                   !renamedCommandPresent &&
                                   expressionParameters.Where(x => x.name.StartsWith("PSE/") && x.name != "PSE/Command")
                                       .All(x => x.saved && !x.networkSynced);

                var bindingOk = PoseSlotExtensionInstaller.HasModularAvatarMenuOverride(
                    descriptor.gameObject);
                var commandGlobalOk = PoseSlotExtensionInstaller.HasModularAvatarCommandParameter(
                    descriptor.gameObject);
                var poseFloatCompatibilityOk = compatibilityApplied && floatGlobalOnBuildClone &&
                                               !renamedFloatInAction && !renamedFloatParameterPresent &&
                                               randomPoseWritesPublicFloat;
                var integrationMenus = CollectMenus(clonedRoot).ToList();
                var poseSlotsAtTop = clonedMain.controls.Any(x =>
                    x.name == "Pose Slots" &&
                    x.type == VRCExpressionsMenu.Control.ControlType.SubMenu && x.subMenu == slotRoot);
                var dancesRemovedFromTop = !clonedMain.controls.Any(x =>
                    x.type == VRCExpressionsMenu.Control.ControlType.SubMenu && x.subMenu == dancesMenu);
                var dancesUnderMore = clonedMore.controls.Any(x =>
                    x.type == VRCExpressionsMenu.Control.ControlType.SubMenu && x.subMenu == dancesMenu);
                var poseSlotsRemovedFromMore = !clonedMore.controls.Any(x => x.name == "Pose Slots");
                var integrationMenuLimitsOk = integrationMenus.All(x => x.controls.Count <= 8);
                var placementOk = poseSlotsAtTop && dancesRemovedFromTop && dancesUnderMore &&
                                  poseSlotsRemovedFromMore && integrationMenuLimitsOk;
                LastResult = menuOk && finalMenuCommandBindingOk && animatorOk && returnTransitionsOk &&
                             driverSemanticsOk && parametersOk && bindingOk && commandGlobalOk &&
                             poseFloatCompatibilityOk && placementOk && posePersistenceOk &&
                             missingMaMenuRecoveryOk;

                var report = new StringBuilder();
                report.AppendLine("# NDMF Build Validation");
                report.AppendLine();
                report.AppendLine("- Result: " + (LastResult ? "PASS" : "FAIL"));
                report.AppendLine("- Fixed contract version: " + PoseSlotFixedSpecification.ContractVersion);
                report.AppendLine("- Target avatar: " + descriptor.gameObject.name);
                report.AppendLine("- BuddyWorks integration: ModularAvatar (fixed)");
                report.AppendLine("- Menu structure and limits: " + menuOk);
                report.AppendLine("- Final built menu targets PSE/Command: " + finalMenuCommandBindingOk);
                report.AppendLine("- Independent range folders/no Next: " + (!controls.Any(x => x.control.name == "Next")));
                report.AppendLine("- Save commands 01-50: " + (saveCommands.Length == PoseSlotFixedSpecification.SlotCount));
                report.AppendLine("- Load commands 01-50: " + (loadCommands.Length == PoseSlotFixedSpecification.SlotCount));
                report.AppendLine("- Animator structure/no stale A-B-C: " + animatorOk);
                report.AppendLine("- Installed playable layer: Action");
                report.AppendLine("- Save/Load return transitions: " + returnTransitionsOk);
                report.AppendLine("- Direct two-field Save/Load drivers: " + driverSemanticsOk);
                report.AppendLine("- Local saved parameters: " + parametersOk);
                report.AppendLine("- BuddyWorks menu binding: " + bindingOk);
                report.AppendLine("- MA missing previous menu reference recovery: " + missingMaMenuRecoveryOk);
                report.AppendLine("- PSE/Command is global/local-only: " + commandGlobalOk);
                report.AppendLine("- Renamed VF*_PSE/Command absent: " + (!renamedCommandPresent));
                report.AppendLine("- PE/Float globalized on temporary build clone: " + (compatibilityApplied && floatGlobalOnBuildClone));
                report.AppendLine("- Random Pose writes public PE/Float: " + randomPoseWritesPublicFloat);
                report.AppendLine("- Automatic pose-release transitions removed: " + posePersistenceOk);
                report.AppendLine("- Remaining automatic pose-release transitions: " + automaticPoseReleaseTransitions);
                report.AppendLine("- Explicit BuddyWorks Reset transitions retained: " + explicitPoseResetTransitions);
                report.AppendLine("- Renamed VF*_PE/Float absent from Action: " + (!renamedFloatInAction));
                report.AppendLine("- Renamed VF*_PE/Float absent from expression parameters: " + (!renamedFloatParameterPresent));
                report.AppendLine("- Pose Slots at BuddyWorks top level: " + poseSlotsAtTop);
                report.AppendLine("- Dances moved under More: " + (dancesRemovedFromTop && dancesUnderMore));
                report.AppendLine("- Pose Slots absent under More: " + poseSlotsRemovedFromMore);
                report.AppendLine("- Integration menu limits: " + integrationMenuLimitsOk);
                report.AppendLine("- Maximum controls in one menu: " +
                    (integrationMenus.Count == 0 ? 0 : integrationMenus.Max(x => x.controls.Count)));
                report.AppendLine("- PSE expression parameters: " + expressionParameters.Count(x => x.name.StartsWith("PSE/")));
                report.AppendLine();
                report.AppendLine("## PSE layers");
                foreach (var layer in pseLayers) report.AppendLine("- `" + layer + "`");
                report.AppendLine();
                report.AppendLine("## Pose Slots menu tree");
                foreach (var entry in Flatten(slotRoot)) report.AppendLine("- `" + entry.path + "`");
                File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);

                if (!LastResult) throw new InvalidOperationException("Post-NDMF build validation failed. Do not upload. Report: " + ReportPath);
                Debug.Log("[PoseSlotExtension] NDMF post-build validation PASS: " + ReportPath);
                if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension", "Post-NDMF build validation: PASS", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!Silent) EditorUtility.DisplayDialog("Pose Slot Extension - Validation FAILED", exception.Message, "OK");
                if (Silent) throw;
            }
            finally
            {
                if (builtAvatar != null) UnityEngine.Object.DestroyImmediate(builtAvatar);
                if (buildSourceAvatar != null) UnityEngine.Object.DestroyImmediate(buildSourceAvatar);
                // VRCFury may persist some validation copies into NDMF's temporary
                // asset area. Those belong to CleanTemporaryAssets; calling the
                // normal DestroyImmediate overload on a persistent object logs a
                // data-loss error. Destroy only copies that remain memory-only.
                foreach (var menu in temporaryMenus.Where(x => x != null && !EditorUtility.IsPersistent(x)))
                    UnityEngine.Object.DestroyImmediate(menu);
                AvatarProcessor.CleanTemporaryAssets();
            }
            return LastResult;
        }

        private static VRCExpressionsMenu CloneMenuGraph(VRCExpressionsMenu source,
            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> map, List<VRCExpressionsMenu> temporaryMenus)
        {
            if (source == null) return null;
            if (map.TryGetValue(source, out var existing)) return existing;
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + " [PSE Validation Copy]";
            clone.hideFlags = HideFlags.HideAndDontSave;
            map[source] = clone;
            temporaryMenus.Add(clone);
            foreach (var control in clone.controls)
                if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                    control.subMenu = CloneMenuGraph(control.subMenu, map, temporaryMenus);
            return clone;
        }

        private static void ReplaceVrcFuryMenuReference(GameObject avatar, VRCExpressionsMenu source,
            VRCExpressionsMenu replacement)
        {
            var replaced = false;
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                var componentChanged = false;
                if (!property.Next(true)) continue;
                do
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != source) continue;
                    property.objectReferenceValue = replacement;
                    componentChanged = true;
                    replaced = true;
                } while (property.Next(true));
                if (componentChanged) serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            if (!replaced)
                throw new InvalidOperationException("VRCFury Pose Extension menu reference was not found on validation clone.");
        }

        private static void ReplaceModularAvatarMenuReference(GameObject avatar,
            VRCExpressionsMenu source, VRCExpressionsMenu replacement)
        {
            var installers = avatar.GetComponentsInChildren<ModularAvatarMenuInstaller>(true)
                .Where(component => component != null && component.menuToAppend == source)
                .ToArray();
            if (installers.Length == 0)
                throw new InvalidOperationException(
                    "Modular Avatar Pose Extension menu reference was not found on validation clone.");
            foreach (var installer in installers)
                installer.menuToAppend = replacement;
        }

        private static bool HasModularAvatarParameter(GameObject avatar, string name,
            bool localOnly, bool saved)
        {
            return avatar.GetComponentsInChildren<ModularAvatarParameters>(true)
                .Where(component => component != null && component.parameters != null)
                .SelectMany(component => component.parameters)
                .Any(parameter => parameter.nameOrPrefix == name &&
                                  parameter.localOnly == localOnly && parameter.saved == saved);
        }

        private static bool IsNumberedSlotButton(string name, string prefix)
        {
            if (name == null || !name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            return int.TryParse(name.Substring(prefix.Length), out var slot) &&
                   slot >= 1 && slot <= PoseSlotFixedSpecification.SlotCount;
        }

        private static void RunVrcFury(GameObject avatar)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var objectType = assemblies.Select(a => a.GetType("VF.Utils.VFGameObject")).FirstOrDefault(t => t != null);
            var builderType = assemblies.Select(a => a.GetType("VF.Builder.VRCFuryBuilder")).FirstOrDefault(t => t != null);
            if (objectType == null || builderType == null)
                throw new InvalidOperationException("VRCFury validation API was not found.");
            var constructor = objectType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(GameObject) }, null);
            var runMain = builderType.GetMethod("RunMain", BindingFlags.Static | BindingFlags.NonPublic);
            if (constructor == null || runMain == null)
                throw new InvalidOperationException("VRCFury validation entry point was not found.");
            var wrapped = constructor.Invoke(new object[] { avatar });
            try { runMain.Invoke(null, new[] { wrapped }); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }

        private static IEnumerable<(VRCExpressionsMenu.Control control, string path)> Flatten(VRCExpressionsMenu root)
        {
            if (root == null) yield break;
            var stack = new Stack<(VRCExpressionsMenu menu, string path)>();
            var visited = new HashSet<VRCExpressionsMenu>();
            stack.Push((root, root.name));
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current.menu == null || !visited.Add(current.menu)) continue;
                foreach (var control in current.menu.controls)
                {
                    var path = current.path + "/" + control.name;
                    yield return (control, path);
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null)
                        stack.Push((control.subMenu, path));
                }
            }
        }

        private static IEnumerable<VRCExpressionsMenu> CollectMenus(VRCExpressionsMenu root)
        {
            if (root == null) yield break;
            var stack = new Stack<VRCExpressionsMenu>();
            var visited = new HashSet<VRCExpressionsMenu>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var menu = stack.Pop();
                if (menu == null || !visited.Add(menu)) continue;
                yield return menu;
                foreach (var control in menu.controls)
                    if (control.type == VRCExpressionsMenu.Control.ControlType.SubMenu && control.subMenu != null) stack.Push(control.subMenu);
            }
        }

        private static bool HasVrcFuryBinding(GameObject avatar, VRCExpressionsMenu clonedRoot)
        {
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

        private static bool HasVrcFuryGlobalParameter(GameObject avatar, string parameterName)
        {
            foreach (var component in avatar.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName.IndexOf("VRCFury", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var property = new SerializedObject(component).GetIterator();
                if (!property.Next(true)) continue;
                do
                {
                    if (!property.isArray || property.propertyType == SerializedPropertyType.String ||
                        !property.propertyPath.EndsWith(".globalParams", StringComparison.Ordinal)) continue;
                    for (var i = 0; i < property.arraySize; i++)
                        if (property.GetArrayElementAtIndex(i).stringValue == parameterName) return true;
                } while (property.Next(true));
            }
            return false;
        }

        private static bool HasCommandReturnTransitions(AnimatorController controller)
        {
            var layer = controller.layers.FirstOrDefault(x => x.name == "PSE Command");
            if (layer == null || layer.stateMachine == null) return false;
            var states = layer.stateMachine.states.Select(x => x.state).ToDictionary(x => x.name);
            for (var slot = 1; slot <= PoseSlotFixedSpecification.SlotCount; slot++)
            {
                foreach (var name in new[] { $"PSE_Save_{slot:00}", $"PSE_Load_{slot:00}" })
                {
                    if (!states.TryGetValue(name, out var state)) return false;
                    if (!state.transitions.Any(t => t.conditions.Any(c =>
                            c.parameter == "PSE/Command" && c.mode == AnimatorConditionMode.Equals && Math.Abs(c.threshold) < 0.001f)))
                        return false;
                }
            }
            return true;
        }

        private static bool HasExpectedCopyDrivers(AnimatorController controller)
        {
            var layer = controller.layers.FirstOrDefault(x => x.name == "PSE Command");
            if (layer == null || layer.stateMachine == null) return false;
            var states = layer.stateMachine.states.Select(x => x.state).ToDictionary(x => x.name);
            for (var slot = 1; slot <= PoseSlotFixedSpecification.SlotCount; slot++)
            {
                if (!HasCopyPair(states, $"PSE_Save_{slot:00}", "PE/Set", $"PSE/{slot:00}/Set", "PE/Float", $"PSE/{slot:00}/Pose")) return false;
                if (!HasLoadCopy(states, slot)) return false;
            }
            return true;
        }

        private static bool IsRenamedPoseFloat(string name) =>
            name != null && name != "PE/Float" && name.EndsWith("_PE/Float", StringComparison.Ordinal);

        private static bool HasRandomPoseFloatWriter(AnimatorController controller, string destination)
        {
            return controller.layers.Any(layer => StateMachines(layer.stateMachine)
                .SelectMany(machine => machine.states.Select(state => state.state))
                .SelectMany(state => state.behaviours.OfType<VRCAvatarParameterDriver>())
                .SelectMany(driver => driver.parameters)
                .Any(parameter => parameter.name == destination &&
                                  parameter.type == VRCAvatarParameterDriver.ChangeType.Random &&
                                  parameter.valueMin >= 0 && parameter.valueMax > parameter.valueMin));
        }

        private static IEnumerable<AnimatorStateMachine> StateMachines(AnimatorStateMachine root)
        {
            if (root == null) yield break;
            yield return root;
            foreach (var child in root.stateMachines)
                foreach (var nested in StateMachines(child.stateMachine))
                    yield return nested;
        }

        private static bool HasCopyPair(Dictionary<string, AnimatorState> states, string stateName,
            string source1, string destination1, string source2, string destination2)
        {
            if (!states.TryGetValue(stateName, out var state)) return false;
            var drivers = state.behaviours.OfType<VRCAvatarParameterDriver>().ToArray();
            if (drivers.Length != 1) return false;
            var parameters = drivers[0].parameters;
            if (parameters.Any(x => x.name == "PSE/Command")) return false;
            return parameters.Count == 2 &&
                   parameters.Any(x => x.type == VRCAvatarParameterDriver.ChangeType.Copy && x.source == source1 && x.name == destination1) &&
                   parameters.Any(x => x.type == VRCAvatarParameterDriver.ChangeType.Copy && x.source == source2 && x.name == destination2);
        }

        private static bool HasLoadCopy(Dictionary<string, AnimatorState> states, int slot)
        {
            if (!states.TryGetValue($"PSE_Load_{slot:00}", out var load)) return false;
            var drivers = load.behaviours.OfType<VRCAvatarParameterDriver>().ToArray();
            var parameters = drivers.SelectMany(x => x.parameters).ToArray();
            return load.behaviours.Length == 1 && drivers.Length == 1 && parameters.Length == 2 &&
                   !parameters.Any(x => x.name == "PSE/Command") &&
                   parameters.Any(x => x.type == VRCAvatarParameterDriver.ChangeType.Copy && x.source == $"PSE/{slot:00}/Set" && x.name == "PE/Set") &&
                   parameters.Any(x => x.type == VRCAvatarParameterDriver.ChangeType.Copy && x.source == $"PSE/{slot:00}/Pose" && x.name == "PE/Float");
        }
    }
}
#endif
