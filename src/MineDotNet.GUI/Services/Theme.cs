using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MineDotNet.GUI.Services
{
    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(30, 30, 33);
        public static readonly Color Surface = Color.FromArgb(45, 45, 50);
        public static readonly Color SurfaceAlt = Color.FromArgb(60, 60, 66);
        public static readonly Color Border = Color.FromArgb(70, 70, 78);
        public static readonly Color TextPrimary = Color.FromArgb(230, 230, 235);
        public static readonly Color TextSecondary = Color.FromArgb(170, 170, 180);
        public static readonly Color TextMuted = Color.FromArgb(130, 130, 140);
        public static readonly Color Accent = Color.FromArgb(0, 122, 204);
        public static readonly Color AccentHover = Color.FromArgb(20, 150, 235);
        public static readonly Color AccentPressed = Color.FromArgb(0, 90, 170);
        public static readonly Color Danger = Color.FromArgb(200, 80, 80);
        public static readonly Color DangerHover = Color.FromArgb(220, 100, 100);

        public static Font GetUIFont(float size = 9F, FontStyle style = FontStyle.Regular)
            => new Font("Segoe UI", size, style, GraphicsUnit.Point);

        public static Font GetMonoFont(float size = 9.5F)
            => new Font("Consolas", size, FontStyle.Regular, GraphicsUnit.Point);

        public static void Apply(Control root)
        {
            if (root is Form form)
            {
                form.Load += (s, e) => TryEnableDarkTitleBar(form);
                form.HandleCreated += (s, e) => TryEnableDarkTitleBar(form);
            }
            ApplyToControl(root);
        }

        private static void ApplyToControl(Control c)
        {
            switch (c)
            {
                case Form f:
                    f.BackColor = Background;
                    f.ForeColor = TextPrimary;
                    f.Font = GetUIFont();
                    break;
                case Button b:
                    if ("danger".Equals(b.Tag)) StyleDangerButton(b);
                    else if ("secondary".Equals(b.Tag)) StyleSecondaryButton(b);
                    else StyleButton(b);
                    break;
                case CheckBox cb:
                    cb.BackColor = Color.Transparent;
                    cb.ForeColor = TextPrimary;
                    cb.FlatStyle = FlatStyle.Flat;
                    break;
                case RichTextBox rtb:
                    rtb.BackColor = SurfaceAlt;
                    rtb.ForeColor = TextPrimary;
                    rtb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case TextBox tb:
                    tb.BackColor = SurfaceAlt;
                    tb.ForeColor = TextPrimary;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case CheckedListBox clb:
                    clb.BackColor = SurfaceAlt;
                    clb.ForeColor = TextPrimary;
                    clb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ListBox lb:
                    lb.BackColor = SurfaceAlt;
                    lb.ForeColor = TextPrimary;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case NumericUpDown n:
                    n.BackColor = SurfaceAlt;
                    n.ForeColor = TextPrimary;
                    n.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox combo:
                    combo.BackColor = SurfaceAlt;
                    combo.ForeColor = TextPrimary;
                    combo.FlatStyle = FlatStyle.Flat;
                    break;
                case TrackBar tbar:
                    tbar.BackColor = Background;
                    break;
                case GroupBox gb:
                    gb.BackColor = Surface;
                    gb.ForeColor = TextSecondary;
                    break;
                case Panel panel:
                    if (panel.BackColor == SystemColors.Control || panel.BackColor == Color.Empty)
                        panel.BackColor = Surface;
                    panel.ForeColor = TextPrimary;
                    break;
                case Label lbl:
                    lbl.BackColor = Color.Transparent;
                    // Only override the default control-text colour so callers can opt a specific
                    // label into a different tint (e.g. section headers) before Apply runs.
                    if (lbl.ForeColor == SystemColors.ControlText || lbl.ForeColor == Color.Empty)
                    {
                        lbl.ForeColor = "header".Equals(lbl.Tag) ? TextMuted : TextPrimary;
                    }
                    break;
                case PictureBox pb:
                    pb.BackColor = Color.FromArgb(22, 22, 25);
                    pb.BorderStyle = BorderStyle.None;
                    break;
                case UserControl uc:
                    uc.BackColor = Background;
                    uc.ForeColor = TextPrimary;
                    break;
            }

            foreach (Control child in c.Controls)
            {
                ApplyToControl(child);
            }
        }

        public static void StyleButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Accent;
            b.ForeColor = Color.White;
            b.Font = GetUIFont(9F, FontStyle.Regular);
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.BorderColor = Accent;
            b.FlatAppearance.MouseOverBackColor = AccentHover;
            b.FlatAppearance.MouseDownBackColor = AccentPressed;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            HookEnabledVisual(b);
        }

        public static void StyleDangerButton(Button b)
        {
            StyleButton(b);
            b.BackColor = Danger;
            b.FlatAppearance.BorderColor = Danger;
            b.FlatAppearance.MouseOverBackColor = DangerHover;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 60, 60);
            HookEnabledVisual(b);
        }

        public static void StyleSecondaryButton(Button b)
        {
            StyleButton(b);
            b.BackColor = SurfaceAlt;
            b.ForeColor = TextPrimary;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 86);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 56);
            HookEnabledVisual(b);
        }

        private static readonly Color DisabledBg = Color.FromArgb(50, 50, 55);

        private static void HookEnabledVisual(Button b)
        {
            // Flat buttons don't dim themselves when disabled; fake it by swapping
            // to a muted bg/fg. We also replay this when Enabled flips so the
            // button bounces back to its real colour. Tag drives which "on"
            // palette to restore, so primary/danger/secondary all round-trip.
            b.EnabledChanged -= OnButtonEnabledChanged;
            b.EnabledChanged += OnButtonEnabledChanged;
            ApplyEnabledVisual(b);
        }

        private static void OnButtonEnabledChanged(object sender, EventArgs e)
        {
            if (sender is Button b) ApplyEnabledVisual(b);
        }

        private static void ApplyEnabledVisual(Button b)
        {
            if (!b.Enabled)
            {
                b.BackColor = DisabledBg;
                b.ForeColor = TextMuted;
                b.FlatAppearance.BorderColor = DisabledBg;
                b.Cursor = Cursors.Default;
                return;
            }
            switch (b.Tag as string)
            {
                case "danger":
                    b.BackColor = Danger;
                    b.ForeColor = Color.White;
                    b.FlatAppearance.BorderColor = Danger;
                    break;
                case "secondary":
                    b.BackColor = SurfaceAlt;
                    b.ForeColor = TextPrimary;
                    b.FlatAppearance.BorderColor = Border;
                    break;
                default:
                    b.BackColor = Accent;
                    b.ForeColor = Color.White;
                    b.FlatAppearance.BorderColor = Accent;
                    break;
            }
            b.Cursor = Cursors.Hand;
        }

        private static void TryEnableDarkTitleBar(Form form)
        {
            try
            {
                if (form.Handle == IntPtr.Zero) return;
                int useDark = 1;
                // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 on Windows 10 build 18985+ / Windows 11.
                // Fall back to 19 for earlier builds that honoured the pre-release attribute.
                if (DwmSetWindowAttribute(form.Handle, 20, ref useDark, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(form.Handle, 19, ref useDark, sizeof(int));
                }
            }
            catch
            {
                // DWM isn't present on older Windows; silently skip.
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
