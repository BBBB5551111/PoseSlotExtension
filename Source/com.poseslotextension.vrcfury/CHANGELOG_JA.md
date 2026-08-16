# 変更履歴

## 2.3.0-vrcfury-stable - 2026-08-16

- 空スロットを `Valid=false` で明示し、未保存LoadがStandingを呼ぶ問題を修正
- Save時に `PE/Set` と `PE/Float` に加えて `Valid=true` を保存
- Loadは `Valid=true` のスロットだけ実行
- 0.35秒の待機を廃止し、Any Stateから各Save/Loadへ直接遷移して連続入力を取りこぼさない構造へ変更
- 明示的なAvatar Reset後は、VRChatの仕様どおり保存スロットを空として扱う

## 2.2.0-vrcfury-stable - 2026-08-16

- **Load後の最短滞在時間（0.35秒）を追加。** 短時間に何度もLoadすると `PE/Set` が連続で書き換わり、BUDDYWORKS のポーズ遷移が中断されて明示Resetでしか復帰しなくなる問題への対処
  - 実装: Load状態からIdleへ戻る遷移に Exit Time を設定。滞在中は次のLoadを受け付けない
  - **押下は捨てられない。** メニューはボタンを押している間コマンド値を保持するため、滞在時間の経過後に適用される。滞在時間より短い一瞬の通過のみ取りこぼす
  - Save側は `PE/Set` を書かないので変更なし
- 滞在時間の実測値はゲーム内で校正すること（`Evidence/pse_osc_settle.py`）

## 2.1.1-vrcfury-stable - 2026-08-16

- **v2.1.0 の修正漏れ。** 復帰トランジションを `NotEqual` へ変えた一方で、`PoseSlotBuildValidator` が「復帰条件は `Command == 0`」を要求したままだった
  - 影響: v2.1.0 で Setup を実行すると `Save/Load return transitions: False` となり、**NDMF検証が FAIL する**
  - 修正: 検査条件を「自分のコマンド以外（NotEqual）」へ更新。これで旧構造を検出する回帰ガードになる

## 2.1.0-vrcfury-stable - 2026-08-16

- **【重大修正】`PSE/Command` が `0` を挟まずに連続すると、2つ目のコマンドが無言で捨てられる問題を修正**
  - 原因: Save/Load 状態から Idle へ戻る条件が `Command == 0` のみだったため、次のコマンドが `0` より先に届くと前の状態から抜けられなかった
  - 症状: VRChatのメニューはカーソルが隣のボタンを通過するとその値を送るため、隣を掠めると「押しても反応しない」か「隣のスロットへ保存される」状態になっていた
  - 修正: 復帰条件を `Command == 0` から「自分のコマンド以外」（NotEqual）へ変更。次のコマンドが来た時点で即座にIdleへ戻る
- この不具合は **v1.0.2 から存在していた**。`Packages/` 化とは無関係
- 固定契約（`PoseSlotFixedSpecification`）に変更なし

## 2.0.0-vrcfury-stable - 2026-08-16

- **導入先を `Assets/` から `Packages/` へ変更。** BUDDYWORKS作者からの「`/Packages` 配下に置いてほしい」という要請に対応
- Editor専用の Assembly Definition を追加（`Packages/` 配下はasmdefが無いとコンパイルされないため）
- 生成物の出力先を `Packages/com.poseslotextension.vrcfury/Generated` へ変更
- 固定契約（`PoseSlotFixedSpecification`）は一切変更なし。v1.0.4と完全に同一
- 【注意】Unityの Export Package は依存関係を辿って `Packages/` 配下も実データごと書き出します。`Packages/` 化はBUDDYWORKS資産の持ち出しを防ぐものではありません

## 1.0.4-vrcfury-stable - 2026-08-15

- 【重大修正】v1.0.3で導入したPrefabオーバーライド方式では、VRCFuryコンポーネント（SerializeReferenceシリアライズ）への変更がシーン保存時に無言で消失し、アップロード後のアバターに`Pose Slots`メニューと`PSE/Command`が含まれない問題を修正
- BUDDYWORKS `[VRCF]` Prefabインスタンスに限定した安全なUnpackを復活（外側PrefabルートがBUDDYWORKS自身の場合のみ。アバターPrefab内にネストされている場合は無言の失敗ではなく明確なエラーで停止）
- ONE CLICK実行時、シーン保存→再読込→NDMF再検証を行い、メモリ上にしか存在しない導入状態を検出してFAILさせるゲートを追加
- MAコンポーネント側（通常シリアライズ）へのオーバーライド方式・その他v1.0.3の改善は維持

