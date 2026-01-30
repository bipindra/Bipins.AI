using System.Text.Json;
using Bipins.AI.Core.Models;
using Microsoft.Extensions.Logging;

namespace Bipins.AI.Agents.Tools.BuiltIn;

/// <summary>
/// Calculator tool for performing mathematical operations.
/// </summary>
public class CalculatorTool : IToolExecutor
{
    private readonly ILogger<CalculatorTool>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatorTool"/> class.
    /// </summary>
    public CalculatorTool(ILogger<CalculatorTool>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "calculator";

    /// <inheritdoc />
    public string Description => "Performs mathematical calculations. Supports basic arithmetic operations (+, -, *, /, ^) and common functions (sqrt, sin, cos, tan, log, ln).";

    /// <inheritdoc />
    public JsonElement ParametersSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            expression = new
            {
                type = "string",
                description = "Mathematical expression to evaluate (e.g., '2 + 2', 'sqrt(16)', 'sin(pi/2)')"
            }
        },
        required = new[] { "expression" }
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

            var expression = toolCall.Arguments.TryGetProperty("expression", out var exprProp)
                ? exprProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return Task.FromResult(new ToolExecutionResult(
                    Success: false,
                    Error: "Expression is required"));
            }

            var result = EvaluateExpression(expression);
            _logger?.LogDebug("Calculator evaluated '{Expression}' = {Result}", expression, result);

            return Task.FromResult(new ToolExecutionResult(
                Success: true,
                Result: new { expression, result }));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error evaluating expression");
            return Task.FromResult(new ToolExecutionResult(
                Success: false,
                Error: ex.Message));
        }
    }

    private double EvaluateExpression(string expression)
    {
        expression = expression.Trim();

        // Replace common constants (case-insensitive, word boundaries to avoid replacing in numbers)
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"\bpi\b", Math.PI.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"\be\b", Math.E.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Handle functions recursively (innermost first)
        while (System.Text.RegularExpressions.Regex.IsMatch(expression, @"(sqrt|sin|cos|tan|log|ln)\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            expression = HandleFunctions(expression);
        }

        // Handle power operator (^) - convert to Math.Pow calls
        while (System.Text.RegularExpressions.Regex.IsMatch(expression, @"\d+(?:\.\d+)?\s*\^\s*\d+(?:\.\d+)?"))
        {
            expression = System.Text.RegularExpressions.Regex.Replace(expression, 
                @"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)", 
                match => Math.Pow(double.Parse(match.Groups[1].Value), double.Parse(match.Groups[2].Value)).ToString());
        }

        // Use DataTable.Compute for safe expression evaluation
        // This handles operator precedence, parentheses, and multiple operations correctly
        try
        {
            var dataTable = new System.Data.DataTable();
            var result = dataTable.Compute(expression, null);
            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Unable to evaluate expression: {expression}. Error: {ex.Message}");
        }
    }

    private string HandleFunctions(string expression)
    {
        // Handle functions - find innermost function calls first
        // This regex matches function calls and captures the function name and argument
        var match = System.Text.RegularExpressions.Regex.Match(expression, 
            @"(sqrt|sin|cos|tan|log|ln)\s*\(\s*([^()]+(?:\([^()]*\)[^()]*)*)\s*\)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return expression;
        }

        var functionName = match.Groups[1].Value.ToLowerInvariant();
        var argument = match.Groups[2].Value;
        double result;

        // Evaluate the argument first (it might contain nested expressions)
        var argValue = EvaluateExpression(argument);

        // Apply the function
        result = functionName switch
        {
            "sqrt" => Math.Sqrt(argValue),
            "sin" => Math.Sin(argValue),
            "cos" => Math.Cos(argValue),
            "tan" => Math.Tan(argValue),
            "log" => Math.Log10(argValue),
            "ln" => Math.Log(argValue),
            _ => throw new ArgumentException($"Unknown function: {functionName}")
        };

        // Replace the function call with its result
        return expression.Replace(match.Value, result.ToString());
    }

}
