using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Retry;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

var manualControl = new CircuitBreakerManualControl();

builder.Services.AddScoped<SomeHttpService>();
// Inject typed http client here
// builder.Services.AddHttpClient<SomeHttpService>(client => client.BaseAddress = new Uri("https://api.github.com"));

// Microsoft http resilience defaults
// https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli#standard-resilience-handler-defaults

// Example configuration for retry, timeouts and circuit breaker
builder.Services.AddResiliencePipeline<string>("my-police", pipeBuilder =>
{
    pipeBuilder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Constant,
            UseJitter = true,
            OnRetry = retryArgs =>
            {
                Console.WriteLine(
                    $"Current attempts: {retryArgs.AttemptNumber}, {retryArgs.Outcome.Exception!.Message}");
                return ValueTask.CompletedTask;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            MinimumThroughput = 15,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.1,
            ManualControl = manualControl,
            OnHalfOpened = _ =>
            {
                Console.WriteLine("Circuit break half opened");
                return ValueTask.CompletedTask;
            },
            OnOpened = _ =>
            {
                Console.WriteLine("Circuit break opened");
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                Console.WriteLine("Circuit break closed");
                return ValueTask.CompletedTask;
            }
        });
});

var app = builder.Build();

// If username == fail, fails the request
app.MapGet("/api/users/{username}", async (string username,
        SomeHttpService someHttpService,
        ResiliencePipelineProvider<string> pipelineProvider,
        CancellationToken ct)
    =>
{
    try
    {
        // get the pipeline by the key registered in .AddResiliencePipeline
        var pipeline = pipelineProvider.GetPipeline("my-police");

        // use the pipeline to apply the policies
        var user = await pipeline.ExecuteAsync(async token => await someHttpService.GetUserAsync(username, token), ct);
        return Results.Ok(user);
    }
    catch (BrokenCircuitException)
    {
        return Results.InternalServerError("Circuit is opened, not retrying.");
    }
    catch (Exception)
    {
        return Results.InternalServerError("Request exceeded the retry limit.");
    }
});

// Manually change the circuit breaker state
app.MapGet("/api/close", async (CancellationToken ct)
    =>
{
    await manualControl.CloseAsync(ct);
    return Results.Ok();
});
app.MapGet("/api/open", async (CancellationToken ct)
    =>
{
    await manualControl.IsolateAsync(ct);
    return Results.Ok();
});

app.Run();
