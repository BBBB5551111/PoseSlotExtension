# VRCFury Stable v1.0.1 修正内容

- 存在しない旧版 `PoseSlotExtensionVrcFuryParameters.asset` の参照が残り、VRCFuryビルドに失敗する問題を修正しました。
- 再導入時に旧PoseSlotExtensionのParameters参照を除去し、現行Stable版の参照だけを修復・追加します。
- 対象アバターでVRCFury処理およびNDMFビルド後検証の成功を確認しました。
