# 変更履歴

## 2.3.0 - 2026-08-16

- 空スロットを`Valid=false`で明示し、未保存LoadがStandingを呼ぶ問題を修正
- Save時に`PE/Set`、`PE/Float`、`Valid=true`を保存
- Loadは`Valid=true`のスロットだけ実行
- Any Stateから各Save／Loadへ直接遷移し、連続入力の取りこぼしを修正
- MA版とVRCFury版の両方で生成・NDMF後検証と実機確認を実施
- 必須ツールと検証済みバージョンを利用規約へ明記

版固有の履歴は以下を参照してください。

- [MA版](Source/com.poseslotextension.ma/CHANGELOG_JA.md)
- [VRCFury版](Source/com.poseslotextension.vrcfury/CHANGELOG_JA.md)
