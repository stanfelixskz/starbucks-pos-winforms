using POSInventorySystem.Data;
using POSInventorySystem.UI;

namespace POSInventorySystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Open (and on first run create) the database, then load it
        try
        {
            Database.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The database could not be opened." + Environment.NewLine +
                Environment.NewLine +
                ex.Message + Environment.NewLine +
                Environment.NewLine +
                "Make sure SQL Server LocalDB is installed.",
                "Starbucks POS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        // Initialize product recipes
        RecipeData.Initialize();

        // Sign in, work, log out - repeat until the window is closed
        // or sign-in is cancelled.
        while (true)
        {
            using LoginForm login = new LoginForm();

            if (login.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            ILogoutForm main = login.Role == "Admin"
                ? new AdminForm(login.FullName)
                : new CashierForm(login.FullName);

            Application.Run((Form)main);

            if (!main.LoggedOut)
            {
                return;
            }
        }
    }
}

// Implemented by the main windows so Program knows
// whether to return to the sign-in screen.
public interface ILogoutForm
{
    bool LoggedOut { get; }
}
