namespace IE11Clone;

/// <summary>
/// Internet Explorer 11 のツールバー/メニューの水色系グラデーションを再現するカラーテーブル。
/// </summary>
internal sealed class Ie11ColorTable : ProfessionalColorTable
{
    // IE11 の特徴的な淡いブルー〜グレーのグラデーション
    private static readonly Color TopLight = Color.FromArgb(255, 255, 255);
    private static readonly Color TopMid = Color.FromArgb(233, 242, 252);
    private static readonly Color BottomMid = Color.FromArgb(205, 224, 244);
    private static readonly Color Selected = Color.FromArgb(255, 255, 255);
    private static readonly Color Hover = Color.FromArgb(224, 238, 253);
    private static readonly Color HoverBorder = Color.FromArgb(122, 168, 220);

    public override Color ToolStripGradientBegin => TopLight;
    public override Color ToolStripGradientMiddle => TopMid;
    public override Color ToolStripGradientEnd => BottomMid;

    public override Color MenuStripGradientBegin => TopLight;
    public override Color MenuStripGradientEnd => TopMid;

    public override Color ToolStripBorder => Color.FromArgb(163, 189, 221);

    public override Color ButtonSelectedGradientBegin => Selected;
    public override Color ButtonSelectedGradientMiddle => Selected;
    public override Color ButtonSelectedGradientEnd => Selected;
    public override Color ButtonSelectedBorder => HoverBorder;

    public override Color ButtonPressedGradientBegin => Hover;
    public override Color ButtonPressedGradientMiddle => Hover;
    public override Color ButtonPressedGradientEnd => Hover;
    public override Color ButtonPressedBorder => HoverBorder;

    public override Color ButtonCheckedGradientBegin => Hover;
    public override Color ButtonCheckedGradientMiddle => Hover;
    public override Color ButtonCheckedGradientEnd => Hover;

    public override Color MenuItemSelected => Hover;
    public override Color MenuItemSelectedGradientBegin => Hover;
    public override Color MenuItemSelectedGradientEnd => Hover;
    public override Color MenuItemBorder => HoverBorder;
    public override Color MenuBorder => Color.FromArgb(163, 189, 221);

    public override Color ImageMarginGradientBegin => Color.FromArgb(246, 249, 253);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(238, 244, 252);
    public override Color ImageMarginGradientEnd => Color.FromArgb(230, 239, 250);

    public override Color SeparatorDark => Color.FromArgb(190, 205, 224);
    public override Color SeparatorLight => Color.FromArgb(255, 255, 255);

    public override Color StatusStripGradientBegin => Color.FromArgb(238, 244, 252);
    public override Color StatusStripGradientEnd => Color.FromArgb(219, 232, 248);

    public override Color OverflowButtonGradientBegin => TopMid;
    public override Color OverflowButtonGradientMiddle => BottomMid;
    public override Color OverflowButtonGradientEnd => BottomMid;
}
