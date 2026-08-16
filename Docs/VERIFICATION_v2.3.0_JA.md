# Pose Slot Extension v2.3.0 検証記録

- 検証日: 2026-08-16
- Unity: 2022.3.22f1
- VRChat SDK - Avatars: 3.10.4
- Modular Avatar: 1.18.1
- NDMF: 1.14.4
- VRCFury: 1.1416.0 / 1.1417.0
- BUDDYWORKS Poses Extension: 7.2.1

## 静的パッケージ検査

日本語・英語のMA版／VRCFury版4パッケージで、原本とのバイト一致、欠落なし、別版混入なし、gzipヘッダ正常を確認しました。

| パッケージ | SHA-256 |
|---|---|
| `PoseSlotExtension_MA_v2.3.0.unitypackage` | `A8FFEE8DB36E576AFE8E647FBA73A53D02A752D99E2B5233DF9DF375FC358B31` |
| `PoseSlotExtension_VRCFury_v2.3.0.unitypackage` | `6D8C989483CCE11BE101524A772C13C7BEEEB6D261233A2500CBE6DBD7E671C8` |
| `PoseSlotExtension_MA_v2.3.0_EN.unitypackage` | `6DA7348825B8CB984C9177286942DBA7EBFABD91107E1C41ACA80F9EE43172D8` |
| `PoseSlotExtension_VRCFury_v2.3.0_EN.unitypackage` | `B00507D59CACEFFC7A1DE1338257CBF121BEB18FC7FA5588D6728B802BF58E7B` |

## 機能検証

- 50 Save／50 Load
- 全50スロットの上書き／再Load
- 未保存スロットLoadのno-op
- 151個のPSEパラメータ（Command 1、Set/Pose/Valid 各50）
- 高速連続Load
- ジャンプ、着地、移動後のポーズ保持
- BUDDYWORKS Resetによる解除
- `Pose Slots`最上段配置と`Dances`の`More`移動
- Expression Menuの各階層8項目以内
- MA版／VRCFury版の生成、導入、NDMF後検証

MA版とVRCFury版はVRChat実機でも動作確認済みです。検証は特定アバター固有の名前、階層、メッシュまたは衣装を前提としていません。
