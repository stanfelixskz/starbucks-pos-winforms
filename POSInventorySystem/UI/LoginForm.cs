using POSInventorySystem.Services;

namespace POSInventorySystem.UI;

public class LoginForm : Form
{
    private readonly LoginService loginService = new LoginService();

    private readonly UnderlineField usernameBox;
    private readonly UnderlineField passwordBox;
    private readonly Label errorLabel;
    private int attempts;

    public string Role { get; private set; } = "";
    public string FullName { get; private set; } = "";

    private const int BrandWidth = 380;

    public LoginForm()
    {
        Text = "Starbucks POS - Sign in";
        ClientSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ==============================
        // LEFT: BRAND PANEL
        // Everything is centered, and the group as a whole is centered
        // vertically so the space above and below is equal.
        // ==============================

        Panel brand = new Panel
        {
            Dock = DockStyle.Left,
            Width = BrandWidth,
            BackColor = Theme.GreenDark
        };

        PictureBox? logo = BrandLogo.Create(220, 180);

        Label wordmark = Centered(
            "STARBUCKS", 28f, true, Color.White, BrandWidth, 52);

        Label subtitle = Centered(
            "POS & INVENTORY", 12f, false, Theme.Gold, BrandWidth, 28);

        Label tagline = Centered(
            "Serve every cup with care.", 10.5f, false,
            Color.FromArgb(170, 200, 190), BrandWidth, 26);

        int groupHeight =
            (logo != null ? logo.Height + 20 : 0) +
            wordmark.Height + subtitle.Height + 18 + tagline.Height;

        int top = (ClientSize.Height - groupHeight) / 2;

        if (logo != null)
        {
            logo.Location = new Point((BrandWidth - logo.Width) / 2, top);
            brand.Controls.Add(logo);
            top += logo.Height + 20;
        }

        wordmark.Top = top;
        top += wordmark.Height;

        subtitle.Top = top;
        top += subtitle.Height + 18;

        tagline.Top = top;

        brand.Controls.AddRange(new Control[]
        {
            wordmark, subtitle, tagline
        });

        // ==============================
        // RIGHT: SIGN-IN CARD
        // Same left/right padding throughout; equal gaps between groups.
        // ==============================

        const int cardWidth = 400;
        const int cardHeight = 420;
        const int pad = 36;
        int inner = cardWidth - pad * 2;

        Panel card = Theme.CardPanel();
        card.Size = new Size(cardWidth, cardHeight);
        card.Location = new Point(
            BrandWidth + (ClientSize.Width - BrandWidth - cardWidth) / 2,
            (ClientSize.Height - cardHeight - 50) / 2);

        Label title = Theme.Label("Welcome back", 20f, true);
        title.Location = new Point(pad, 34);

        Label hint = Theme.Label(
            "Sign in to start your shift", 10f, false, Theme.Muted);
        hint.Location = new Point(pad + 2, 76);

        Label userLabel = Theme.Label("USERNAME", 8.5f, true, Theme.Muted);
        userLabel.Location = new Point(pad + 2, 130);

        usernameBox = new UnderlineField(inner);
        usernameBox.Location = new Point(pad, 150);

        Label passLabel = Theme.Label("PASSWORD", 8.5f, true, Theme.Muted);
        passLabel.Location = new Point(pad + 2, 212);

        passwordBox = new UnderlineField(inner);
        passwordBox.Location = new Point(pad, 232);
        passwordBox.UseSystemPasswordChar = true;

        // A fixed-height row, so the layout never jumps when an error shows
        errorLabel = Theme.Label("", 9.5f, false, Theme.Danger);
        errorLabel.AutoSize = false;
        errorLabel.Size = new Size(inner, 36);
        errorLabel.Location = new Point(pad, 288);

        RoundedButton signIn = Theme.PrimaryButton("Sign in");
        signIn.Size = new Size(inner, 50);
        signIn.Location = new Point(pad, 338);
        signIn.Click += (_, _) => SignIn();

        card.Controls.AddRange(new Control[]
        {
            title, hint, userLabel, usernameBox,
            passLabel, passwordBox, errorLabel, signIn
        });

        Label footer = Centered(
            "Authorized staff only", 9f, false, Theme.Muted,
            cardWidth, 22);

        footer.Location = new Point(card.Left, card.Bottom + 16);

        Controls.Add(card);
        Controls.Add(footer);
        Controls.Add(brand);

        AcceptButton = signIn;

        Shown += (_, _) => usernameBox.Focus();
    }

    private static Label Centered(
        string text,
        float size,
        bool bold,
        Color color,
        int width,
        int height)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Size = new Size(width, height),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = bold ? Theme.Bold(size) : Theme.Regular(size),
            ForeColor = color,
            BackColor = Color.Transparent
        };
    }

    private void SignIn()
    {
        string username = usernameBox.Text.Trim();
        string password = passwordBox.Text;

        if (username.Length == 0 || password.Length == 0)
        {
            errorLabel.Text = "Enter your username and password.";
            return;
        }

        if (!loginService.Login(
            username,
            password,
            out string role,
            out string fullName))
        {
            attempts++;

            if (attempts >= 3)
            {
                MessageBox.Show(
                    "Too many failed login attempts.\nThe system will close.",
                    "Sign in",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            errorLabel.Text =
                $"Invalid username or password. " +
                $"Attempts remaining: {3 - attempts}";

            passwordBox.Clear();
            passwordBox.Focus();
            return;
        }

        Role = role;
        FullName = fullName;

        DialogResult = DialogResult.OK;
        Close();
    }
}
