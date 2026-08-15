# VRChat実機 50スロット全数OSCテスト結果（MA版 v1.0.0）

- 日時: 2026-08-15 15:0x JST / VRChat実機
- 対象: SiuSiuUniversal（avtr_********）にMA版 `1.0.0-ma-stable` を導入・再アップロードした状態（FURY版からの置き換え。同時導入ではない）
- OSC設定: 総パラメータ487、PSE/Commandあり、PSEスロット100、VRCFury由来パラメータは140→93へ減少（PSE統合がVRCFury経由からMA経由へ置換された構成の傍証）
- 方法: `pse_osc_full50.py`（FURY版検証と同一スクリプト・同一手順）
- 所要: 284秒

## 結果 — ALL_PASS（全フェーズ50/50、観測漏れなし）

| フェーズ | 内容 | 結果 |
|---|---|---|
| A | パターンA（PE/Set=1..50, PE/Float=0.01..0.50）を50スロットへ保存 | 50/50 PASS |
| B | 撹乱値（255/0.999）を挟み全50スロットLoad→パターンA復元 | 50/50 PASS |
| C | パターンB（PE/Set=101..150, PE/Float逆順）で全50スロット上書き | 50/50 PASS |
| D | 全50スロット再Load→パターンB復元（旧値消滅=上書き動作） | 50/50 PASS |
| E | 実ポーズLoad→`/input/Jump`→着地後PE/Set=7・PE/Float=0.25保持 | POSE_SURVIVED_JUMP |

判定: MA版もFURY版と同一契約で全数動作。両版の実機動作が同一手順で確認された。
