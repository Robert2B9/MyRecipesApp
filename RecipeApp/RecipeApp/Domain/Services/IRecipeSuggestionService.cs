using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecipeApp.Domain.Models;

namespace RecipeApp.Domain.Services;

/// <summary>
/// Turns a free-text request ("vegan pasta", "I have eggs and rice", ...) into recipes.
/// The UI only knows this interface, so Gemini can be swapped for a fake or another provider.
/// </summary>
public interface IRecipeSuggestionService
{
    /// <exception cref="RecipeSuggestionException">
    /// Anything that goes wrong (network, bad key, unusable AI answer), with a message safe to show the user.
    /// </exception>
    Task<IReadOnlyList<Recipe>> SuggestAsync(string request, CancellationToken cancellationToken = default);
}