using CommunityToolkit.Maui.Markup;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using RecipeApp.ViewModels;

namespace RecipeApp;

public class MainPage : ContentPage
{
   public MainPage(MainViewModel viewModel)
   {
      BindingContext = viewModel;
      Content = new VerticalStackLayout()
      {
         Spacing = 8,
         Padding = new Thickness(12),
         Children =
         {
            new Label().Bind(Label.TextProperty, static (MainViewModel vm) => vm.Greeting),
            new Button().BindCommand(static (MainViewModel vm) => vm.IncrementCommand)
               .Text("Increment")
         }
      };
   }
}