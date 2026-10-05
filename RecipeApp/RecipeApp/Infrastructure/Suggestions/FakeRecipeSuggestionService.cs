using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.Infrastructure.Suggestions;

/// <summary>
/// Demo fallback: returns one canned recipe after a short delay, no network needed.
/// Swap it in MauiProgram if the wifi or the API misbehaves during the demo.
/// </summary>
public class FakeRecipeSuggestionService : IRecipeSuggestionService
{
    public async Task<IReadOnlyList<Recipe>> SuggestAsync(
        string request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1000, cancellationToken); // pretend to think

        return new List<Recipe>
        {
            new Recipe(
                $"Demo suggestion: {request}",
                new[] { "Ingredient A", "Ingredient B" },
                new[]
                {
                    new RecipeStep("Prepare the ingredients.", null),
                    new RecipeStep("Cook for ten seconds (short demo timer).", 10),
                    new RecipeStep("Serve and enjoy.", null)
                })
        };
    }
}