using System.Threading.Tasks;
using RecipeApp.Domain.Models;

namespace RecipeApp.Domain.Services;

/// <summary>
/// Lets ViewModels move between screens without referencing MAUI's Shell.
/// </summary>
public interface INavigationService
{
    /// <summary>Opens the cooking screen for the given recipe.</summary>
    Task ShowCookingAsync(Recipe recipe);

    /// <summary>Goes back one screen.</summary>
    Task GoBackAsync();
}