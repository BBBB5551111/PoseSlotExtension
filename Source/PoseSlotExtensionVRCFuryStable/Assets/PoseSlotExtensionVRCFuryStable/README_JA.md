# Pose Slot Extension - VRCFury Stable

BuddyWorks Poses Extensionの`[VRCF]`版だけを対象にした固定版です。MA対応前のv1.0.2を基準にし、MA検出・MAメニュー置換・方式選択UIには依存しません。

## 機能

- `Save 01-50`: 現在の`PE/Set`と`PE/Float`を指定スロットへ上書き保存
- `Load 01-50`: 指定スロットの2値をBuddyWorksへ復元
- Save/Loadそれぞれ7フォルダ、独自Nextなし、最後は43-50の8項目
- 保存値100個はSavedかつローカル専用
- Load後のポーズはBuddyWorks本来の同期経路を使うため他人にも表示
- ジャンプ、着地、移動、Emoteでは解除せず、BuddyWorksの明示的なResetで解除
- BuddyWorks最上段へ`Pose Slots`を配置し、元の`Dances`を`More`へ移動

VRCFury専用安定版へ追加したBuddyWorksメニュー変更は、複製メニュー内の`Dances`と`Pose Slots`の位置交換だけです。BuddyWorks Package原本は変更しません。

## 必要なもの

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- Modular Avatar 1.18.1
- VRCFury 1.1417.0
- BuddyWorks Poses Extension 7.2.1（`[VRCF]` Prefabはセットアップ時に自動導入されます）

## 導入

1. Unityプロジェクトをバックアップします。
2. BuddyWorks Poses Extension 7.2.1がVCCへ導入済みであることを確認します。対象アバターへの`[VRCF]`版配置はツールが自動実行します。
3. 本UnityPackageをImportします。
4. `Tools > Pose Slot Extension VRCFury > Setup`を開きます。
5. 対象Avatar Descriptorを選び、`生成・導入・検証`を押します。
6. `PASS`を確認してからアップロードします。

`Generated`は利用環境で生成され、配布UnityPackageには含まれません。旧統合版のPoseSlotExtensionオブジェクトと旧生成メニュー参照は、新しいVRCFury版の導入が成功した後に移行されます。

## 非破壊方針

- BuddyWorks Package原本は編集しません。
- Action ControllerとExpression Menuを`Assets/PoseSlotExtensionVRCFuryStable/Generated`へ複製して加工します。
- 元のExpression Parametersを直接変更せず、必要時はPrivateParametersへ複製して移行します。
- 対象アバターのVRCFury Full Controllerには必要な参照・パラメータ設定だけを保存します。
- Locomotion（Base）が複数ある場合は停止し、既存Locomotionを自動削除しません。

同じアバターにMA固定版とVRCFury固定版を同時導入しないでください。

## 検証

実プロジェクトの`SiuSiuUniversal`へ生成・導入し、VRCFury＋NDMF後の50 Save、50 Load、101個のPSEパラメータ、上書き、ポーズ保持、Reset、最上段配置、8項目上限を検証しています。さらにVRChat公式SDKのBuildと既存Blueprintへの実アップロードまでPASSしています。

利用前に`TERMS_JA.md`と`THIRD_PARTY_NOTICES_JA.md`を確認してください。
