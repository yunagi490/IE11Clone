using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace IE11Clone;

/// <summary>
/// 見た目は Internet Explorer 11、中身(レンダリングエンジン)は WebView2(Chromium) の
/// デスクトップブラウザ アプリケーション。
/// </summary>
public class MainForm : Form
{
    private const string HomePage = "https://www.bing.com";
    private const string NewTabPlusKey = "__newtab__";
    private const int CloseButtonSize = 16;

    private readonly MenuStrip _menuStrip = new();
    private readonly ToolStrip _navToolStrip = new();
    private readonly ToolStrip _favoritesBar = new();
    private readonly TabControl _tabControl = new();
    private readonly StatusStrip _statusStrip = new();

    private ToolStripButton _btnBack = null!;
    private ToolStripButton _btnForward = null!;
    private ToolStripButton _btnRefresh = null!;
    private ToolStripButton _btnStop = null!;
    private ToolStripButton _btnHome = null!;
    private ToolStripComboBox _addressBar = null!;
    private ToolStripButton _btnGo = null!;

    private ToolStripMenuItem _favoritesMenu = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _zoneLabel = null!;
    private ToolStripStatusLabel _zoomLabel = null!;
    private ToolStripProgressBar _progressBar = null!;

    private readonly List<(string Title, string Url)> _favorites = new();

    // --- お気に入りセンター(サイドパネル) ---
    private Panel _favoritesPanel = null!;
    private TreeView _favoritesTree = null!;
    private Panel _feedsPanel = null!;
    private Panel _historyPanel = null!;
    private ListBox _historyListBox = null!;
    private Label _tabFav = null!;
    private Label _tabFeeds = null!;
    private Label _tabHistory = null!;
    private readonly List<HistoryEntry> _history = new();

    private sealed record HistoryEntry(string Title, string Url)
    {
        public override string ToString() => Title;
    }

    public MainForm()
    {
        Text = "使用したことのないページ - Internet Explorer";
        Size = new Size(1200, 800);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;

        ToolStripManager.Renderer = new ToolStripProfessionalRenderer(new Ie11ColorTable());

        BuildMenuStrip();
        BuildNavToolStrip();
        BuildFavoritesBar();
        BuildFavoritesCenterPanel();
        BuildTabControl();
        BuildStatusStrip();

        // Dock 順序: Fill(中身)→左パネル→下→上...の順で追加すると、
        // 後から追加したものほど優先的に端を確保するため、
        // メニュー/ナビゲーション/お気に入りバーが全幅の帯として最上部に並び、
        // お気に入りセンターはその下・ステータスバーの上の範囲だけを左側から確保する。
        Controls.Add(_tabControl);
        Controls.Add(_favoritesPanel);
        Controls.Add(_statusStrip);
        Controls.Add(_favoritesBar);
        Controls.Add(_navToolStrip);
        Controls.Add(_menuStrip);
        MainMenuStrip = _menuStrip;

        Load += (s, e) => UpdateAddressBarWidth();
        _navToolStrip.SizeChanged += (s, e) => UpdateAddressBarWidth();

        // 本家 IE11 のお気に入りセンター初期状態(Bing/Google)を再現
        AddFavorite("Bing", "https://www.bing.com");
        AddFavorite("Google", "https://www.google.com");

        AddNewTab(HomePage);
    }

