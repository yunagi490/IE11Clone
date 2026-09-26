namespace IE11Clone;

/// <summary>
/// アドレスバーへの入力を、実際にナビゲートできる URL へ正規化する。
/// WinForms や WebView2 に依存しない純粋なロジックとして MainForm から切り出してあるので、
/// NUnit などのユニットテストから直接呼び出せる。
/// </summary>
public static class UrlNormalizer
{
    /// <summary>キーワード検索とみなされた場合に使う検索エンジンのベース URL。</summary>
    public const string DefaultSearchEngine = "https://www.bing.com/search?q=";

    /// <summary>
    /// アドレスバーの入力文字列を URL に正規化する。
    /// </summary>
    /// <param name="input">アドレスバーに入力された文字列。</param>
    /// <param name="homePage">入力が空だった場合に返すホームページの URL。</param>
    public static string Normalize(string? input, string homePage)
    {
        input = input?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(input)) return homePage;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "file"))
        {
            return input;
        }

        bool looksLikeDomain = !input.Contains(' ') && input.Contains('.') && !input.Contains("://");
        if (looksLikeDomain) return "https://" + input;

        return DefaultSearchEngine + Uri.EscapeDataString(input);
    }
}