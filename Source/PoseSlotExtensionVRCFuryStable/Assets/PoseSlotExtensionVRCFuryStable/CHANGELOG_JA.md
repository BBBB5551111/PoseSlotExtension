# 変更履歴

## 1.0.4-vrcfury-stable - 2026-08-15

- 【重大修正】v1.0.3で導入したPrefabオーバーライド方式では、VRCFuryコンポーネント（SerializeReferenceシリアライズ）への変更がシーン保存時に無言で消失し、アップロード後のアバターに`Pose Slots`メニューと`PSE/Command`が含まれない問題を修正
- BuddyWorks `[VRCF]` Prefabインスタンスに限定した安全なUnpackを復活（外側PrefabルートがBuddyWorks自身の場合のみ。アバターPrefab内にネストされている場合は無言の失敗ではなく明確なエラーで停止）
- ONE CLICK実行時、シーン保存→再読込→NDMF再検証を行い、メモリ上にしか存在しない導入状態を検出してFAILさせるゲートを追加
- MAコンポーネント側（通常シリアライズ）へのオーバーライド方式・その他v1.0.3の改善は維持

## 1.0.3-vrcfury-stable - 2026-08-15

- MA安定版で検証済みの改善をVRCFury版へ移植
- 導入時にBuddyWorks Prefabインスタンスを完全Unpackする破壊的動作を廃止し、Prefabを保持したままプロパティオーバーライドで参照を書き替えるよう変更（ネストPrefab構成のアバターでの失敗・平坦化を防止）
- Prefabインスタンス上のVRCFury参照変更を`RecordPrefabInstancePropertyModifications`で確実に保存するよう修正
- 生成時の依存チェックを`[MA]` Prefabではなく`[VRCF]` Prefabの存在確認へ修正
- 生成のたびに`README_JA.md`を自動生成文で上書きする動作を廃止
- NDMF後検証のメニュー8項目制限チェックを、統合メニュー全階層の再帰走査へ強化
- Save/Load本体仕様（50スロット、`PE/Set`・`PE/Float`のみ保存、ポーズ保持）に変更なし

## 1.0.2-vrcfury-stable - 2026-08-15

- 対象アバターにBuddyWorks Poses Extension `[VRCF]` が無い場合、Package同梱Prefabを自動導入するよう修正
- `[MA]` 版しか入っていないアバターでも、VRCFury固定版の生成・導入・検証を開始できるよう修正
- 存在しない旧Parameters／メニュー参照を除去し、現行Stable参照へ自己修復
- `SiuSiuUniversal`で生成・導入、NDMF検証、VRChat公式SDK Build、既存Blueprintへの実アップロードを順に実行してPASS

## 1.0.0-vrcfury-stable - 2026-08-15

- MA対応前のVRCFury専用安定版v1.0.2を基準に独立パッケージ化
- MA検出、MAメニュー置換、方式選択UIへの依存を持たないVRCFury固定経路
- BuddyWorks原本を編集せず、複製メニュー内だけで`Pose Slots`を最上段へ移動し、`Dances`を`More`へ移動
- 旧統合版の生成メニュー参照と導入オブジェクトを、導入成功後に安全に移行
- 独立アセットルート、独立名前空間、独立NDMFプラグインIDを採用
- 同一アバターへ2回連続で生成・導入・NDMF後検証を行い、両方PASS

## 1.0.2 - 2026-08-14

- 対象アバターにはBuddyWorksの `[VRCF]` 版が必要であることをSetup画面とREADMEへ明記
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
- 保存値をローカル専用、Load先をBuddyWorks同期パラメータに設定
- ジャンプ、着地、移動、Emoteによる自動解除を抑止
- BuddyWorksの明示Resetによる解除を維持
- 対象アバター選択式のSetup画面を追加
- BuddyWorks Package原本を変更しない非破壊生成方式を採用
- VRCFury Base Locomotion重複の事前検出を追加
- NDMF/VRCFuryビルド後検証を追加
- 公開済みVRChatアバターで50枠＋上書き50枠、計100サイクルの実機検証を実施
