# FALCON-Dashboard

FALCON(隼専用AIアシスタント)のサブ画面として動く、Windows専用デスクトップアプリ。

FALCON本体([FALCON](https://github.com/Avuorina/FALCON))のサーバー(`server.py`)にHTTPS(Tailscale経由)で接続し、
チャットの他、各種サブシステム(Minecraftサーバーのログ表示など)を専用画面で確認できるようにする。

## 現在の機能

- **チャット** … FALCON本体と同じ会話履歴を共有して会話できる
- **Minecraftログ** … 登録済みMinecraftサーバーのログをリアルタイム(3秒おきポーリング)表示

## 必要なもの

- .NET 8.0
- FALCON本体(`server.py`)がTailscale経由のHTTPSで起動していること

## 開発環境

C# / WPF (.NET 8.0)、MVVMパターン。[MCServerManager](https://github.com/Avuorina/MCserverManager)と同様の構成。

## ライセンス

[MIT License](LICENSE)