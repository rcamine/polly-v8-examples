# Polly v8 Example Project

This project demonstrates how to use Polly v8 for implementing resilience patterns in a .NET web application. Polly is a resilience and transient fault handling library that helps you build more robust applications.

## What is Polly v8?

Polly v8 is a major update to the popular resilience library for .NET that brings:

- A simplified API through the new `ResiliencePipeline` abstraction
- Better dependency injection integration with .NET
- Enhanced performance and configurability
- Support for both synchronous and asynchronous operations

## What This Project Demonstrates

This sample project shows how to implement common resilience patterns using Polly v8:

1. **Retry Pattern** - Automatically retry operations that fail due to transient issues
2. **Circuit Breaker Pattern** - Prevent cascading failures by stopping operations when consistent failures are detected
3. **Timeout Pattern** - Prevent operations from hanging indefinitely

## Project Structure

- WebApplication1.csproj - The main project file that includes Polly v8 packages
- Program.cs - Configuration of resilience pipelines and endpoints
- SomeHttpService.cs - A simulated HTTP service that we apply resilience to

## How Polly is Configured

In Program.cs, we configure a resilience pipeline with multiple strategies:

```csharp
builder.Services.AddResiliencePipeline<string>("my-police", pipeBuilder =>
{
    pipeBuilder
        .AddRetry(new RetryStrategyOptions { ... })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ... });
});
```

### Retry Strategy

The retry strategy is configured to:
- Make up to 3 retry attempts
- Wait 2 seconds between attempts (constant backoff)
- Apply jitter to prevent retry storms
- Log attempt information

### Timeout Strategy

A simple timeout of 30 seconds is applied to prevent operations from hanging.

### Circuit Breaker Strategy

The circuit breaker is configured to:
- Monitor minimum throughput of 15 requests
- Sample over a 30-second window
- Break the circuit when the failure ratio exceeds 10%
- Stay open for 30 seconds when broken
- Support manual control for testing
- Log state changes (half-open, open, closed)

## API Endpoints

- `GET /api/users/{username}` - Retrieves user data with resilience applied
  - If username is "fail", it simulates a failure to test resilience
  - Uses the configured resilience pipeline

- `GET /api/close` - Manually closes the circuit breaker
- `GET /api/open` - Manually opens the circuit breaker

## Testing with HTTP Requests

The project includes PollyTests.http for testing:

```http
### Fail request
GET http://localhost:5047/api/users/fail

### Get by name
GET http://localhost:5047/api/users/rcamine

### Close circuit
GET http://localhost:5047/api/close

### Open circuit
GET http://localhost:5047/api/open
```

## Key Concepts

### ResiliencePipeline

The new `ResiliencePipeline` abstraction allows you to compose multiple resilience strategies into a single pipeline that you can execute operations through.

### Resilience Strategy Options

Each strategy (retry, timeout, circuit breaker) has its own options class that allows for detailed configuration.

### Manual Circuit Control

For testing purposes, you can use `CircuitBreakerManualControl` to force the circuit breaker into specific states.

## Running the Project

To run the project:

```bash
cd src/WebApplication1
dotnet run
```

Then use the HTTP endpoints to test the resilience patterns.