using System.Drawing;
using System.Windows.Forms;

namespace SAPVpis.Net47
{
    // Shared visual style helpers — modern flat buttons, SAP-themed colors, body font.
    internal static class UiStyle
    {
        // --- SAP brand colors ---
        public static readonly Color PrimaryBg     = Color.FromArgb(  0, 112, 192); // SAP Blue
        public static readonly Color PrimaryHover  = Color.FromArgb(  0,  90, 160);
        public static readonly Color PrimaryDown   = Color.FromArgb(  0,  74, 132);
        public static readonly Color PrimaryFg     = Color.White;

        public static readonly Color SecondaryBg     = Color.White;
        public static readonly Color SecondaryHover  = Color.FromArgb(235, 240, 248);
        public static readonly Color SecondaryDown   = Color.FromArgb(215, 225, 240);
        public static readonly Color SecondaryFg     = Color.FromArgb( 40,  40,  40);
        public static readonly Color SecondaryBorder = Color.FromArgb(170, 175, 185);

        public static readonly Color DangerBg     = Color.FromArgb(196,  60,  60);
        public static readonly Color DangerHover  = Color.FromArgb(170,  45,  45);

        // --- Fonts ---
        // Body font is what forms set as Form.Font so labels/listboxes/textboxes inherit it.
        public static readonly Font BodyFont           = new Font("Segoe UI",  9.75f, FontStyle.Regular);
        public static readonly Font ButtonFont         = new Font("Segoe UI",  9.75f, FontStyle.Regular);
        public static readonly Font PrimaryButtonFont  = new Font("Segoe UI", 10.0f,  FontStyle.Bold);
        // Menus stay at the OS default size — caller sets MenuStrip.Font = MenuFont.
        public static readonly Font MenuFont           = new Font("Segoe UI",  9.0f,  FontStyle.Regular);

        public static void ApplyPrimary(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = PrimaryHover;
            btn.FlatAppearance.MouseDownBackColor = PrimaryDown;
            btn.BackColor = PrimaryBg;
            btn.ForeColor = PrimaryFg;
            btn.Font = PrimaryButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        public static void ApplySecondary(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = SecondaryBorder;
            btn.FlatAppearance.MouseOverBackColor = SecondaryHover;
            btn.FlatAppearance.MouseDownBackColor = SecondaryDown;
            btn.BackColor = SecondaryBg;
            btn.ForeColor = SecondaryFg;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }

        // Subtle danger styling — secondary look but red accents on hover. Used for delete-style actions.
        public static void ApplyDanger(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = DangerBg;
            btn.FlatAppearance.MouseOverBackColor = DangerBg;
            btn.FlatAppearance.MouseDownBackColor = DangerHover;
            btn.BackColor = SecondaryBg;
            btn.ForeColor = DangerBg;
            btn.Font = ButtonFont;
            btn.Cursor = Cursors.Hand;
            btn.UseVisualStyleBackColor = false;
        }
    }
}
