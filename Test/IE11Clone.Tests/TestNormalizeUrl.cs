using IE11Clone;
using NUnit.Framework;

namespace IE11Clone.Tests;

[TestFixture]
public class TestNormalizeUrl
{
    private const string HomePage = "https://www.bing.com";

    [Test]
    public void 空文字列はホームページになる()
    {
        Assert.That(UrlNormalizer.Normalize("", HomePage), Is.EqualTo(HomePage));
    }

    [Test]
    public void 空白のみの入力もホームページになる()
    {
        Assert.That(UrlNormalizer.Normalize("   ", HomePage), Is.EqualTo(HomePage));
    }

    [Test]
    public void nullを渡してもホームページになる()
    {
        Assert.That(UrlNormalizer.Normalize(null, HomePage), Is.EqualTo(HomePage));
    }

    [TestCase("https://www.google.com")]
    [TestCase("http://example.com")]
    [TestCase("https://example.com/path?query=1#frag")]
    public void httpまたはhttpsで始まるURLはそのまま返す(string url)
    {
        Assert.That(UrlNormalizer.Normalize(url, HomePage), Is.EqualTo(url));
    }

    [Test]
    public void fileスキームのURLもそのまま返す()
    {
        const string url = "file:///C:/Users/test/index.html";
        Assert.That(UrlNormalizer.Normalize(url, HomePage), Is.EqualTo(url));
    }

    [TestCase("google.com", "https://google.com")]
    [TestCase("www.yahoo.co.jp", "https://www.yahoo.co.jp")]
    [TestCase("localhost.localdomain", "https://localhost.localdomain")]
    public void スキームなしのドメインらしき入力はhttpsを補完する(string input, string expected)
    {
        Assert.That(UrlNormalizer.Normalize(input, HomePage), Is.EqualTo(expected));
    }

    [Test]
    public void 前後の空白はトリムしてから判定する()
    {
        Assert.That(UrlNormalizer.Normalize("  google.com  ", HomePage), Is.EqualTo("https://google.com"));
    }

    [TestCase("インターネット エクスプローラー")]
    [TestCase("how to use webview2")]
    [TestCase("C# tutorial")]
    public void スペースを含む文字列や単語はBing検索クエリになる(string keyword)
    {
        string expected = UrlNormalizer.DefaultSearchEngine + Uri.EscapeDataString(keyword);
        Assert.That(UrlNormalizer.Normalize(keyword, HomePage), Is.EqualTo(expected));
    }

    [Test]
    public void ドットを含まない単語も検索クエリ扱いになる()
    {
        // '.' を含まないため「ドメインらしき入力」とは判定されず、検索クエリとして扱われる
        const string keyword = "google";
        string expected = UrlNormalizer.DefaultSearchEngine + Uri.EscapeDataString(keyword);
        Assert.That(UrlNormalizer.Normalize(keyword, HomePage), Is.EqualTo(expected));
    }

    [Test]
    public void スキーム付きだがhttp系でない入力は検索クエリ扱いになる()
    {
        // 例: "ftp://example.com" は Uri.TryCreate 自体は成功するが、
        // Http/Https/File のいずれでもないため検索クエリとして扱われる想定
        const string input = "ftp://example.com";
        string expected = UrlNormalizer.DefaultSearchEngine + Uri.EscapeDataString(input);
        Assert.That(UrlNormalizer.Normalize(input, HomePage), Is.EqualTo(expected));
    }
}