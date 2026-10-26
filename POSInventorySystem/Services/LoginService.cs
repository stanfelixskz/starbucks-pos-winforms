using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

public class LoginService
{
    public bool UserExists(string username)
    {
        return DataStore.Users.Any(
            user => user.Username == username
        );
    }

    // Creates a new cashier account.
    // Returns false and sets error when the details are not valid.
    public bool Register(
        string username,
        string password,
        string fullName,
        out string error)
    {
        error = "";

        if (username.Length < 3 ||
            username.Any(char.IsWhiteSpace))
        {
            error =
                "Username must be at least 3 characters " +
                "with no spaces.";

            return false;
        }

        if (DataStore.Users.Any(
            user => user.Username.Equals(
                username,
                StringComparison.OrdinalIgnoreCase)))
        {
            error = "That username is already taken.";
            return false;
        }

        if (password.Length < 6)
        {
            error = "Password must be at least 6 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            error = "Full name is required.";
            return false;
        }

        User account = new User(
            username,
            PasswordHasher.Hash(password),
            fullName.Trim(),
            "Cashier"
        );

        try
        {
            Database.AddUser(account);
        }
        catch (Exception ex)
        {
            error = $"Could not save the account: {ex.Message}";
            return false;
        }

        DataStore.Users.Add(account);

        return true;
    }

    public bool Login(
        string username,
        string password,
        out string role,
        out string fullName)
    {
        role = "";
        fullName = "";

        User? user = DataStore.Users.FirstOrDefault(
            u => u.Username == username
        );

        if (user == null)
        {
            return false;
        }

        if (!PasswordHasher.Verify(password, user.Password))
        {
            return false;
        }

        role = user.Role;
        fullName = user.FullName;

        return true;
    }
}