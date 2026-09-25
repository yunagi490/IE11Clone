# IE11Clone

見た目は **Internet Explorer 11**、中身(レンダリングエンジン)は **WebView2 (Chromium/Edge)** という
デスクトップ ブラウザです。C# / WinForms + WebView2 で実装しています。

## 特徴

- IE11 風のメニューバー(ファイル/編集/表示/お気に入り/ツール/ヘルプ)
- IE11 風のナビゲーション バー(戻る・進む・更新・中止・ホーム・アドレスバー)
- IE11 風のタブ(角丸なしの矩形タブ、+ ボタンで新規タブ、× で閉じる)
- お気に入りバー・お気に入りメニュー(簡易・メモリ内保持)
- ステータス バー(「インターネット | 保護モード: 無効」などの IE 風表示、ズーム表示)
- 中身は WebView2 なので Chrome と同じ Blink/V8 エンジンでレンダリング
- F12 で WebView2 の開発者ツールが開く(Chrome DevTools 相当)
- Ctrl+T(新規タブ)/ Ctrl+W(タブを閉じる)/ Ctrl+L(アドレスバーへ)/ Alt+←→(戻る進む)/ F5(更新)

## 必要環境

- Windows 10 / 11
- .NET 8 SDK
- **Microsoft Edge WebView2 Runtime**
  (Windows 11 や最近の Windows 10 には標準搭載済みのことが多いです。
  無い場合は https://developer.microsoft.com/microsoft-edge/webview2/ から
  「Evergreen Bootstrapper」をインストールしてください)

## ビルドと実行

Windows 上の Visual Studio 2022、または `dotnet` CLI を使います。
このプロジェクトは NuGet パッケージ `Microsoft.Web.WebView2` に依存しているため、
初回ビルド時にインターネット接続で NuGet の復元が必要です。

```powershell
cd IE11Clone
dotnet restore
dotnet build
dotnet run
```

Visual Studio の場合は `IE11Clone.csproj` を開いて F5 で実行できます。

## ファイル構成

- `Program.cs` — エントリポイント
- `MainForm.cs` — IE11 風 UI 全体(メニュー/ツールバー/タブ/ステータスバー)とタブ管理、
  WebView2 のイベント配線
- `Ie11ColorTable.cs` — ツールバー/メニューの水色系グラデーションを再現する
  `ProfessionalColorTable` のカスタム実装
- `app.manifest` — DPI 対応マニフェスト

## カスタマイズのヒント

- `MainForm.cs` 内の `HomePage` 定数でホームページの URL を変更できます。
- ツールバーのアイコンは `Segoe MDL2 Assets` フォントのグリフ文字で表現しています
  (画像アセット不要)。実際のアイコン画像に差し替えたい場合は `CreateGlyphButton` を
  `Image` プロパティを使う形に書き換えてください。
- アプリ アイコンを付けたい場合は `.ico` ファイルを追加し、`IE11Clone.csproj` に
  `<ApplicationIcon>your.ico</ApplicationIcon>` を追記してください。
