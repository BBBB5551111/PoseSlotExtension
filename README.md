# Pose Slot Extension v2.3.0

BUDDYWORKS Poses Extension向けの、非公式50スロット・ポーズ保存／復元ADD-ONです。

> [!IMPORTANT]
> 本ツールはBUDDYWORKSの非公式ADD-ONです。BUDDYWORKSが開発・配布・サポートするものではありません。本ツールについてBUDDYWORKSへ問い合わせず、[GitHub Issues](https://github.com/BBBB5551111/PoseSlotExtension/issues)へご連絡ください。

## ダウンロード

[Releasesのv2.3.0](https://github.com/BBBB5551111/PoseSlotExtension/releases/tag/v2.3.0)から、対象方式と言語に合うファイルを1つだけダウンロードしてください。

| 対象 | 日本語 | English |
|---|---|---|
| Modular Avatar | `PoseSlotExtension_MA_v2.3.0.unitypackage` | `PoseSlotExtension_MA_v2.3.0_EN.unitypackage` |
| VRCFury | `PoseSlotExtension_VRCFury_v2.3.0.unitypackage` | `PoseSlotExtension_VRCFury_v2.3.0_EN.unitypackage` |

**同じアバターへMA版とVRCFury版を同時導入しないでください。** 日本語版と英語版も同じプロジェクトへ同時Importしないでください。

## 主な機能

- `Save 01–50`で現在の`PE/Set`と`PE/Float`を上書き保存
- `Load 01–50`で保存ポーズを復元
- 未保存スロットのLoadは何も変更しない
- 50スロットの保存値はローカル専用
- Load後のポーズはBUDDYWORKS本来の同期経路を使うため他ユーザーにも表示
- ジャンプ、着地、移動、Emoteで解除されず、BUDDYWORKSの明示Resetで解除
- BUDDYWORKS最上段へ`Pose Slots`を配置し、元の`Dances`を`More`へ移動
- 連続Save／Loadを取りこぼさない直接遷移

## 必須ツール・検証済み環境

| 共通 | 検証済みバージョン |
|---|---|
| Unity | `2022.3.22f1` |
| VRChat SDK - Avatars | `3.10.4` |
| Modular Avatar | `1.18.1` |
| NDMF | `1.14.4` |
| BUDDYWORKS Poses Extension | `7.2.1` |

- MA版：対象アバターに`BUDDYWORKS Poses Extension [MA]`が必要です。PSEの導入経路自体にはVRCFuryを使用しません。
- VRCFury版：VRCFury `1.1416.0`または`1.1417.0`と、BUDDYWORKSの`[VRCF]`版が必要です。`[VRCF]` Prefabはセットアップ時に自動導入されます。

上記以外のバージョンで動作する場合もありますが、検証済み環境ではなく、将来の互換性は保証しません。本ツールは特定のアバター名、階層名、メッシュまたは衣装には依存しません。

## 導入

1. Unityプロジェクトをバックアップします。
2. VCCで上記の必須ツールとBUDDYWORKS Poses Extensionを正規に導入します。
3. v1.xを使用していた場合は、旧`Assets/PoseSlotExtensionMAStable`または`Assets/PoseSlotExtensionVRCFuryStable`を削除します。
4. 対象の`.unitypackage`をImportします。ツール本体は`Packages/com.poseslotextension.ma`または`Packages/com.poseslotextension.vrcfury`へ入ります。
5. MA版は`Tools > Pose Slot Extension MA > Setup`、VRCFury版は`Tools > Pose Slot Extension VRCFury > Setup`を開きます。
6. Avatar Descriptorを選択し、`生成・導入・検証`を押します。
7. `PASS`を確認してからVRChat SDKでBuild & Uploadします。

## v2.3.0について

v2.3.0では各スロットへ`Valid`フラグを追加し、空スロットのLoadでStandingが呼ばれる問題を修正しました。また、待機時間を使わずAny StateからSave／Loadへ直接遷移するため、短時間の連続入力も処理します。明示的なAvatar Reset後はVRChatの仕様どおり保存スロットを空として扱います。

静的パッケージ検査、MA／VRCFuryの生成・NDMF後検証、50件の保存・読込・全上書き・再読込、高速連続Load、ジャンプ保持、Resetを確認しています。詳細は[検証記録](Docs/VERIFICATION_v2.3.0_JA.md)を参照してください。

## 第三者アセットとサポート

- 配布物にBUDDYWORKS、VRChat SDK、Modular Avatar、NDMF、VRCFuryのファイルは含みません。
- BUDDYWORKS Package原本は編集しません。
- 利用者環境で生成されるファイルは、正規導入済みのBUDDYWORKS資産を参照または複製して加工する場合があります。生成物を第三者へ再配布しないでください。
- 不具合報告は[GitHub Issues](https://github.com/BBBB5551111/PoseSlotExtension/issues)へお願いします。

## 規約

- [MA版 利用規約（日本語）](TERMS_MA_JA.md)
- [VRCFury版 利用規約（日本語）](TERMS_VRCFURY_JA.md)
- [MA edition Terms (English)](TERMS_MA.md)
- [VRCFury edition Terms (English)](TERMS_VRCFURY.md)
- [第三者表記](THIRD_PARTY_NOTICES_JA.md)

## English summary

Pose Slot Extension is an **unofficial** add-on for BUDDYWORKS Poses Extension. It provides 50 overwrite Save/Load slots, keeps loaded poses active through jumps and movement, and supports separate fixed editions for Modular Avatar and VRCFury. Download exactly one edition/language package from the [v2.3.0 release](https://github.com/BBBB5551111/PoseSlotExtension/releases/tag/v2.3.0). Do not contact BUDDYWORKS for support; use this repository's Issues.
