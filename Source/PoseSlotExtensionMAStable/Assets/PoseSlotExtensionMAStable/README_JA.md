# Pose Slot Extension - MA Stable

BuddyWorks Poses Extensionの`[MA]`版だけを対象にした固定版です。VRCFury版との自動判定や切替UIはありません。

## 機能

- `Save 01-50`: 現在の`PE/Set`と`PE/Float`を指定スロットへ上書き保存
- `Load 01-50`: 指定スロットの2値をBuddyWorksへ復元
- Save/Loadそれぞれ7フォルダ、独自Nextなし、最後は43-50の8項目
- 保存値100個はSavedかつローカル専用
- Load後のポーズはBuddyWorks本来の同期経路を使うため他人にも表示
- ジャンプ、着地、移動、Emoteでは解除せず、BuddyWorksの明示的なResetで解除
- BuddyWorks最上段へ`Pose Slots`を配置し、元の`Dances`を`More`へ移動

## 必要なもの

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- Modular Avatar 1.18.1
- NDMF 1.14.4
- BuddyWorks Poses Extension 7.2.1の`BUDDYWORKS Poses Extension [MA]`

VRCFuryはこの固定版の導入経路には使いません。別ギミック用のVRCFuryが同居していても、BuddyWorks `[VRCF]`として扱いません。

## 導入

1. Unityプロジェクトをバックアップします。
2. 対象アバターへBuddyWorks Poses Extensionの`[MA]`版を導入します。
3. 本UnityPackageをImportします。
4. `Tools > Pose Slot Extension MA > Setup`を開きます。
5. 対象Avatar Descriptorを選び、`生成・導入・検証`を押します。
6. `PASS`を確認してからアップロードします。

`Generated`は利用環境で生成され、配布UnityPackageには含まれません。旧統合版のPoseSlotExtensionオブジェクトは、新しいMA版の導入が成功した後に置換されます。

## 非破壊方針

- BuddyWorks Package原本は編集しません。
- Action ControllerとExpression Menuを`Assets/PoseSlotExtensionMAStable/Generated`へ複製して加工します。
- 元のExpression Parametersを直接変更せず、必要時はPrivateParametersへ複製して移行します。
- 対象アバターのMA Menu InstallerとMA Merge AnimatorにはPrefab Overrideを保存します。

同じアバターにMA固定版とVRCFury固定版を同時導入しないでください。

## 検証

実プロジェクトのBuddyWorks `[MA]`アバターへ2回連続で生成・導入し、各回でNDMF後の50 Save、50 Load、101個のPSEパラメータ、上書き、ポーズ保持、Reset、最上段配置、8項目上限を検証してPASSしています。

利用前に`TERMS_JA.md`と`THIRD_PARTY_NOTICES_JA.md`を確認してください。