## 1.0.3-vrcfury-stable - 2026-08-15

- MA安定版で検証済みの改善をVRCFury版へ移植
- 導入時にBUDDYWORKS Prefabインスタンスを完全Unpackする破壊的動作を廃止し、Prefabを保持したままプロパティオーバーライドで参照を書き替えるよう変更（ネストPrefab構成のアバターでの失敗・平坦化を防止）
- Prefabインスタンス上のVRCFury参照変更を`RecordPrefabInstancePropertyModifications`で確実に保存するよう修正
- 生成時の依存チェックを`[MA]` Prefabではなく`[VRCF]` Prefabの存在確認へ修正
- 生成のたびに`README_JA.md`を自動生成文で上書きする動作を廃止
- NDMF後検証のメニュー8項目制限チェックを、統合メニュー全階層の再帰走査へ強化
- Save/Load本体仕様（50スロット、`PE/Set`・`PE/Float`のみ保存、ポーズ保持）に変更なし

## 1.0.2-vrcfury-stable - 2026-08-15

- 対象アバターにBUDDYWORKS Poses Extension `[VRCF]` が無い場合、Package同梱Prefabを自動導入するよう修正
- `[MA]` 版しか入っていないアバターでも、VRCFury固定版の生成・導入・検証を開始できるよう修正
- 存在しない旧Parameters／メニュー参照を除去し、現行Stable参照へ自己修復
- `SiuSiuUniversal`で生成・導入、NDMF検証、VRChat公式SDK Build、既存Blueprintへの実アップロードを順に実行してPASS

## 1.0.0-vrcfury-stable - 2026-08-15

- MA対応前のVRCFury専用安定版v1.0.2を基準に独立パッケージ化
- MA検出、MAメニュー置換、方式選択UIへの依存を持たないVRCFury固定経路
- BUDDYWORKS原本を編集せず、複製メニュー内だけで`Pose Slots`を最上段へ移動し、`Dances`を`More`へ移動
- 旧統合版の生成メニュー参照と導入オブジェクトを、導入成功後に安全に移行
- 独立アセットルート、独立名前空間、独立NDMFプラグインIDを採用
- 同一アバターへ2回連続で生成・導入・NDMF後検証を行い、両方PASS

## 1.0.2 - 2026-08-14

- 対象アバターにはBUDDYWORKSの `[VRCF]` 版が必要であることをSetup画面とREADMEへ明記
- `[MA]` 版だけが対象に入っている場合、Setup画面へ「MA版のみ」と具体的に表示
- 導入失敗時の共通メッセージを廃止し、生成Prefab、Animator、Modular Avatar、VRCFuryメニュー参照のどこで失敗したか表示
- Unity 2022.3.22f1上で新規 `[VRCF]` Prefabから生成・導入する回帰テストを実施
- Save 01～50、Load 01～50、ポーズ保持の本体仕様に変更なし

## 1.0.1 - 2026-08-14

- Lyuma Av3 Emulator未導入環境で`PoseSlotRuntimeTest.cs`がCS0246になる問題を修正
- Lyumaを使用する内部Runtime Testを`PSE_LYUMA_RUNTIME_TEST`定義時だけコンパイルするよう変更
- Save 01～50、Load 01～50、ポーズ保持の本体仕様に変更なし

## 1.0.0 - 2026-08-13

- 50個の上書きSave/Loadスロットを実装
- `PE/Set` と `PE/Float` のみを保存する固定仕様v1を採用
- Save/Load各7フォルダ、独自Nextなしのメニュー構成を実装
- 保存値をローカル専用、Load先をBUDDYWORKS同期パラメータに設定
- ジャンプ、着地、移動、Emoteによる自動解除を抑止
- BUDDYWORKSの明示Resetによる解除を維持
- 対象アバター選択式のSetup画面を追加
- BUDDYWORKS Package原本を変更しない非破壊生成方式を採用
- VRCFury Base Locomotion重複の事前検出を追加
- NDMF/VRCFuryビルド後検証を追加
- 公開済みVRChatアバターで50枠＋上書き50枠、計100サイクルの実機検証を実施
