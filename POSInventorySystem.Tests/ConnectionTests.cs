using POSInventorySystem.Data;

namespace POSInventorySystem.Tests;

// The server setting, the connection timeouts and the startup help text.
// These do not need a database.
public class ConnectionTests : IDisposable
{
    private readonly string? savedVariable =
        Environment.GetEnvironmentVariable(Database.ServerVariable);

    private readonly string savedServer = Database.ServerName;

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(Database.ServerVariable, savedVariable);
        Database.ServerName = savedServer;
    }

    [Fact]
    public void The_server_defaults_to_the_standard_localdb_instance()
    {
        Environment.SetEnvironmentVariable(Database.ServerVariable, null);

        Assert.Equal(@"(localdb)\MSSQLLocalDB", Database.ServerFromEnvironment());
    }

    [Fact]
    public void An_environment_variable_picks_another_server()
    {
        Environment.SetEnvironmentVariable(Database.ServerVariable, @"  (localdb)\School  ");

        Assert.Equal(@"(localdb)\School", Database.ServerFromEnvironment());
    }

    [Fact]
    public void A_blank_variable_is_ignored()
    {
        Environment.SetEnvironmentVariable(Database.ServerVariable, "   ");

        Assert.Equal(Database.DefaultServer, Database.ServerFromEnvironment());
    }

    [Fact]
    public void The_connection_uses_the_chosen_server()
    {
        Database.ServerName = @"(localdb)\School";

        Assert.Contains(@"Server=(localdb)\School;", Database.ConnectionString("X", 10));
    }

    [Fact]
    public void Normal_saves_give_up_quickly_but_startup_is_patient()
    {
        Assert.True(Database.SaveTimeoutSeconds <= 15);
        Assert.True(Database.StartupTimeoutSeconds >= 30);
        Assert.True(Database.SaveTimeoutSeconds < Database.StartupTimeoutSeconds);

        Assert.EndsWith("Connect Timeout=10", Database.ConnectionString("X", Database.SaveTimeoutSeconds));
        Assert.EndsWith("Connect Timeout=60", Database.ConnectionString("X", Database.StartupTimeoutSeconds));
    }

    [Theory]
    [InlineData(@"(localdb)\MSSQLLocalDB", "MSSQLLocalDB")]
    [InlineData(@"(LocalDB)\School", "School")]
    [InlineData(@"  (localdb)\School  ", "School")]
    [InlineData(@"SERVER01\SQLEXPRESS", null)]
    [InlineData("localhost", null)]
    public void The_localdb_instance_name_is_read_from_the_server(string server, string? expected)
    {
        Assert.Equal(expected, StartupHelp.LocalDbInstance(server));
    }

    [Fact]
    public void The_startup_message_gives_the_exact_commands_for_localdb()
    {
        string message = StartupHelp.Describe(Database.DefaultServer, new Exception("boom"));

        Assert.Contains("boom", message);
        Assert.Contains("sqllocaldb info", message);
        Assert.Contains("sqllocaldb create \"MSSQLLocalDB\"", message);
        Assert.Contains("sqllocaldb start \"MSSQLLocalDB\"", message);
        Assert.Contains("setx STARBUCKSPOS_SERVER", message);
    }

    [Fact]
    public void The_startup_message_names_the_instance_that_was_asked_for()
    {
        string message = StartupHelp.Describe(@"(localdb)\School", new Exception("boom"));

        Assert.Contains("sqllocaldb start \"School\"", message);
        Assert.DoesNotContain("sqllocaldb start \"MSSQLLocalDB\"", message);
    }

    [Fact]
    public void The_startup_message_for_another_server_explains_how_to_go_back()
    {
        string message = StartupHelp.Describe(@"SERVER01\SQLEXPRESS", new Exception("boom"));

        Assert.DoesNotContain("sqllocaldb start", message);
        Assert.Contains(@"setx STARBUCKSPOS_SERVER ""(localdb)\MSSQLLocalDB""", message);
    }
}
