using System.Text.Json;
using Bipins.AI.Agents.Tools;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.AgentSamples.Tools;

/// <summary>
/// Custom tool implementation that simulates weather API calls.
/// This demonstrates how to create custom tools for agents.
/// </summary>
public class WeatherTool : IToolExecutor
{
    private readonly ILogger<WeatherTool>? _logger;
    private readonly Dictionary<string, WeatherData> _weatherCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherTool"/> class.
    /// </summary>
    public WeatherTool(ILogger<WeatherTool>? logger = null)
    {
        _logger = logger;
        // Initialize with some sample data
        InitializeSampleData();
    }

    /// <inheritdoc />
    public string Name => "get_weather";

    /// <inheritdoc />
    public string Description => "Gets the current weather for a given location. Returns temperature in Fahrenheit, conditions, and humidity.";

    /// <inheritdoc />
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            location = new
            {
                type = "string",
                description = "The city and state, e.g. San Francisco, CA or New York, NY"
            },
            unit = new
            {
                type = "string",
                @enum = new[] { "fahrenheit", "celsius" },
                description = "Temperature unit (default: fahrenheit)"
            }
        },
        required = new[] { "location" }
    });

    /// <inheritdoc />
    public Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            if (toolCall.Arguments.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult(new ToolExecutionResult(
                    Success: false,
                    Error: "Invalid arguments format"));
            }

            var location = toolCall.Arguments.TryGetProperty("location", out var locationProp)
                ? locationProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(location))
            {
                return Task.FromResult(new ToolExecutionResult(
                    Success: false,
                    Error: "Location is required"));
            }

            var unit = toolCall.Arguments.TryGetProperty("unit", out var unitProp)
                ? unitProp.GetString()?.ToLowerInvariant() ?? "fahrenheit"
                : "fahrenheit";

            // Simulate weather API call with cached data
            var weather = GetWeather(location, unit);
            _logger?.LogDebug("WeatherTool: Retrieved weather for {Location} - {Temperature}°{Unit}", 
                location, weather.Temperature, unit == "celsius" ? "C" : "F");

            return Task.FromResult(new ToolExecutionResult(
                Success: true,
                Result: new
                {
                    location = weather.Location,
                    temperature = weather.Temperature,
                    unit = unit,
                    conditions = weather.Conditions,
                    humidity = weather.Humidity,
                    windSpeed = weather.WindSpeed
                }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing weather tool");
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                Error: ex.Message));
        }
    }

    private WeatherData GetWeather(string location, string unit)
    {
        var normalizedLocation = location.ToLowerInvariant();
        
        if (_weatherCache.TryGetValue(normalizedLocation, out var cached))
        {
            var result = cached;
            if (unit == "celsius" && result.Unit == "fahrenheit")
            {
                // Convert to Celsius
                result = new WeatherData(
                    result.Location,
                    (result.Temperature - 32) * 5 / 9,
                    "celsius",
                    result.Conditions,
                    result.Humidity,
                    result.WindSpeed);
            }
            return result;
        }

        // Generate random weather for unknown locations
        var random = new Random(location.GetHashCode());
        var tempF = random.Next(50, 85);
        var conditions = new[] { "Sunny", "Partly Cloudy", "Cloudy", "Rainy", "Clear" }[random.Next(5)];
        var humidity = random.Next(30, 80);
        var windSpeed = random.Next(5, 20);

        var weather = new WeatherData(location, tempF, "fahrenheit", conditions, humidity, windSpeed);
        _weatherCache[normalizedLocation] = weather;

        if (unit == "celsius")
        {
            return new WeatherData(
                weather.Location,
                (weather.Temperature - 32) * 5 / 9,
                "celsius",
                weather.Conditions,
                weather.Humidity,
                weather.WindSpeed);
        }

        return weather;
    }

    private void InitializeSampleData()
    {
        _weatherCache["san francisco, ca"] = new WeatherData("San Francisco, CA", 65, "fahrenheit", "Partly Cloudy", 70, 12);
        _weatherCache["new york, ny"] = new WeatherData("New York, NY", 72, "fahrenheit", "Sunny", 55, 8);
        _weatherCache["los angeles, ca"] = new WeatherData("Los Angeles, CA", 75, "fahrenheit", "Clear", 45, 10);
        _weatherCache["chicago, il"] = new WeatherData("Chicago, IL", 68, "fahrenheit", "Cloudy", 60, 15);
        _weatherCache["seattle, wa"] = new WeatherData("Seattle, WA", 58, "fahrenheit", "Rainy", 80, 10);
    }

    private record WeatherData(
        string Location,
        double Temperature,
        string Unit,
        string Conditions,
        int Humidity,
        int WindSpeed);
}
