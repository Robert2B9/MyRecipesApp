using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.Infrastructure.Navigation;

/// <summary>
/// Navigation implemented with MAUI Shell. This is the only place that
/// calls Shell, so ViewModels stay free of MAUI navigation types.
/// </summary>
public class ShellNavigationService : INavigationService
{
    public Task ShowCookingAsync(Recipe recipe)
    {
        // "cooking" is the route registered in AppShell.
        // The dictionary key "Recipe" must match the [QueryProperty] on CookingViewModel.
        return Shell.Current.GoToAsync("cooking", new Dictionary<string, object>
        {
            ["Recipe"] = recipe
        });
    }

    // ".." means "go back one page".
    public Task GoBackAsync() => Shell.Current.GoToAsync("..");
}