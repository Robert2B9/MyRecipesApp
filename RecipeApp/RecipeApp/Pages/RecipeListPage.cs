using System.Linq;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using RecipeApp.Domain.Models;
using RecipeApp.ViewModels;
using RecipeApp.UI;
namespace RecipeApp.Pages;

/// <summary>
/// Start screen: an AI request box on top, the recipe list below.
/// Tapping a recipe opens the cooking page.
/// </summary>
public class RecipeListPage : ContentPage
{
    public RecipeListPage(RecipeListViewModel viewModel)
    {
        BindingContext = viewModel;
        Title = "Recipes";
        BackgroundColor = AppColours.Background;
        // Text box for the AI request. Two-way: typing updates vm.Prompt.
        Entry promptEntry = new Entry
        {
            Placeholder = "Ingredients, diet, cuisine...",
            Margin = new Thickness(12, 8, 12, 0),
            TextColor = Colors.Black,
            PlaceholderColor = Colors.DimGrey
        }
        .Bind(Entry.TextProperty,
              static (RecipeListViewModel vm) => vm.Prompt,
              static (RecipeListViewModel vm, string text) => vm.Prompt = text);

        Button suggestButton = new Button
        {
            Text = "Suggest recipes with AI",
            Margin = new Thickness(12, 8),
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(24, 10),
            BackgroundColor = AppColours.Accent
        }
        .BindCommand(static (RecipeListViewModel vm) => vm.SuggestCommand);

        // Spinner while waiting, red error text if the request failed.
        VerticalStackLayout status = new VerticalStackLayout
        {
            Margin = new Thickness(12, 0),
            Children =
            {
                new ActivityIndicator()
                    .Bind(ActivityIndicator.IsRunningProperty, static (RecipeListViewModel vm) => vm.IsBusy),
                new Label { TextColor = Colors.Red }
                    .Bind(Label.TextProperty, static (RecipeListViewModel vm) => vm.ErrorMessage)
            }
        };

        CollectionView list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,

            // How ONE recipe row looks. Inside this template the binding
            // context is a single Recipe, not the ViewModel.
            ItemTemplate = new DataTemplate(() =>
                new Label { FontSize = 22, Padding = new Thickness(16, 14) }
                    .Bind(Label.TextProperty, static (Recipe recipe) => recipe.Title))
        }
        .Bind(ItemsView.ItemsSourceProperty, static (RecipeListViewModel vm) => vm.Recipes);

        // Forward taps to the ViewModel's command. No logic lives here.
        list.SelectionChanged += (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is Recipe recipe)
            {
                viewModel.OpenRecipeCommand.Execute(recipe);
                list.SelectedItem = null; // clear the highlight so it can be tapped again later
            }
        };

        // Three rows that fit their content, and a last row that takes the remaining space.
        Grid grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        grid.Add(promptEntry, 0, 0);   // column 0, row 0
        grid.Add(suggestButton, 0, 1);
        grid.Add(status, 0, 2);
        grid.Add(list, 0, 3);

        Content = grid;
    }
}