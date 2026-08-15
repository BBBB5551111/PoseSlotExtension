# 変更履歴

## 1.0.0-ma-stable - 2026-08-15

- v1.1.5のMA実装を独立パッケージとして固定
- BuddyWorks `[MA]` だけを検出・導入・検証し、VRCFury経路を入口から使用しない構造へ分離
- 旧統合版の導入オブジェクトを、MA版の導入成功後に安全に置換
- 独立アセットルート、独立名前空間、独立NDMFプラグインIDを採用
- 同一アバターへ2回連続で生成・導入・NDMF後検証を行い、両方PASS

## 1.1.5 - 2026-08-15

- MA生成直後のAssetDatabase Importタイミング差により、生成メニューの実グラフを検査せず「Menu Installer is not bound」と誤判定する問題を修正
- `Generated`アセットの参照一致に依存せず、Save/Load 01～50の`PSE/Command`を含む実メニューグラフから結線を検証
- NDMFがMA Menu Installerを処理済みの場合、ビルド用Avatar Descriptorの最終メニューも検査
- 結線が本当に不足している場合、生成アセット、Installer数、参照メニュー、コマンド検出結果を例外へ出力
- 生成処理が配布元READMEを旧テンプレートで上書きしていた問題を修正し、同梱文書を読み取り専用化

## 1.1.4 - 2026-08-15

- 配布Packageが意図的に除外する`Generated`を旧導入環境で削除した際、BuddyWorks `[MA]`の旧複製メニュー参照がMissingになりMA版を未検出と誤判定する問題を修正
- MA Menu Installerの参照だけでなく、BuddyWorks公式Prefabの出自と`BWPosesExtension`本体コンポーネントからMA版を復旧検出
- MAメニュー参照をnullにした一時複製アバターを使う回帰検査をNDMF後検証へ追加

## 1.1.3 - 2026-08-15

- `Pose Slots`をBuddyWorks Poses Extensionの最上位メニューへ移動
- 最上位にあった`Dances`を`More`へ移し、既存項目を失わずメニュー上限8以内を維持
- BuddyWorks Package原本を変更せず、PSEが生成するアバター専用複製メニューだけを加工
- MA版・VRCF版のNDMF後検証へ、最上位配置・Dances移動・メニュー上限の検査を追加
- MA版への再導入時、再生成されたAction Controllerの一時的なMissing参照をBuddyWorks未検出と誤判定する問題を修正

## 1.1.2 - 2026-08-15

- 公開済みVRChatアバターで、Save 01～50、Load 01～50、ジャンプ後のポーズ保持を実機OSC検証
- 異なる50ポーズで全スロットを上書きし、再度Load 01～50とジャンプ後のポーズ保持を実機OSC検証
- ジャンプ判定に`Grounded`だけでなく、上昇・下降の`VelocityY`実測値を追加して全100件PASS
- Save 01～50、Load 01～50、ポーズ保持の固定仕様v1に変更なし

## 1.1.1 - 2026-08-14

- `[MA]` 版と無関係なVRCFuryコンポーネントが同居するアバターで、未生成のPSE複製メニュー（null）をVRCFury参照と誤判定する問題を修正
- BuddyWorks Poses Extension `[MA]` と、BuddyWorks Toolboxなどの別VRCFuryギミックを同時利用できるよう修正
- VRChat SDK 3.10.4、Modular Avatar 1.18.1、NDMF 1.14.4、VRCFury 1.1417.0、BuddyWorks Poses Extension 7.2.1の混在環境でNDMF後検証PASS

## 1.1.0 - 2026-08-14

- BuddyWorks Poses Extension同梱の `[MA]` 版を正式対応へ追加
- `[MA]` 版ではMA Menu Installerを生成メニューへOverrideし、`PSE/Command`をローカル専用パラメータとして追加
- `[MA]` 版のAction Merge Animatorを、ジャンプ等で解除されない生成済みAction Controllerへ非破壊Override
- `[VRCF]` 版と `[MA]` 版を自動判別し、両方の同時導入は競合として停止
- 入れ子Prefabを展開せず、UnityのPrefab Overrideとして参照変更を保存する方式へ修正
- MA直接配置、MA入れ子Prefab、VRCF直接配置、VRCF入れ子Prefabの自動導入テストを追加
- MA/VRCFそれぞれでNDMF後のSave 01～50、Load 01～50、パラメータ、Action Controller構造を検証
- Save 01～50、Load 01～50、ポーズ保持の固定仕様v1に変更なし

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
