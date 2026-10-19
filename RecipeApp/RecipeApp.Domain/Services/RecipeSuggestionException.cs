namespace RecipeApp.Domain.Services;

/// <summary>
/// The one exception ViewModels need to catch from a suggestion service.
/// Implementations wrap their own failures (HTTP, JSON, ...) in this type.
/// </summary>
public class RecipeSuggestionException : Exception
{
    public RecipeSuggestionException(string message) : base(message)
    {
    }

    public RecipeSuggestionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}