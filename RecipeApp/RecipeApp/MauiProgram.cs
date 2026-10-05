//using Android.OS;
using System;
using System.Net.Http;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using RecipeApp.Domain.Services;
using RecipeApp.Infrastructure.Navigation;
using RecipeApp.Infrastructure.Recipes;
using RecipeApp.Infrastructure.Suggestions;
using RecipeApp.Infrastructure.Timers;
using RecipeApp.Pages;
using RecipeApp.ViewModels;

namespace RecipeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMarkup()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        RegisterServices(builder);
        return builder.Build();
    }

    private static void RegisterServices(this MauiAppBuilder builder)
    {
// Services
        builder.Services.AddSingleton<ITimerService, CountdownTimerService>();
        builder.Services.AddSingleton<IRecipeRepository, InMemoryRecipeRepository>();
        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();

// ViewModels
        builder.Services.AddTransient<RecipeListViewModel>();
        builder.Services.AddTransient<CookingViewModel>();

// Pages
        builder.Services.AddTransient<RecipeListPage>();
        builder.Services.AddTransient<CookingPage>();
        
        builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(60) });
        builder.Services.AddSingleton<IRecipeSuggestionService, GeminiRecipeSuggestionService>();
        // Offline demo fallback: use this line instead of the Gemini one above.
        // builder.Services.AddSingleton<IRecipeSuggestionService, FakeRecipeSuggestionService>();
    }
}