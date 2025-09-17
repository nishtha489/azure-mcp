// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using AzureMcp.Core.Commands;
using AzureMcp.Core.Models.Option;
using AzureMcp.LoadTesting.Models.LoadTest;
using AzureMcp.LoadTesting.Options.LoadTest;
using AzureMcp.LoadTesting.Services;
using Microsoft.Extensions.Logging;

namespace AzureMcp.LoadTesting.Commands.LoadTest;

public sealed class TestGetCommand(ILogger<TestGetCommand> logger)
    : BaseLoadTestingCommand<TestGetOptions>
{
    private const string _commandTitle = "Test Get";
    private readonly ILogger<TestGetCommand> _logger = logger;
    private readonly Option<string> _loadTestIdOption = OptionDefinitions.LoadTesting.Test;

    public override string Name => "get";
    public override string Description =>
        $"""
        Retrieve the load test definition (Test ID) from a Load Testing resource.
        Returns only the test configuration and metadata such as duration, ramp-up, virtual users, endpoint, display name, and description.
        Does NOT return any test runs or run-level data.
        """;
    public override string Title => _commandTitle;

    public override ToolMetadata Metadata => new() { Destructive = false, ReadOnly = true };

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.AddOption(_loadTestIdOption);
    }

    protected override TestGetOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.TestId = parseResult.GetValueForOption(_loadTestIdOption);
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult)
    {
        var options = BindOptions(parseResult);
        try
        {
            // Required validation step using the base Validate method
            if (!Validate(parseResult.CommandResult, context.Response).IsValid)
            {
                return context.Response;
            }

            // Get the appropriate service from DI
            var service = context.GetService<ILoadTestingService>();

            // Call service operation(s)
            var results = await service.GetTestAsync(
                options.Subscription!,
                options.TestResourceName!,
                options.TestId!,
                options.ResourceGroup,
                options.Tenant,
                options.RetryPolicy);

            // Set results if any were returned
            context.Response.Results = results != null ?
                ResponseResult.Create(new TestGetCommandResult(results), LoadTestJsonContext.Default.TestGetCommandResult) :
                null;
        }
        catch (Exception ex)
        {
            // Log error with context information
            _logger.LogError(ex, "Error in {Operation}. Options: {Options}", Name, options);
            // Let base class handle standard error processing
            HandleException(context, ex);
        }
        return context.Response;
    }
    internal record TestGetCommandResult(Test Test);
}
