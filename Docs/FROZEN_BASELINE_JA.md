# Pose Slot Extension 凍結基準点（ハードコーディング記録）

- 凍結日: 2026-08-16（Asia/Tokyo）
- 凍結対象: VRCFury版 `1.0.4-vrcfury-stable` / MA版 `1.0.0-ma-stable`（日本語版・英語版）
- 凍結の意味: **このスナップショットは以後変更しない。** 実装を変える場合は必ず新バージョンを切り、ここへは戻して書かない
- 解除条件: **利用者からの明示的な指示があるまで、このフォルダ配下を変更しない**

---

## 1. 凍結した固定契約（`PoseSlotFixedSpecification`）

以下は日英・両版で完全一致していることを確認済み。**この契約は変更しない。**

| 項目 | 値 |
|---|---|
| 契約バージョン | `ContractVersion = 1` |
| スロット数 | `SlotCount = 50` |
| Saveコマンド | `1`〜`50`（スロット番号と同値） |
| Loadコマンド | `101`〜`150`（`LoadCommandOffset = 100`） |
| 制御パラメータ | `PSE/Command`（Int、ローカル専用、非保存） |
| 保存対象 | `PE/Set`（Int）と `PE/Float`（Float）の2値のみ |
| 保存先 | `PSE/01/Set`〜`PSE/50/Set`、`PSE/01/Pose`〜`PSE/50/Pose`（計100個、Saved かつ ローカル専用） |
| メニュー範囲 | `01–07` / `08–14` / `15–21` / `22–28` / `29–35` / `36–42` / `43–50`（Save・Load各7フォルダ、独自Nextなし） |
| メニュー配置 | `Pose Slots` をBUDDYWORKS最上段へ、元の `Dances` を `More` へ移動 |
| ポーズ保持 | ジャンプ・着地・移動・Emoteで解除しない。BUDDYWORKSの明示Resetのみで解除 |

## 2. 凍結した製品分割方針

- **VRCFury版とMA版は別製品。** 1ツールへの再統合、方式の自動判定は実装しない
- 同一アバターへの両版同時導入は不可
- BUDDYWORKS Package原本は編集しない
- 本ツールはBUDDYWORKSの**非公式ADD-ON**。サポートは配布元が受ける

## 3. 凍結物のハッシュ

### 配布パッケージ（`Packages/`）

| ファイル | SHA-256 |
|---|---|
| `PoseSlotExtension_VRCFury_Stable_v1.0.4.unitypackage` | `E2BEC7BCB235B8082316922788F6CBD8ED93CDC83A2E4D15F4D5270BFBAE51B0` |
| `PoseSlotExtension_MA_Stable_v1.0.0.unitypackage` | `1760BDC288F4D71A10C32DC4FBA24A4C6D36C2FD23C140152071A3957C61B23F` |
| `PoseSlotExtension_VRCFury_Stable_v1.0.4_EN.unitypackage` | `9DFB2F1AAB9E8CD3C1CFC305309C231CB5D3A37038CF1BBFF7A66C6A5802032A` |
| `PoseSlotExtension_MA_Stable_v1.0.0_EN.unitypackage` | `2354F860428243A7478DD48106300118B2AB0B5E02B0783AC7AE8E893E544C9C` |

### ソース

全50ファイルのSHA-256を `SOURCE_SHA256.txt` に記録。照合コマンド:

```bash
cd Frozen_v1.0.4 && sha256sum -c SOURCE_SHA256.txt
```

### 整合性

凍結時点で、**配布パッケージの中身とソースが全ファイルバイト一致**することを確認済み（VRCFury版14ファイル、不一致0）。日本語版はBUDDYWORKS表記修正を反映して再ビルドしてある。

## 4. 検証済み事項（この凍結版で確認したこと）

両版とも以下をPASS。

- クリーンプロジェクトへのUnityPackage導入、C#コンパイルエラー0
- NDMFビルド後検証PASS（シーン保存→再読込後の再検証を含む）
- VRChat公式SDK BuildがPASS、シェーダーエラー0
- 実Blueprintへの実アップロード成功
- **VRChat実機OSC全数テスト**: 50保存 → 撹乱値を挟んだ50読込 → 別パターンで50上書き → 再読込、すべて50/50 PASS、`/input/Jump` 後のポーズ保持PASS
- 別環境（`VRChat_Avators` / `Raelynn Rainbow`）での手動導入もPASS（自動解除トランジション残0、明示Reset保持1）

## 5. 【重要】未解決のまま凍結した事項

**この凍結版には未解決の問題がある。配布してはいけない。**

BUDDYWORKS Poses Extension の作者より「このツール経由でBUDDYWORKSのアセットが持ち出せてしまわないようにしてほしい」という指摘を受け、実測により次を確認した。

- 本ツールは BUDDYWORKS の Action Controller（481,045バイト）とメニュー3種を `Assets/.../Generated/` へ複製する
- その複製が BUDDYWORKS Package 内の37アセットを参照しており、Unity の Export Package（依存関係を含む）で **BUDDYWORKS由来167アセット・実測3,738,599バイト**を持ち出せてしまう

このため **2026-08-16時点で配布を停止中**（GitHubリリースのアセットは取り下げ済み、README/リリースノートに告知済み）。

### 対応方針（v2.0として別途実施）

BUDDYWORKS由来のアセットをユーザープロジェクトへ一切複製しない設計へ改修する。詳細と第三者レビュー結果は次を参照。

- `../Handoff_v2.0_Design/CROSSCHECK_REQUEST_JA.md`（設計案・実測データ）
- `../Handoff_v2.0_Design/CROSSCHECK_RESPONSE_CODEX_JA.md`（Codexによるクロスチェック回答）

**重要な既知の誤り**: 設計案§3に記載した「VRCFuryが先に走るのでNDMFパスからマージ後が見える」という前提は**誤り**。実際の順序は NDMF Preprocess `-11000` → VRCFury `VrcPreuploadHook` `-10000` → NDMF Optimize `-1025` であり、NDMF TransformingはVRCFuryのマージより前に完了する。VRCFury版の改修方針は再設計が必要（ソースで確認済み）。

## 6. このフォルダの構成

```
Frozen_v1.0.4/
├ FROZEN_BASELINE_JA.md   ← 本書
├ SOURCE_SHA256.txt        ← ソース全50ファイルのハッシュ
├ Source/                  ← 日本語版 正本（VRCFury / MA）
├ Source_EN/               ← 英語版 正本（VRCFury / MA）
└ Packages/                ← 配布UnityPackage 4点 + SHA256.txt
```

## 7. 次のAI・開発者への指示

1. **このフォルダ配下を変更しない。** 参照と照合のみ
2. 実装を変更する場合は、このスナップショットからコピーして新バージョンとして作業する
3. 配布再開の判断は利用者が行う。§5の問題が未解決のうちは配布しない
4. 「動作確認できた」と報告する際は、コンパイル / NDMF / SDK Build / 実アップロード / 実機OSC のどの段階かを必ず区別する
