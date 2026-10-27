using System.Text.RegularExpressions;

namespace POSInventorySystem.Data;

// The message shown when the database can not be opened at startup: what
// went wrong and the exact commands that usually fix it.
public static class StartupHelp
{
    // "(localdb)\MSSQLLocalDB" -> "MSSQLLocalDB"; null for any other server
    public static string? LocalDbInstance(string server)
    {
        Match match = Regex.Match(
            server.Trim(),
            @"^\(localdb\)\\(?<name>[^\\]+)$",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["name"].Value : null;
    }

    public static string Describe(string server, Exception error)
    {
        string? instance = LocalDbInstance(server);
        string nl = Environment.NewLine;

        string message =
            "The database could not be opened." + nl + nl +
            error.Message + nl + nl +
            $"Server: {server}" + nl + nl;

        if (instance != null)
        {
            message +=
                "To fix it, open Command Prompt and run:" + nl + nl +
                "    sqllocaldb info" + nl +
                $"    sqllocaldb create \"{instance}\"      (only if {instance} is not listed)" + nl +
                $"    sqllocaldb start \"{instance}\"" + nl + nl +
                "If 'sqllocaldb' is not found, install SQL Server Express " +
                "LocalDB from Microsoft." + nl + nl +
                "To use a different instance, run this, then start the " +
                "app again:" + nl + nl +
                $"    setx {Database.ServerVariable} \"(localdb)\\OtherName\"";
        }
        else
        {
            message +=
                "Check that this server is running and that your Windows " +
                "account can sign in to it." + nl + nl +
                "To go back to LocalDB, run this, then start the app again:" +
                nl + nl +
                $"    setx {Database.ServerVariable} \"{Database.DefaultServer}\"";
        }

        return message;
    }
}
