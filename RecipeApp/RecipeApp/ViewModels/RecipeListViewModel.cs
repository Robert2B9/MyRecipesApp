using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.ViewModels;

/// <summary>
/// Start screen: the available recipes, plus a box to ask the AI for more.
/// </summary>
public partial class RecipeListViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IRecipeSuggestionService _suggestionService;

    // ObservableCollection tells the list when items are added, so new AI recipes appear instantly.
    public ObservableCollection<Recipe> Recipes { get; }

    // What the user typed in the AI request box.
    [ObservableProperty] private string _prompt = "";

    // True while waiting for the AI (drives the spinner).
    [ObservableProperty] private bool _isBusy;

    // Shown under the button when something goes wrong. Empty means no error.
    [ObservableProperty] private string _errorMessage = "";

    public RecipeListViewModel(IRecipeRepository repository, INavigationService navigation, IRecipeSuggestionService suggestionService)
    {
        _navigation = navigation;
        _suggestionService = suggestionService;
        Recipes = new ObservableCollection<Recipe>(repository.GetAll());
    }

    // Opens a recipe for cooking. Called when a row is tapped.
    [RelayCommand]
    private async Task OpenRecipe(Recipe recipe)
    {
        await _navigation.ShowCookingAsync(recipe);
    }

    // Asks the AI for recipes and puts them at the top of the list.
    [RelayCommand]
    private async Task Suggest()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(Prompt)) return;

        IsBusy = true;
        ErrorMessage = "";

        try
        {
            IReadOnlyList<Recipe> suggestions = await _suggestionService.SuggestAsync(Prompt);

            // Insert in reverse so the AI's first suggestion ends up on top.
            for (int i = suggestions.Count - 1; i >= 0; i--)
                Recipes.Insert(0, suggestions[i]);
        }
        catch (RecipeSuggestionException ex)
        {
            // The service already wrote a readable message.
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}