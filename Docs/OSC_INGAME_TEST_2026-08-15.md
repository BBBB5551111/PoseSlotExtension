# VRChat実機OSCテスト結果（v1.0.4）

- 日時: 2026-08-15 13:5x JST / VRChat再起動後、SiuSiuUniversal装着状態
- 方法: OSC送信 127.0.0.1:9000 / 受信 127.0.0.1:9001（Pythonスクリプト、外部ライブラリなし）
- OSC設定JSON: 総パラメータ496、`PSE/Command`あり、PSEスロット100、`PE/Set`・`PE/Float`あり

## テスト1: Slot 01 基本Save/Load
1. PE/Set=7, PE/Float=0.25 を設定
2. PSE/Command=1（Save 01）→ 0
3. PE/Set=13, PE/Float=0.75 へ変更
4. PSE/Command=101（Load 01）→ 0
5. 受信: PE/Set=7, PE/Float=0.25 → **PASS**

## テスト2: Slot 50（端スロット）
1. PE/Set=21, PE/Float=0.9 → PSE/Command=50でSave
2. ポーズ変更後、PSE/Command=150でLoad
3. 受信: PE/Set=21, PE/Float=0.9 → **PASS**

## テスト3: Slot 01 上書き
1. PE/Set=2, PE/Float=0.1 → PSE/Command=1でSlot01を上書き
2. ポーズ変更後、PSE/Command=101でLoad
3. 受信: PE/Set=2, PE/Float=0.1（旧値7/0.25ではない）→ **PASS**

## 備考
- VRChat側のアバター更新反映には、VRChat再起動（またはアバター再取得）が必要だった。
- 同一アバターIDへの`/avatar/change`は無視される。別の所有アバターへ切替→戻すことで再装着は可能だが、APIキャッシュにより新バージョン取得は保証されない。確実なのはVRChat再起動。
- OSC設定JSON（`<VRChat OSC config folder>\<avatar>.json`）は存在すると再生成されない。削除→アバター再装着（または再起動）で再生成される。