    // ============================================================
    //  メニュー バー
    // ============================================================
    private void BuildMenuStrip()
    {
        _menuStrip.BackColor = Color.Transparent;

        var fileMenu = new ToolStripMenuItem("ファイル(&F)");
        var newTabItem = new ToolStripMenuItem("新しいタブ(&T)", null, (s, e) => AddNewTab(HomePage)) { ShortcutKeys = Keys.Control | Keys.T };
        var newWindowItem = new ToolStripMenuItem("新しいウィンドウ(&N)", null, (s, e) => new MainForm().Show()) { ShortcutKeys = Keys.Control | Keys.N };
        var closeTabItem = new ToolStripMenuItem("タブを閉じる(&C)", null, (s, e) =>
        {
            if (_tabControl.SelectedTab != null) CloseTab(_tabControl.SelectedTab);
        })
        { ShortcutKeys = Keys.Control | Keys.W };
        var exitItem = new ToolStripMenuItem("終了(&X)", null, (s, e) => Close());
        fileMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            newTabItem, newWindowItem, closeTabItem, new ToolStripSeparator(), exitItem
        });

        var editMenu = new ToolStripMenuItem("編集(&E)");
        editMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem("切り取り(&T)", null, (s, e) => ExecClipboardCommand("cut")),
            new ToolStripMenuItem("コピー(&C)", null, (s, e) => ExecClipboardCommand("copy")),
            new ToolStripMenuItem("貼り付け(&P)", null, (s, e) => ExecClipboardCommand("paste")),
        });

        var viewMenu = new ToolStripMenuItem("表示(&V)");
        var toolbarsMenu = new ToolStripMenuItem("ツール バー(&T)");
        var favBarToggle = new ToolStripMenuItem("お気に入りバー(&F)") { CheckOnClick = true, Checked = true };
        favBarToggle.Click += (s, e) => _favoritesBar.Visible = favBarToggle.Checked;
        toolbarsMenu.DropDownItems.Add(favBarToggle);

        var zoomMenu = new ToolStripMenuItem("拡大/縮小(&Z)");
        foreach (var pct in new[] { 50, 75, 100, 125, 150, 200 })
        {
            var item = new ToolStripMenuItem($"{pct}%");
            item.Click += (s, e) =>
            {
                var wv = GetActiveWebView();
                if (wv != null) wv.ZoomFactor = pct / 100.0;
                _zoomLabel.Text = $"{pct}%";
            };
            zoomMenu.DropDownItems.Add(item);
        }
        viewMenu.DropDownItems.Add(toolbarsMenu);
        viewMenu.DropDownItems.Add(zoomMenu);
        viewMenu.DropDownItems.Add(new ToolStripMenuItem("最新の情報に更新(&R)", null, (s, e) => GetActiveWebView()?.Reload()) { ShortcutKeys = Keys.F5 });

        _favoritesMenu = new ToolStripMenuItem("お気に入り(&A)");
        var addFavItem = new ToolStripMenuItem("お気に入りに追加(&D)...", null, (s, e) => AddCurrentPageToFavorites());
        _favoritesMenu.DropDownItems.Add(addFavItem);
        _favoritesMenu.DropDownItems.Add(new ToolStripSeparator());

        var toolsMenu = new ToolStripMenuItem("ツール(&S)");
        toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem("ポップアップ ブロック(&P)") { CheckOnClick = true, Checked = true },
            new ToolStripMenuItem("開発者ツール(&D)", null, (s, e) => GetActiveWebView()?.CoreWebView2?.OpenDevToolsWindow()) { ShortcutKeys = Keys.F12 },
            new ToolStripSeparator(),
            new ToolStripMenuItem("インターネット オプション(&O)...", null, (s, e) =>
                MessageBox.Show(this,
                    "このデモ実装ではインターネット オプションの設定画面は実装されていません。",
                    "インターネット オプション", MessageBoxButtons.OK, MessageBoxIcon.Information)),
        });

        var helpMenu = new ToolStripMenuItem("ヘルプ(&H)");
        helpMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem("Internet Explorer ヘルプ(&H)", null, (s, e) =>
                MessageBox.Show(this, "ヘルプ コンテンツはこのデモには含まれていません。", "ヘルプ")),
            new ToolStripMenuItem("Internet Explorer について(&A)", null, (s, e) => ShowAboutDialog()),
        });

        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
            fileMenu, editMenu, viewMenu, _favoritesMenu, toolsMenu, helpMenu
        });
    }

    private void ShowAboutDialog()
    {
        MessageBox.Show(this,
            "Internet Explorer\nバージョン 11.0.0 (互換モード)\n\n" +
            "この製品はレンダリング エンジンとして\n" +
            "Microsoft Edge WebView2 (Chromium) を使用しています。\n\n" +
            "見た目は IE11、中身は Chrome 系エンジンという構成の\n" +
            "デモ アプリケーションです。",
            "Internet Explorer について",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecClipboardCommand(string command)
    {
        _ = GetActiveWebView()?.CoreWebView2?.ExecuteScriptAsync($"document.execCommand('{command}')");
    }

    private void AddCurrentPageToFavorites()
    {
        var webView = GetActiveWebView();
        if (webView?.CoreWebView2 == null || webView.Source == null) return;
        var title = string.IsNullOrWhiteSpace(webView.CoreWebView2.DocumentTitle)
            ? webView.Source.ToString()
            : webView.CoreWebView2.DocumentTitle;
        AddFavorite(title, webView.Source.ToString());
    }

    private void AddFavorite(string title, string url)
    {
        _favorites.Add((title, url));

        var menuItem = new ToolStripMenuItem(title);
        menuItem.Click += (s, e) => Navigate(url);
        _favoritesMenu.DropDownItems.Add(menuItem);

        var barButton = new ToolStripButton(title) { AutoSize = true, DisplayStyle = ToolStripItemDisplayStyle.Text };
        barButton.Click += (s, e) => Navigate(url);
        _favoritesBar.Items.Add(barButton);

        RefreshFavoritesTree();
    }

    // ============================================================
    //  ナビゲーション ツール バー(戻る/進む/更新/中止/ホーム/アドレスバー)
    // ============================================================
    private void BuildNavToolStrip()
    {
        _navToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _navToolStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        _navToolStrip.Padding = new Padding(4, 3, 4, 3);

        _btnBack = CreateGlyphButton("\uE72B", "戻る");
        _btnForward = CreateGlyphButton("\uE72A", "進む");
        _btnForward.Enabled = false;
        _btnBack.Enabled = false;
        _btnRefresh = CreateGlyphButton("\uE72C", "最新の情報に更新");
        _btnStop = CreateGlyphButton("\uE711", "中止");
        _btnStop.Enabled = false;
        _btnHome = CreateGlyphButton("\uE80F", "ホーム");

        _addressBar = new ToolStripComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoSize = false,
            Width = 500,
            Font = new Font("Segoe UI", 9.5f),
        };

        _btnGo = CreateGlyphButton("\uE72A", "移動");
        var btnFavStar = CreateGlyphButton("\uE734", "お気に入り、フィード、履歴の表示");
        var btnTools = CreateGlyphButton("\uE713", "ツール");

        _btnBack.Click += (s, e) => GetActiveWebView()?.GoBack();
        _btnForward.Click += (s, e) => GetActiveWebView()?.GoForward();
        _btnRefresh.Click += (s, e) => GetActiveWebView()?.Reload();
        _btnStop.Click += (s, e) => GetActiveWebView()?.Stop();
        _btnHome.Click += (s, e) => Navigate(HomePage);
        _btnGo.Click += (s, e) => NavigateFromAddressBar();
        btnFavStar.Click += (s, e) => ToggleFavoritesPanel();
        btnTools.Click += (s, e) => MessageBox.Show(this, "ツール メニューをご利用ください。", "ツール");

        _addressBar.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                NavigateFromAddressBar();
            }
        };

        _navToolStrip.Items.AddRange(new ToolStripItem[]
        {
            _btnBack, _btnForward, _btnRefresh, _btnStop, _btnHome,
            new ToolStripSeparator(),
            _addressBar, _btnGo,
            new ToolStripSeparator(),
            btnFavStar, btnTools
        });
    }

    private static ToolStripButton CreateGlyphButton(string glyph, string tooltip)
    {
        return new ToolStripButton
        {
            Text = glyph,
            Font = new Font("Segoe MDL2 Assets", 12f),
            ToolTipText = tooltip,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AutoSize = true,
            Margin = new Padding(2, 1, 2, 2),
        };
    }

    private void UpdateAddressBarWidth()
    {
        if (_addressBar == null) return;
        int used = 0;
        foreach (ToolStripItem item in _navToolStrip.Items)
        {
            if (item == _addressBar) continue;
            used += item.Width + item.Margin.Horizontal;
        }
        int newWidth = _navToolStrip.Width - used - 20;
        _addressBar.Width = Math.Max(150, newWidth);
    }

    // ============================================================
    //  お気に入りバー
    // ============================================================
    private void BuildFavoritesBar()
    {
        _favoritesBar.GripStyle = ToolStripGripStyle.Hidden;
        _favoritesBar.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        _favoritesBar.Height = 24;
    }

    // ============================================================
    //  お気に入りセンター(左サイドパネル: お気に入り/フィード/履歴)
    // ============================================================
    private void ToggleFavoritesPanel()
    {
        _favoritesPanel.Visible = !_favoritesPanel.Visible;
        if (_favoritesPanel.Visible) RefreshFavoritesTree();
    }

    private void BuildFavoritesCenterPanel()
    {
        _favoritesPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 260,
            Visible = false,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
        };

        // 上段: 「お気に入りに追加」ドロップダウン + 閉じるボタン
        var topStrip = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.ManagerRenderMode,
            Dock = DockStyle.Top,
        };
        var addFavDropDown = new ToolStripDropDownButton("お気に入りに追加")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        addFavDropDown.DropDownItems.Add("お気に入りに追加(&A)...", null, (s, e) => AddCurrentPageToFavorites());
        addFavDropDown.DropDownItems.Add("お気に入りバーに追加(&B)", null, (s, e) => AddCurrentPageToFavorites());
        var closeBtn = new ToolStripButton("×")
        {
            Alignment = ToolStripItemAlignment.Right,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = "閉じる",
        };
        closeBtn.Click += (s, e) => _favoritesPanel.Visible = false;
        topStrip.Items.Add(addFavDropDown);
        topStrip.Items.Add(closeBtn);

        // 中段: お気に入り / フィード / 履歴 の擬似タブ
        var tabStripPanel = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(240, 240, 240) };
        var tabFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _tabFav = CreateFavTabLabel("お気に入り");
        _tabFeeds = CreateFavTabLabel("フィード");
        _tabHistory = CreateFavTabLabel("履歴");
        tabFlow.Controls.AddRange(new Control[] { _tabFav, _tabFeeds, _tabHistory });
        tabStripPanel.Controls.Add(tabFlow);

        // 下段: コンテンツ(お気に入りツリー/フィード プレースホルダー/履歴リスト)
        _favoritesTree = new TreeView { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Visible = true };
        _favoritesTree.NodeMouseClick += (s, e) =>
        {
            if (e.Node?.Tag is string url) Navigate(url);
        };

        _feedsPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
        _feedsPanel.Controls.Add(new Label
        {
            Text = "このページに登録されているフィードはありません。",
            Dock = DockStyle.Top,
            Padding = new Padding(10),
            AutoSize = false,
            Height = 60,
        });

        _historyListBox = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
        _historyListBox.MouseClick += (s, e) =>
        {
            int index = _historyListBox.IndexFromPoint(e.Location);
            if (index >= 0 && _historyListBox.Items[index] is HistoryEntry entry) Navigate(entry.Url);
        };
        _historyPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
        _historyPanel.Controls.Add(_historyListBox);

        var contentHost = new Panel { Dock = DockStyle.Fill };
        contentHost.Controls.Add(_favoritesTree);
        contentHost.Controls.Add(_historyPanel);
        contentHost.Controls.Add(_feedsPanel);

        _favoritesPanel.Controls.Add(contentHost);
        _favoritesPanel.Controls.Add(tabStripPanel);
        _favoritesPanel.Controls.Add(topStrip);

        SelectFavoritesTab(_tabFav);
        RefreshFavoritesTree();
    }

    private Label CreateFavTabLabel(string text)
    {
        var lbl = new Label
        {
            Text = text,
            AutoSize = false,
            Width = 84,
            Height = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(240, 240, 240),
        };
        lbl.Click += (s, e) => SelectFavoritesTab(lbl);
        return lbl;
    }

    private void SelectFavoritesTab(Label selected)
    {
        foreach (var lbl in new[] { _tabFav, _tabFeeds, _tabHistory })
        {
            bool isSelected = lbl == selected;
            lbl.BackColor = isSelected ? Color.White : Color.FromArgb(240, 240, 240);
            lbl.Font = new Font(lbl.Font, isSelected ? FontStyle.Bold : FontStyle.Regular);
        }
        _favoritesTree.Visible = selected == _tabFav;
        _feedsPanel.Visible = selected == _tabFeeds;
        _historyPanel.Visible = selected == _tabHistory;
    }

    private void RefreshFavoritesTree()
    {
        if (_favoritesTree == null) return;
        _favoritesTree.Nodes.Clear();
        // 本家 IE11 のお気に入りセンターに合わせ、Links / リンク フォルダーを先頭に置き、
        // 追加したお気に入りはその後にフラットな一覧として並べる。
        _favoritesTree.Nodes.Add(new TreeNode("Links"));
        _favoritesTree.Nodes.Add(new TreeNode("リンク"));
        foreach (var fav in _favorites)
        {
            _favoritesTree.Nodes.Add(new TreeNode(fav.Title) { Tag = fav.Url });
        }
    }

    private void RefreshHistoryList()
    {
        if (_historyListBox == null) return;
        _historyListBox.Items.Clear();
        foreach (var entry in _history)
        {
            _historyListBox.Items.Add(entry);
        }
    }

    // ============================================================
    //  タブ ストリップ
    // ============================================================
    private void BuildTabControl()
    {
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.SizeMode = TabSizeMode.Fixed;
        _tabControl.ItemSize = new Size(170, 28);
        _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabControl.Padding = new Point(10, 4);
        _tabControl.HotTrack = true;

        var plusTab = new TabPage("+") { Name = NewTabPlusKey };
        _tabControl.TabPages.Add(plusTab);

        _tabControl.DrawItem += TabControl_DrawItem;
        _tabControl.MouseDown += TabControl_MouseDown;
        _tabControl.Selecting += TabControl_Selecting;
        _tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
    }

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tabPage = _tabControl.TabPages[e.Index];
        var rect = _tabControl.GetTabRect(e.Index);
        bool isPlus = tabPage.Name == NewTabPlusKey;
        bool isSelected = e.Index == _tabControl.SelectedIndex;

        Color back = isSelected ? Color.White : Color.FromArgb(214, 228, 246);
        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, rect);
        using (var pen = new Pen(Color.FromArgb(163, 189, 221)))
        {
            e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
            e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);
            e.Graphics.DrawLine(pen, rect.Right - 1, rect.Top, rect.Right - 1, rect.Bottom);
        }

        if (isPlus)
        {
            TextRenderer.DrawText(e.Graphics, "+", new Font("Segoe UI", 12f, FontStyle.Bold), rect,
                Color.FromArgb(70, 70, 70), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var iconRect = new Rectangle(rect.Left + 8, rect.Top + (rect.Height - 16) / 2, 16, 16);
        TextRenderer.DrawText(e.Graphics, "\uE774", new Font("Segoe MDL2 Assets", 9f), iconRect,
            Color.FromArgb(90, 110, 140), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var closeRect = GetCloseButtonRect(e.Index);
        var textRect = new Rectangle(iconRect.Right + 4, rect.Top,
            Math.Max(10, rect.Width - iconRect.Width - CloseButtonSize - 26), rect.Height);
        string title = string.IsNullOrEmpty(tabPage.Text) ? "新しいタブ" : tabPage.Text;
        TextRenderer.DrawText(e.Graphics, title, Font, textRect, Color.Black,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(e.Graphics, "\uE711", new Font("Segoe MDL2 Assets", 8f), closeRect,
            Color.FromArgb(90, 90, 90), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private Rectangle GetCloseButtonRect(int index)
    {
        var rect = _tabControl.GetTabRect(index);
        return new Rectangle(rect.Right - CloseButtonSize - 8, rect.Top + (rect.Height - CloseButtonSize) / 2,
            CloseButtonSize, CloseButtonSize);
    }

    private void TabControl_MouseDown(object? sender, MouseEventArgs e)
    {
        for (int i = 0; i < _tabControl.TabCount; i++)
        {
            var tabPage = _tabControl.TabPages[i];
            if (tabPage.Name == NewTabPlusKey) continue;
            if (GetCloseButtonRect(i).Contains(e.Location))
            {
                CloseTab(tabPage);
                return;
            }
        }
    }

    private void TabControl_Selecting(object? sender, TabControlCancelEventArgs e)
    {
        if (e.TabPage?.Name == NewTabPlusKey)
        {
            e.Cancel = true;
            AddNewTab(HomePage);
        }
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTab?.Tag is WebView2 webView)
        {
            _addressBar.Text = webView.Source?.ToString() ?? string.Empty;
            UpdateNavButtons(webView);
            var titleText = webView.CoreWebView2?.DocumentTitle;
            Text = string.IsNullOrWhiteSpace(titleText) ? "Internet Explorer" : $"{titleText} - Internet Explorer";
        }
    }

    // ============================================================
    //  タブ(WebView2)の生成/破棄
    // ============================================================
    private void AddNewTab(string url) => _ = AddNewTabAsync(url);

    private async Task<TabPage?> AddNewTabAsync(string url)
    {
        var webView = new WebView2 { Dock = DockStyle.Fill };
        var tabPage = new TabPage("新しいタブ") { Tag = webView };
        tabPage.Controls.Add(webView);

        int insertIndex = _tabControl.TabPages.IndexOfKey(NewTabPlusKey);
        if (insertIndex < 0) insertIndex = _tabControl.TabCount;
        _tabControl.TabPages.Insert(insertIndex, tabPage);
        _tabControl.SelectedTab = tabPage;

        try
        {
            await webView.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "WebView2 ランタイムの初期化に失敗しました。\n" +
                "Microsoft Edge WebView2 Runtime がインストールされているか確認してください。\n\n" +
                ex.Message,
                "初期化エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return tabPage;
        }

        var core = webView.CoreWebView2!;
        core.NavigationStarting += (s, e) => OnNavigationStarting(tabPage);
        core.NavigationCompleted += (s, e) => OnNavigationCompleted(webView, tabPage);
        core.SourceChanged += (s, e) => OnSourceChanged(webView, tabPage);
        core.DocumentTitleChanged += (s, e) => OnTitleChanged(webView, tabPage);
        core.HistoryChanged += (s, e) => UpdateNavButtonsIfActive(webView, tabPage);
        core.NewWindowRequested += (s, e) =>
        {
            e.Handled = true;
            _ = AddNewTabAsync(e.Uri);
        };

        webView.Source = new Uri(NormalizeUrl(url));
        return tabPage;
    }

    private void CloseTab(TabPage tabPage)
    {
        if (tabPage.Name == NewTabPlusKey) return;
        if (RealTabCount() <= 1) return; // 最後の1枚は閉じない

        int idx = _tabControl.TabPages.IndexOf(tabPage);
        bool wasSelected = _tabControl.SelectedTab == tabPage;

        _tabControl.TabPages.Remove(tabPage);
        if (tabPage.Tag is WebView2 wv) wv.Dispose();
        tabPage.Dispose();

        if (wasSelected)
        {
            int newIndex = Math.Min(idx, RealTabCount() - 1);
            _tabControl.SelectedIndex = Math.Max(0, newIndex);
        }
    }

    private int RealTabCount() => _tabControl.TabPages.Cast<TabPage>().Count(t => t.Name != NewTabPlusKey);

    private WebView2? GetActiveWebView() => _tabControl.SelectedTab?.Tag as WebView2;

    private bool IsActive(TabPage tabPage) => _tabControl.SelectedTab == tabPage;

    // ============================================================
    //  ナビゲーション イベント ハンドラ
    // ============================================================
    private void OnNavigationStarting(TabPage tabPage)
    {
        if (!IsActive(tabPage)) return;
        _statusLabel.Text = "ページを開いています...";
        _progressBar.Visible = true;
        _btnStop.Enabled = true;
    }

    private void OnNavigationCompleted(WebView2 webView, TabPage tabPage)
    {
        if (IsActive(tabPage))
        {
            _statusLabel.Text = "完了";
            _progressBar.Visible = false;
            _btnStop.Enabled = false;
            UpdateNavButtons(webView);
        }

        if (webView.Source != null)
        {
            var title = string.IsNullOrWhiteSpace(webView.CoreWebView2?.DocumentTitle)
                ? webView.Source.ToString()
                : webView.CoreWebView2!.DocumentTitle;
            _history.Insert(0, new HistoryEntry(title, webView.Source.ToString()));
            if (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
            RefreshHistoryList();
        }
    }

    private void OnSourceChanged(WebView2 webView, TabPage tabPage)
    {
        if (IsActive(tabPage))
        {
            _addressBar.Text = webView.Source?.ToString() ?? string.Empty;
        }
    }

    private void OnTitleChanged(WebView2 webView, TabPage tabPage)
    {
        string title = string.IsNullOrWhiteSpace(webView.CoreWebView2?.DocumentTitle)
            ? "新しいタブ"
            : webView.CoreWebView2!.DocumentTitle;
        tabPage.Text = title.Length > 22 ? title[..22] + "…" : title;
        _tabControl.Invalidate();
        if (IsActive(tabPage))
        {
            Text = $"{title} - Internet Explorer";
        }
    }

    private void UpdateNavButtonsIfActive(WebView2 webView, TabPage tabPage)
    {
        if (IsActive(tabPage)) UpdateNavButtons(webView);
    }

    private void UpdateNavButtons(WebView2 webView)
    {
        _btnBack.Enabled = webView.CanGoBack;
        _btnForward.Enabled = webView.CanGoForward;
    }

    private void Navigate(string url)
    {
        var webView = GetActiveWebView();
        if (webView == null) return;
        webView.Source = new Uri(NormalizeUrl(url));
    }

    private void NavigateFromAddressBar() => Navigate(_addressBar.Text);

    private static string NormalizeUrl(string input)
    {
        input = input.Trim();
        if (string.IsNullOrEmpty(input)) return HomePage;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "file"))
        {
            return input;
        }

        bool looksLikeDomain = !input.Contains(' ') && input.Contains('.') && !input.Contains("://");
        if (looksLikeDomain) return "https://" + input;

        return "https://www.bing.com/search?q=" + Uri.EscapeDataString(input);
    }

    // ============================================================
    //  ステータス バー
    // ============================================================
    private void BuildStatusStrip()
    {
        _statusLabel = new ToolStripStatusLabel("完了") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _progressBar = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30, Visible = false, Width = 100 };
        _zoneLabel = new ToolStripStatusLabel("インターネット | 保護モード: 無効");
        _zoomLabel = new ToolStripStatusLabel("100%");

        _statusStrip.Items.AddRange(new ToolStripItem[]
        {
            _statusLabel, _progressBar, new ToolStripStatusLabel("|"), _zoneLabel, new ToolStripStatusLabel("|"), _zoomLabel
        });
    }

    // ============================================================
    //  キーボード ショートカット
    // ============================================================
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.T:
                AddNewTab(HomePage);
                return true;
            case Keys.Control | Keys.W:
                if (_tabControl.SelectedTab != null) CloseTab(_tabControl.SelectedTab);
                return true;
            case Keys.Control | Keys.L:
                _addressBar.Focus();
                _addressBar.ComboBox?.SelectAll();
                return true;
            case Keys.F5:
                GetActiveWebView()?.Reload();
                return true;
            case Keys.Alt | Keys.Left:
                GetActiveWebView()?.GoBack();
                return true;
            case Keys.Alt | Keys.Right:
                GetActiveWebView()?.GoForward();
                return true;
            case Keys.F12:
                GetActiveWebView()?.CoreWebView2?.OpenDevToolsWindow();
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}