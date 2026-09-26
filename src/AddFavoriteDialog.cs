namespace IE11Clone;

/// <summary>
/// 本家 IE11 の「お気に入りの追加」ダイアログを再現。
/// 名前の変更と、追加先フォルダーの選択/新規作成に対応。
/// </summary>
internal sealed class AddFavoriteDialog : Form
{
    private readonly TextBox _nameBox = new();
    private readonly ComboBox _folderCombo = new();

    public string FavoriteName => _nameBox.Text.Trim();
    public string SelectedFolder => string.IsNullOrWhiteSpace(_folderCombo.Text) ? "Favorites" : _folderCombo.Text.Trim();

    public AddFavoriteDialog(string defaultName, IEnumerable<string> folders)
    {
        Text = "お気に入りの追加";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(480, 196);
        Font = new Font("Segoe UI", 9f);

        var icon = new Label
        {
            Text = "\u2605",
            Font = new Font("Segoe UI", 20f),
            ForeColor = Color.FromArgb(240, 180, 30),
            Location = new Point(16, 14),
            Size = new Size(36, 36),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        var titleLabel = new Label
        {
            Text = "お気に入りの追加",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Location = new Point(60, 12),
            AutoSize = true,
        };
        var descLabel = new Label
        {
            Text = "この Web ページをお気に入りとして追加します。お気に入りの項目には\nお気に入りセンターからアクセスできます。",
            Location = new Point(60, 34),
            Size = new Size(404, 36),
        };

        var nameLabel = new Label { Text = "名前(&N):", Location = new Point(16, 92), AutoSize = true };
        _nameBox.Text = defaultName;
        _nameBox.Location = new Point(100, 89);
        _nameBox.Size = new Size(364, 23);

        var folderLabel = new Label { Text = "作成先(&R):", Location = new Point(16, 124), AutoSize = true };
        _folderCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _folderCombo.Location = new Point(100, 121);
        _folderCombo.Size = new Size(280, 23);
        foreach (var f in folders) _folderCombo.Items.Add(f);
        if (_folderCombo.Items.Count == 0) _folderCombo.Items.Add("Favorites");
        _folderCombo.SelectedIndex = 0;

        var newFolderButton = new Button
        {
            Text = "新規フォルダー(&E)",
            Location = new Point(386, 120),
            Size = new Size(78, 25),
        };
        newFolderButton.Click += (s, e) =>
        {
            using var input = new TextInputDialog("新規フォルダーの作成", "フォルダー名(&N):", "新しいフォルダー");
            if (input.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(input.InputText))
            {
                if (!_folderCombo.Items.Contains(input.InputText))
                    _folderCombo.Items.Add(input.InputText);
                _folderCombo.SelectedItem = input.InputText;
            }
        };

        var addButton = new Button
        {
            Text = "追加(&A)",
            DialogResult = DialogResult.OK,
            Location = new Point(304, 160),
            Size = new Size(80, 25),
        };
        var cancelButton = new Button
        {
            Text = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Location = new Point(390, 160),
            Size = new Size(80, 25),
        };

        AcceptButton = addButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[]
        {
            icon, titleLabel, descLabel, nameLabel, _nameBox, folderLabel, _folderCombo, newFolderButton, addButton, cancelButton
        });
    }
}

/// <summary>
/// 「新規フォルダーの作成」など、1行のテキスト入力を求める簡易ダイアログ。
/// </summary>
internal sealed class TextInputDialog : Form
{
    private readonly TextBox _textBox = new();

    public string InputText => _textBox.Text.Trim();

    public TextInputDialog(string title, string label, string defaultValue)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 120);
        Font = new Font("Segoe UI", 9f);

        var lbl = new Label { Text = label, Location = new Point(16, 16), AutoSize = true };
        _textBox.Text = defaultValue;
        _textBox.Location = new Point(16, 40);
        _textBox.Size = new Size(288, 23);
        _textBox.SelectAll();

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(144, 80), Size = new Size(75, 25) };
        var cancelButton = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Location = new Point(228, 80), Size = new Size(75, 25) };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[] { lbl, _textBox, okButton, cancelButton });
    }
}