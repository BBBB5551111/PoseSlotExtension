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
            EditorGUILayout.LabelField("実機確認済み固定仕様 v" + PoseSlotFixedSpecification.ContractVersion,
                EditorStyles.miniLabel);
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Save 01-50は現在のBuddyWorksポーズ（PE/Set + PE/Float）だけを上書き保存し、" +
                "Load 01-50で復元します。保存値はローカル専用ですが、Load後のポーズ表示は" +
                "BuddyWorks本来の同期経路を使うため他人にも見えます。ジャンプ等では解除されず、" +
                "BuddyWorksのResetで解除されます。", MessageType.Info);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("対象アバター", EditorStyles.boldLabel);
            target = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                "Avatar Descriptor", target, typeof(VRCAvatarDescriptor), true);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Hierarchyの選択から取得"))
                {
                    var selected = Selection.activeGameObject == null
                        ? null
                        : Selection.activeGameObject.GetComponentInParent<VRCAvatarDescriptor>();
                    if (selected != null) target = selected;
                }
                if (GUILayout.Button("互換アバターを自動検出"))
                {
                    var compatible = PoseSlotExtensionInstaller.FindCompatibleAvatars().ToArray();
                    if (compatible.Length == 1) target = compatible[0];
                    else
                    {
                        lastResult = compatible.Length == 0
                            ? "BuddyWorks Poses Extensionを持つアバターが見つかりません。"
                            : "候補が複数あります。Hierarchyで対象を選択してください。";
                        lastResultType = MessageType.Warning;
                    }
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("導入前チェック", EditorStyles.boldLabel);
            var hasSupportedBuddy = target != null &&
                                    PoseSlotExtensionInstaller.HasBuddyWorksModularAvatar(target.gameObject);
            DrawStatus("Avatar Descriptor", target != null);
            DrawStatus("BuddyWorks Poses Extension [MA]", hasSupportedBuddy);
            DrawStatus("Modular Avatar", typeof(ModularAvatarMergeAnimator) != null);
            DrawStatus("BuddyWorks Package原本", AssetDatabase.LoadAssetAtPath<GameObject>(BuddyWorksPrefabPath) != null);
            DrawStatus("生成済みアセット", AssetDatabase.LoadAssetAtPath<GameObject>(GeneratedPrefabPath) != null);
            DrawStatus("対象へ導入済み", PoseSlotExtensionInstaller.IsInstalled(target));

            EditorGUILayout.Space(12);
            var ready = target != null && target.gameObject.scene.IsValid() && hasSupportedBuddy;
            using (new EditorGUI.DisabledScope(!ready || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.45f, 0.9f, 0.55f);
                if (GUILayout.Button("生成・導入・検証", GUILayout.Height(42))) RunFullSetup();
                GUI.backgroundColor = oldColor;

                using (new EditorGUI.DisabledScope(!PoseSlotExtensionInstaller.IsInstalled(target)))
                    if (GUILayout.Button("現在の導入内容を再検証", GUILayout.Height(28))) RunValidationOnly();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "安全性: BuddyWorksのPackage原本は編集しません。必要なAction Controllerとメニューを" +
                "Generated配下へ複製して加工します。旧PSEパラメータがある場合も、元のExpression " +
                "Parametersを直接変更せずPrivateParametersへ複製して移行します。", MessageType.None);

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
                    throw new InvalidOperationException("導入処理が完了しませんでした。");
                if (!PoseSlotBuildValidator.Validate(target))
                    throw new InvalidOperationException("NDMFビルド後検証がFAILでした。アップロードしないでください。");
                lastResult = "PASS: 50スロットを生成・導入し、NDMFビルド後のメニュー、Animator、" +
                             "パラメータ、BuddyWorks連携を検証しました。アップロード可能です。";
                lastResultType = MessageType.Info;
            });
        }

        private void RunValidationOnly()
        {
            RunOperation(() =>
            {
                if (!PoseSlotBuildValidator.Validate(target))
                    throw new InvalidOperationException("NDMFビルド後検証がFAILでした。アップロードしないでください。");
                lastResult = "PASS: 現在の導入内容は固定仕様v" +
                             PoseSlotFixedSpecification.ContractVersion + "に一致しています。";
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

        private static void DrawStatus(string label, bool ok, string failureLabel = "未検出")
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
