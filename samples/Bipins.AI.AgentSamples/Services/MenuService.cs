using Bipins.AI.AgentSamples.Core;

namespace Bipins.AI.AgentSamples.Services;

/// <summary>
/// Interactive menu service for scenario selection.
/// Follows Single Responsibility Principle - only handles menu display and interaction.
/// </summary>
public class MenuService : IMenuService
{
    private readonly IOutputFormatter _outputFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MenuService"/> class.
    /// </summary>
    public MenuService(IOutputFormatter outputFormatter)
    {
        _outputFormatter = outputFormatter ?? throw new ArgumentNullException(nameof(outputFormatter));
    }

    /// <inheritdoc />
    public async Task ShowMenuAsync(IReadOnlyList<IScenario> scenarios, CancellationToken cancellationToken = default)
    {
        var menuItems = new Dictionary<int, (IScenario Scenario, string Marker)>
        {
            { 0, (null!, "❌") }
        };

        // Add scenarios
        foreach (var scenario in scenarios.OrderBy(s => s.Number))
        {
            menuItems[scenario.Number] = (scenario, "📌");
        }

        // Add "Run All" option
        menuItems[scenarios.Count + 1] = (null!, "🚀");

        while (!cancellationToken.IsCancellationRequested)
        {
            _outputFormatter.WriteSeparator();
            Console.WriteLine("Available Scenarios:");
            Console.WriteLine();

            // Display exit option
            Console.WriteLine($"  ❌ 0. Exit");
            Console.WriteLine($"     Exit the application");
            Console.WriteLine();

            // Display scenarios
            foreach (var scenario in scenarios.OrderBy(s => s.Number))
            {
                var marker = "📌";
                Console.WriteLine($"  {marker} {scenario.Number}. {scenario.Name}");
                Console.WriteLine($"     {scenario.Description}");
            }

            // Display run all option
            Console.WriteLine();
            Console.WriteLine($"  🚀 {scenarios.Count + 1}. Run All");
            Console.WriteLine($"     Execute all scenarios in sequence");
            Console.WriteLine();

            Console.Write("Select a scenario (0-{0}): ", scenarios.Count + 1);

            var input = Console.ReadLine();
            
            // Handle null input (non-interactive terminal)
            if (input == null)
            {
                _outputFormatter.WriteError("Console input not available. Exiting...");
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                _outputFormatter.WriteError("Invalid selection. Please try again.");
                _outputFormatter.WriteSeparator();
                continue;
            }

            if (!int.TryParse(input.Trim(), out var choice) || !menuItems.ContainsKey(choice))
            {
                _outputFormatter.WriteError("Invalid selection. Please try again.");
                _outputFormatter.WriteSeparator();
                continue;
            }

            if (choice == 0)
            {
                Console.WriteLine("👋 Goodbye!");
                break;
            }

            if (choice == scenarios.Count + 1)
            {
                // Run all scenarios
                await RunAllScenariosAsync(scenarios, cancellationToken);
            }
            else
            {
                // Run selected scenario
                var selectedScenario = menuItems[choice].Scenario;
                try
                {
                    await selectedScenario.ExecuteAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _outputFormatter.WriteError($"Error in scenario: {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.Write("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private async Task RunAllScenariosAsync(IReadOnlyList<IScenario> scenarios, CancellationToken cancellationToken)
    {
        Console.WriteLine("🚀 Running all scenarios...");
        Console.WriteLine();

        foreach (var scenario in scenarios.OrderBy(s => s.Number))
        {
            try
            {
                await scenario.ExecuteAsync(cancellationToken);
                await Task.Delay(500, cancellationToken); // Brief pause between scenarios
            }
            catch (Exception ex)
            {
                _outputFormatter.WriteError($"Error in scenario {scenario.Number}: {ex.Message}");
            }
        }
    }
}
