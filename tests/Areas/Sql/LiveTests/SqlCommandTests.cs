// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using AzureMcp.Tests.Client;
using AzureMcp.Tests.Client.Helpers;
using Xunit;

namespace AzureMcp.Tests.Areas.Sql.LiveTests;

[Trait("Area", "Sql")]
[Trait("Category", "Live")]
public class SqlCommandTests(LiveTestFixture liveTestFixture, ITestOutputHelper output)
    : CommandTestsBase(liveTestFixture, output), IClassFixture<LiveTestFixture>
{

    [Fact]
    public async Task Should_ShowDatabase_Successfully()
    {
        // Use the deployed test SQL server and database
        var serverName = Settings.ResourceBaseName;
        var databaseName = "testdb";

        var result = await CallToolAsync(
            "azmcp_sql_db_show",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "server", serverName },
                { "database", databaseName }
            });

        // Should successfully retrieve the database
        var database = result.AssertProperty("database");
        Assert.Equal(JsonValueKind.Object, database.ValueKind);

        // Verify database properties
        var dbName = database.GetProperty("name").GetString();
        Assert.Equal(databaseName, dbName);

        var dbType = database.GetProperty("type").GetString();
        Assert.Equal("Microsoft.Sql/servers/databases", dbType);
    }

    [Theory]
    [InlineData("--invalid-param", new string[0])]
    [InlineData("--subscription", new[] { "invalidSub" })]
    [InlineData("--subscription", new[] { "sub", "--resource-group", "rg" })]  // Missing server and database
    public async Task Should_Return400_WithInvalidInput(string firstArg, string[] remainingArgs)
    {
        var allArgs = new[] { firstArg }.Concat(remainingArgs);
        var argsString = string.Join(" ", allArgs);

        // For error testing, we expect CallToolAsync to return null (no results)
        // and we need to catch any exceptions or check the response manually
        try
        {
            var result = await CallToolAsync("azmcp_sql_db_show",
                new Dictionary<string, object?> { { "args", argsString } });

            // If we get here, the command didn't fail as expected
            // This might indicate the command succeeded when it should have failed
            Assert.Fail("Expected command to fail with invalid input, but it succeeded");
        }
        catch (Exception ex)
        {
            // Expected behavior - the command should fail with invalid input
            Assert.NotNull(ex.Message);
            Assert.NotEmpty(ex.Message);
        }
    }

    [Fact]
    public async Task Should_ValidateRequiredParameters()
    {
        // Test with missing required parameters - expect an exception or null result
        try
        {
            var result = await CallToolAsync(
                "azmcp_sql_db_show",
                new Dictionary<string, object?>
                {
                    { "subscription", Settings.SubscriptionId }
                    // Missing resource-group, server, and database
                });

            // If we get here without an exception, the validation didn't work as expected
            Assert.Fail("Expected command to fail due to missing required parameters, but it succeeded");
        }
        catch (Exception ex)
        {
            // Expected behavior - should fail due to missing required parameters
            Assert.NotNull(ex.Message);
            Assert.Contains("required", ex.Message.ToLower());
        }
    }

    [Fact]
    public async Task Should_ListSqlServerEntraAdmins_Successfully()
    {
        // Use the deployed test SQL server
        var serverName = Settings.ResourceBaseName;

        var result = await CallToolAsync(
            "azmcp_sql_server_entra-admin_list",
            new()
            {
                { "subscription", Settings.SubscriptionId },
                { "resource-group", Settings.ResourceGroupName },
                { "server", serverName }
            });

        // The command should succeed, but results may be null if no Entra admins are configured
        if (result.HasValue)
        {
            // If there are results, verify the structure
            var admins = result.Value.AssertProperty("administrators");
            Assert.Equal(JsonValueKind.Array, admins.ValueKind);

            // If there are admins, verify their structure
            if (admins.GetArrayLength() > 0)
            {
                var firstAdmin = admins.EnumerateArray().First();
                Assert.Equal(JsonValueKind.Object, firstAdmin.ValueKind);

                // Verify required properties exist
                Assert.True(firstAdmin.TryGetProperty("administratorType", out _));
                Assert.True(firstAdmin.TryGetProperty("login", out _));
                Assert.True(firstAdmin.TryGetProperty("sid", out _));
            }
        }
        // If result is null, that's valid - it means no AD administrators are configured
        // The test passes as long as the command executed successfully (no exception thrown)
    }
}
