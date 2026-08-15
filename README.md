# Pose Slot Extension — Stable Split

BUDDYWORKS Poses Extension向けの50スロット ポーズ保存・復元ツール。

---

## ⛔ 現在、配布を停止しています / Distribution is currently paused

**配布物は取り下げました。現在インストールできる版はありません。**

BUDDYWORKS Poses Extension の作者より「このツール経由でBUDDYWORKSのアセットが持ち出せてしまわないようにしてほしい」との指摘を受け、調査したところ次の問題を実測で確認しました。

- 本ツールは BUDDYWORKS の Action Controller とメニューを `Assets/` 配下へ複製している
- その複製経由で、Unityの Export Package（依存関係を含む）により **BUDDYWORKSのアニメーション・BlendTree・アイコン計167アセット（実測3.7MB）を持ち出せてしまう**

`Packages/` 配下はVPM経由でしか入手できず、それが作者にとっての保護になっています。本ツールはその保護を迂回する経路を作ってしまっていました。

現在、**BUDDYWORKS由来のアセットをユーザーのプロジェクトへ一切複製しない設計**へ改修中です。改修完了と作者の確認をもって配布を再開します。

ソースは検証のため公開したままにしていますが、**現行のソースをそのままビルドして使用しないでください**（上記の問題を含みます）。

> **Distribution is paused. There is currently no installable build.**
>
> The author of BUDDYWORKS Poses Extension asked that this tool must not let people export their assets out of a project. We measured the current design and confirmed the problem: this tool copies the BUDDYWORKS Action Controller and menus into `Assets/`, and through those copies Unity's "Export Package (include dependencies)" pulls **167 BUDDYWORKS assets** — animations, blend trees and icons, 3.7 MB measured — into a redistributable file. Content under `Packages/` can only be obtained through VPM, and that is the author's protection; this tool was creating a way around it.
>
> We are reworking the tool so that nothing derived from BUDDYWORKS is ever written into a user's project. Distribution will resume once that is done and the author is satisfied with it. The source stays public for review, but please do **not** build and use it as-is.

---

## ⚠ 非公式ADD-ON / Unofficial add-on

**本ツールはBUDDYWORKSの非公式ADD-ONです。BUDDYWORKS様が開発・配布・サポートするものではありません。**

**本ツールに関する不具合報告・質問・要望は、すべて本リポジトリのIssuesへお願いします。BUDDYWORKS様へ問い合わせないでください。**
BUDDYWORKS様のサポート窓口・Discord・購入ページ等へ本ツールの件で連絡することはお控えください。BUDDYWORKS Poses Extension本体の問題と切り分けられない場合も、まず本リポジトリへご連絡ください。

> **This is an UNOFFICIAL add-on for BUDDYWORKS Poses Extension.**
> It is not developed, distributed or supported by BUDDYWORKS.
>
> **All bug reports, questions and feature requests about this tool must go to this repository's Issues — please do not contact BUDDYWORKS about it.**
> Do not raise issues with this tool on BUDDYWORKS' support channels, Discord or store pages. Even if you are unsure whether the problem comes from this tool or from BUDDYWORKS Poses Extension itself, please report it here first.

サポート窓口 / Support: https://github.com/BBBB5551111/PoseSlotExtension/issues

---

**VRCFury版とMA版は別製品です。どちらか一方だけを使い、同じアバターへ同時導入しないでください。**

| 版 | バージョン | 対象 | ソース |
|---|---|---|---|
| VRCFury Stable | `1.0.4-vrcfury-stable` | BUDDYWORKS `[VRCF]` | `Source/PoseSlotExtensionVRCFuryStable` |
| MA Stable | `1.0.0-ma-stable` | BUDDYWORKS `[MA]` | `Source/PoseSlotExtensionMAStable` |

英語版（English edition）は `Source_EN/` にあります。名前空間・アセットパス・メニュー項目・パラメータ契約は日本語版と同一で、UI文言と同梱ドキュメントだけが英語です。日英で導入済み設定を壊さずに入れ替えられますが、**同一プロジェクトへ日英を同時にImportしないでください**（同じパスを使うため）。

## 機能（固定仕様）

- `Save 01–50` / `Load 01–50`（上書き方式、7フォルダ構成、独自Nextなし）
- 保存対象はBUDDYWORKSの `PE/Set`(Int) と `PE/Float`(Float) の2値のみ
- 保存バンクはローカル専用（50スロット分を他者へ同期しない）。Load後のポーズはBUDDYWORKS本来の同期経路で他者にも見える
- ジャンプ・移動・Emoteでポーズ解除されない。解除はBUDDYWORKSの明示Resetのみ
- BUDDYWORKS Package原本は編集しない（必要アセットは`Generated`へ複製して加工）

## 検証状態（2026-08-15）

両版とも VRChat実機で50スロット全数（保存→読込→全上書き→再読込）とジャンプ保持をOSCで検証済み。詳細は `Docs/` の検証レポートを参照。

- 配布は停止中です。Releasesのアセットは取り下げてあります
- `Tools/pse_osc_full50.py` は実機全数検証スクリプト（OSC 9000/9001、Python3・依存なし）

## 導入（共通の流れ）

> **現在は配布を停止しているため、この手順は実行できません。** 配布再開後の参考として残しています。

1. VCCで VRChat SDK / Modular Avatar / （VRCFury版のみ）VRCFury / BUDDYWORKS Poses Extension を導入
2. Releasesから対象版の `.unitypackage` をImport
3. `Tools > Pose Slot Extension VRCFury > Setup`（または `... MA > Setup`）で対象アバターを選び「生成・導入・検証」
4. PASSを確認してからVRChat SDKでBuild & Upload

## 禁止事項

- MA/VRCFuryの1ツールへの再統合、方式自動判定の実装
- 同一アバターへの両版同時導入
- 旧版（VRCFury v1.0.0〜v1.0.3、統合版v1.1.5系）の配布・使用
