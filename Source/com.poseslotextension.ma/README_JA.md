# Pose Slot Extension - MA Stable

---

## ⚠ 非公式ADD-ON

**本ツールはBUDDYWORKSの非公式ADD-ONです。BUDDYWORKS様が開発・配布・サポートするものではありません。**

**本ツールに関する不具合報告・質問・要望は、すべて配布元（GitHub Issues）へお願いします。BUDDYWORKS様へ問い合わせないでください。**
BUDDYWORKS様のサポート窓口・Discord・購入ページ等へ本ツールの件で連絡することはお控えください。

サポート窓口: https://github.com/BBBB5551111/PoseSlotExtension/issues

---


BUDDYWORKS Poses Extensionの`[MA]`版だけを対象にした固定版です。VRCFury版との自動判定や切替UIはありません。

## 機能

- `Save 01-50`: 現在の`PE/Set`と`PE/Float`を指定スロットへ上書き保存
- `Load 01-50`: 指定スロットの2値をBUDDYWORKSへ復元
- Save/Loadそれぞれ7フォルダ、独自Nextなし、最後は43-50の8項目
- 各スロットの`Set`、`Pose`、`Valid`（合計150個）はSavedかつローカル専用
- 未保存スロットのLoadは何も変更しない
- Load後のポーズはBUDDYWORKS本来の同期経路を使うため他人にも表示
- ジャンプ、着地、移動、Emoteでは解除せず、BUDDYWORKSの明示的なResetで解除
- BUDDYWORKS最上段へ`Pose Slots`を配置し、元の`Dances`を`More`へ移動

## 必要なもの

- Unity 2022.3.22f1
- VRChat SDK - Avatars 3.10.4
- Modular Avatar 1.18.1
- NDMF 1.14.4
- BUDDYWORKS Poses Extension 7.2.1の`BUDDYWORKS Poses Extension [MA]`

VRCFuryはこの固定版の導入経路には使いません。別ギミック用のVRCFuryが同居していても、BUDDYWORKS `[VRCF]`として扱いません。

## 導入

1. Unityプロジェクトをバックアップします。
2. 対象アバターへBUDDYWORKS Poses Extensionの`[MA]`版を導入します。
3. **v1.x を導入済みの場合は、先に `Assets/PoseSlotExtensionMAStable` フォルダを削除します。** v2.0で導入先が `Packages/` へ変わったため、両方が残るとクラスが重複します。
4. 本UnityPackageをImportします。`Packages/com.poseslotextension.ma` へ展開され、Unityの Package Manager に `Pose Slot Extension (Modular Avatar)` として表示されます。`Assets/` には何も追加されません。
5. `Tools > Pose Slot Extension MA > Setup`を開きます。
6. 対象Avatar Descriptorを選び、`生成・導入・検証`を押します。
7. `PASS`を確認してからアップロードします。

`Generated`は利用環境で生成され、配布UnityPackageには含まれません。旧統合版のPoseSlotExtensionオブジェクトは、新しいMA版の導入が成功した後に置換されます。

アンインストールは `Packages/com.poseslotextension.ma` フォルダを削除するだけです。

## 非破壊方針

- BUDDYWORKS Package原本は編集しません。
- Action ControllerとExpression Menuを`Packages/com.poseslotextension.ma/Generated`へ複製して加工します。
- 元のExpression Parametersを直接変更せず、必要時はPrivateParametersへ複製して移行します。
- 対象アバターのMA Menu InstallerとMA Merge AnimatorにはPrefab Overrideを保存します。

同じアバターにMA固定版とVRCFury固定版を同時導入しないでください。

## 検証

実プロジェクトのBUDDYWORKS `[MA]`アバターへ生成・導入し、NDMF後の50 Save、50 Load、151個のPSEパラメータ、空スロット、上書き、高速連続Load、ポーズ保持、Reset、最上段配置、8項目上限を検証してPASSしています。VRChat実機でも50件保存・読込・上書きとジャンプ保持を確認済みです。特定のアバター名、階層名、メッシュまたは衣装には依存しません。

利用前に`TERMS_JA.md`と`THIRD_PARTY_NOTICES_JA.md`を確認してください。
