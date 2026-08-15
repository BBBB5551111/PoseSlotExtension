# Pose Slot Extension — Stable Split

BuddyWorks Poses Extension向けの50スロット ポーズ保存・復元ツール。

**VRCFury版とMA版は別製品です。どちらか一方だけを使い、同じアバターへ同時導入しないでください。**

| 版 | バージョン | 対象 | ソース |
|---|---|---|---|
| VRCFury Stable | `1.0.4-vrcfury-stable` | BuddyWorks `[VRCF]` | `Source/PoseSlotExtensionVRCFuryStable` |
| MA Stable | `1.0.0-ma-stable` | BuddyWorks `[MA]` | `Source/PoseSlotExtensionMAStable` |

英語版（English edition）は `Source_EN/` にあります。名前空間・アセットパス・メニュー項目・パラメータ契約は日本語版と同一で、UI文言と同梱ドキュメントだけが英語です。日英で導入済み設定を壊さずに入れ替えられますが、**同一プロジェクトへ日英を同時にImportしないでください**（同じパスを使うため）。

## 機能（固定仕様）

- `Save 01–50` / `Load 01–50`（上書き方式、7フォルダ構成、独自Nextなし）
- 保存対象はBuddyWorksの `PE/Set`(Int) と `PE/Float`(Float) の2値のみ
- 保存バンクはローカル専用（50スロット分を他者へ同期しない）。Load後のポーズはBuddyWorks本来の同期経路で他者にも見える
- ジャンプ・移動・Emoteでポーズ解除されない。解除はBuddyWorksの明示Resetのみ
- BuddyWorks Package原本は編集しない（必要アセットは`Generated`へ複製して加工）

## 検証状態（2026-08-15）

両版とも VRChat実機で50スロット全数（保存→読込→全上書き→再読込）とジャンプ保持をOSCで検証済み。詳細は `Docs/` の検証レポートと `Docs/AI_HANDOFF_SPEC_JA.md`（開発引継ぎ仕様書）を参照。

- 配布物はReleasesの `stable-split-v1.0.4` を使用してください（SHA-256はリリースノートに記載）
- `Tools/pse_osc_full50.py` は実機全数検証スクリプト（OSC 9000/9001、Python3・依存なし）

## 導入（共通の流れ）

1. VCCで VRChat SDK / Modular Avatar / （VRCFury版のみ）VRCFury / BuddyWorks Poses Extension を導入
2. Releasesから対象版の `.unitypackage` をImport
3. `Tools > Pose Slot Extension VRCFury > Setup`（または `... MA > Setup`）で対象アバターを選び「生成・導入・検証」
4. PASSを確認してからVRChat SDKでBuild & Upload

## 禁止事項

- MA/VRCFuryの1ツールへの再統合、方式自動判定の実装
- 同一アバターへの両版同時導入
- 旧版（VRCFury v1.0.0〜v1.0.3、統合版v1.1.5系）の配布・使用
