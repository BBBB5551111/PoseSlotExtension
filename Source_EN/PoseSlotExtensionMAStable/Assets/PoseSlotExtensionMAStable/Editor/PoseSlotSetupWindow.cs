#if UNITY_EDITOR
using System;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace PoseSlotExtensionMAStable.Editor
{
    internal sealed class PoseSlotSetupWindow : EditorWindow
    {
        private const string GeneratedPrefabPath =
            "Assets/PoseSlotExtensionMAStable/Generated/Prefab/PoseSlotExtension.prefab";
        private const string BuddyWorksPrefabPath =
            "Packages/wtf.buddyworks.posesextension/Data/BUDDYWORKS Poses Extension [MA].prefab";
        private VRCAvatarDescriptor target;
        private Vector2 scroll;
        private string lastResult;
        private MessageType lastResultType = MessageType.Info;

        internal static void Open()
        {
            var window = GetWindow<PoseSlotSetupWindow>();
            window.titleContent = new GUIContent("Pose Slots Setup - MA Stable");
            window.minSize = new Vector2(520, 520);
            window.Show();
        }

        private void OnEnable()
        {
            target = PoseSlotExtensionInstaller.ResolveTargetAvatar();
        }

        private void OnSelectionChange()
        {
            var selected = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (selected != null)
            {
                target = selected;
                Repaint();
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Pose Slot Extension - MA Stable", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Verified fixed contract v" + PoseSlotFixedSpecification.ContractVersion,
                EditorStyles.miniLabel);
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Save 01-50 overwrites the selected slot with the current BUDDYWORKS pose (PE/Set + PE/Float) only, " +
                "and Load 01-50 restores it. Saved values are local only, but the pose shown after Load " +
                "uses BUDDYWORKS' own sync path, so other players see it. Jumping does not clear the pose; " +
                "only the BUDDYWORKS Reset does.", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Target Avatar", EditorStyles.boldLabel);
            target = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Avatar Descriptor", target, typeof(VRCAvatarDescriptor), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Hierarchy Selection"))
                {
                    var selected = Selection.activeGameObject == null
                        ? null
                        : Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
                    if (selected != null) target = selected;
                }
                if (GUILayout.Button("Auto-detect Compatible Avatar"))
                {
                    var compatible = PoseSlotExtensionInstaller.FindCompatibleAvatars().ToArray();
                    if (compatible.Length == 1) target = compatible[0];
                    else
                    {
                        lastResult = compatible.Length == 0
                            ? "No avatar with BUDDYWORKS Poses Extension was found."
                            : "Multiple candidates found. Select the target in the Hierarchy.";
                        lastResultType = MessageType.Warning;
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pre-install Checks", EditorStyles.boldLabel);
            var hasSupportedBuddy = target != null &&
                                    PoseSlotExtensionInstaller.HasBuddyWorksModularAvatar(target.gameObject);
            DrawStatus("Avatar Descriptor", target != null);
            DrawStatus("BUDDYWORKS Poses Extension [MA]", hasSupportedBuddy);
            DrawStatus("Modular Avatar", typeof(ModularAvatarMergeAnimator) != null);
            DrawStatus("BUDDYWORKS package original", AssetDatabase.LoadAssetAtPath<GameObject>(BuddyWorksPrefabPath) != null);
            DrawStatus("Generated assets", AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath) != null);
            DrawStatus("Installed on target", PoseSlotExtensionInstaller.IsInstalled(target));

            EditorGUILayout.Space(12);
            var ready = target != null && target.gameObject.scene.IsValid() && hasSupportedBuddy;
            using (new EditorGUI.DisabledScope(!ready || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.45f, 0.9f, 0.55f);
                if (GUILayout.Button("Generate, Install and Validate", GUILayout.Height(42))) RunFullSetup();
                GUI.backgroundColor = oldColor;

                using (new EditorGUI.DisabledScope(!PoseSlotExtensionInstaller.IsInstalled(target)))
                    if (GUILayout.Button("Re-validate Current Installation", GUILayout.Height(28))) RunValidationOnly();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Safety: the BUDDYWORKS package original is never edited. The required Action Controller and menus are " +
                "copied into the Generated folder and modified there. Existing Expression " +
                "Parameters are never modified in place; they are copied into PrivateParameters when migration is needed.", MessageType.None);

            if (!string.IsNullOrEmpty(lastResult))
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(lastResult, lastResultType);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunFullSetup()
        {
            RunOperation(() =>
            {
                PoseSlotFixedSpecification.ValidateOrThrow();
                PoseSlotExtensionGenerator.Generate();
                if (!PoseSlotExtensionInstaller.Install(target))
                    throw new InvalidOperationException("The installation did not complete.");
                if (!PoseSlotBuildValidator.Validate(target))
                    throw new InvalidOperationException("Post-NDMF build validation FAILED. Do not upload.");
                lastResult = "PASS: 50 slots were generated and installed. The menu, Animator, " +
                             "parameters and BUDDYWORKS integration were validated after the NDMF build. Ready to upload.";
                lastResultType = MessageType.Info;
            });
        }

        private void RunValidationOnly()
        {
            RunOperation(() =>
            {
                if (!PoseSlotBuildValidator.Validate(target))
                    throw new InvalidOperationException("Post-NDMF build validation FAILED. Do not upload.");
                lastResult = "PASS: the current installation matches fixed contract v" +
                             PoseSlotFixedSpecification.ContractVersion + ".";
                lastResultType = MessageType.Info;
            });
        }

        private void RunOperation(Action operation)
        {
            try
            {
                PoseSlotExtensionGenerator.Silent = true;
                PoseSlotExtensionInstaller.Silent = true;
                PoseSlotBuildValidator.Silent = true;
                operation();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                lastResult = "FAIL: " + exception.Message;
                lastResultType = MessageType.Error;
            }
            finally
            {
                PoseSlotExtensionGenerator.Silent = false;
                PoseSlotExtensionInstaller.Silent = false;
                PoseSlotBuildValidator.Silent = false;
                Repaint();
            }
        }

        private static void DrawStatus(string label, bool ok, string failureLabel = "not found")
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(ok ? "✓" : "—", GUILayout.Width(22));
                EditorGUILayout.LabelField(label, ok ? "OK" : failureLabel);
            }
        }
    }
}
#endif
