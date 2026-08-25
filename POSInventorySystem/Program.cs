    using POSInventorySystem;
    using POSInventorySystem.Services;
    using POSInventorySystem.Data;

    // Initialize product recipes
    RecipeData.Initialize();

    LoginService loginService = new LoginService();

    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("      STARBUCKS POS SYSTEM");
        Console.WriteLine("=================================");
        Console.WriteLine("             LOGIN");
        Console.WriteLine("=================================");

        int attempts = 0;
        bool loggedIn = false;

        while (attempts < 3 && !loggedIn)
        {
            Console.Write("Username: ");
            string? username = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                attempts++;
                Console.WriteLine("Username is required.");
                Console.WriteLine(
                    $"Attempts remaining: {3 - attempts}"
                );
                Console.WriteLine();
                continue;
            }

            if (!loginService.UserExists(username))
            {
                attempts++;
                Console.WriteLine("Account not found.");
                Console.WriteLine(
                    $"Attempts remaining: {3 - attempts}"
                );
                Console.WriteLine();
                continue;
            }

            Console.Write("Password: ");

            string password = "";

            while (true)
            {
                ConsoleKeyInfo key =
                    Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password[..^1];
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                attempts++;
                Console.WriteLine("Password is required.");
                Console.WriteLine(
                    $"Attempts remaining: {3 - attempts}"
                );
                Console.WriteLine();
                continue;
            }

            if (!loginService.Login(
                username,
                password,
                out string role,
                out string fullName))
            {
                attempts++;
                Console.WriteLine("Invalid password.");
                Console.WriteLine(
                    $"Attempts remaining: {3 - attempts}"
                );
                Console.WriteLine();
                continue;
            }

            loggedIn = true;

            Console.WriteLine();
            Console.WriteLine("Login successful!");
            Console.WriteLine($"Welcome, {fullName}!");
            Console.WriteLine($"Role: {role}");
            Console.WriteLine();

            if (role == "Admin")
            {
                AdminMenu adminMenu = new AdminMenu();
                adminMenu.Show();
            }
            else if (role == "Cashier")
            {
                CashierMenu cashierMenu = new CashierMenu();
                cashierMenu.Show();
            }

            Console.WriteLine();
            Console.WriteLine("You have been logged out.");
        }

        if (!loggedIn)
        {
            Console.WriteLine();
            Console.WriteLine("Too many failed login attempts.");
            Console.WriteLine("System exiting...");
            break;
        }
    }