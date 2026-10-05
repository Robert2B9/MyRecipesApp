using Microsoft.Maui.Controls;
using RecipeApp.Pages;
using RecipeApp.UI;
namespace RecipeApp;

public class AppShell : Shell
{
    public AppShell()
    {
        ShellContent shellContent = new ShellContent();
        shellContent.Title = "Recipe App";
        shellContent.Route = "main";
        shellContent.ContentTemplate = new DataTemplate(typeof(RecipeListPage));
        //shellContent.ContentTemplate = new DataTemplate(typeof(MainPage)); 
        Items.Add(shellContent);
        FlyoutBehavior = FlyoutBehavior.Disabled;
        Shell.SetBackgroundColor(this, AppColours.Background);
   
        
        Routing.RegisterRoute("cooking", typeof(CookingPage)); 
    }
}