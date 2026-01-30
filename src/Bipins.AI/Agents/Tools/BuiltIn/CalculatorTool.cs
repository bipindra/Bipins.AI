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
        // Simple expression evaluator - in production, use a proper math parser
        expression = expression.Trim().ToLowerInvariant();

        // Replace common constants
        expression = expression.Replace("pi", Math.PI.ToString());
        expression = expression.Replace("e", Math.E.ToString());

        // Handle functions
        if (expression.StartsWith("sqrt("))
        {
            var value = ExtractFunctionArgument(expression, "sqrt");
            return Math.Sqrt(value);
        }
        if (expression.StartsWith("sin("))
        {
            var value = ExtractFunctionArgument(expression, "sin");
            return Math.Sin(value);
        }
        if (expression.StartsWith("cos("))
        {
            var value = ExtractFunctionArgument(expression, "cos");
            return Math.Cos(value);
        }
        if (expression.StartsWith("tan("))
        {
            var value = ExtractFunctionArgument(expression, "tan");
            return Math.Tan(value);
        }
        if (expression.StartsWith("log("))
        {
            var value = ExtractFunctionArgument(expression, "log");
            return Math.Log10(value);
        }
        if (expression.StartsWith("ln("))
        {
            var value = ExtractFunctionArgument(expression, "ln");
            return Math.Log(value);
        }

        // Handle basic arithmetic (simplified - use proper parser in production)
        // This is a very basic implementation
        if (double.TryParse(expression, out var number))
        {
            return number;
        }

        // Try to evaluate simple expressions
        var parts = expression.Split(new[] { '+', '-', '*', '/', '^' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            if (double.TryParse(parts[0].Trim(), out var left) && double.TryParse(parts[1].Trim(), out var right))
            {
                if (expression.Contains('+')) return left + right;
                if (expression.Contains('-')) return left - right;
                if (expression.Contains('*')) return left * right;
                if (expression.Contains('/')) return left / right;
                if (expression.Contains('^')) return Math.Pow(left, right);
            }
        }

        throw new ArgumentException($"Unable to evaluate expression: {expression}");
    }

    private double ExtractFunctionArgument(string expression, string functionName)
    {
        var start = expression.IndexOf('(') + 1;
        var end = expression.LastIndexOf(')');
        if (start <= 0 || end <= start)
        {
            throw new ArgumentException($"Invalid function syntax: {expression}");
        }

        var arg = expression.Substring(start, end - start).Trim();
        return EvaluateExpression(arg);
    }
}
