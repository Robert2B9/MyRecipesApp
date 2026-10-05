using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RecipeApp.Domain.Cooking;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.ViewModels;

/// <summary>
/// Drives the cooking screen: shows one step at a time and runs that step's timer.
///
/// Design choices to keep it simple:
///  - Moving to another step stops and resets the timer (one timer at a time).
///  - [QueryProperty] is the one MAUI attribute in this file. It is how Shell
///    hands the selected recipe to this ViewModel.
/// </summary>
[QueryProperty(nameof(Recipe), "Recipe")]
public partial class CookingViewModel : ObservableObject
{
    private readonly ITimerService _timerService;
    private readonly INavigationService _navigation;

    // Tracks "which step am I on". Created when the recipe arrives.
    private CookingSession? _session;

    // True once the timer has been started for the current step
    // (lets us tell "paused" apart from "not started yet").
    private bool _timerStartedForStep;

    // Each [ObservableProperty] field becomes a public property
    // (_title -> Title) that the page can bind to.
    [ObservableProperty] private Recipe? _recipe;
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _ingredientsText = "";
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private string _instruction = "";
    [ObservableProperty] private bool _hasTimer;
    [ObservableProperty] private string _timerText = "";
    [ObservableProperty] private string _timerButtonText = "Start timer";
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private string _nextButtonText = "Next";

    public CookingViewModel(ITimerService timerService, INavigationService navigation)
    {
        _timerService = timerService;
        _navigation = navigation;

        // Listen to the timer. Removed again in Detach().
        _timerService.Tick += OnTimerTick;
        _timerService.Finished += OnTimerFinished;
    }

    // Called automatically by the generated code whenever Recipe is set,
    // which is when Shell passes the recipe in after navigation.
    partial void OnRecipeChanged(Recipe? value)
    {
        if (value is null) return;

        _session = new CookingSession(value);
        Title = value.Title;
        IngredientsText = "Ingredients: " + string.Join(", ", value.Ingredients);
        ShowCurrentStep();
    }

    // Moves to the next step, or leaves the screen after the last one.
    [RelayCommand]
    private async Task Next()
    {
        if (_session is null) return;

        if (_session.IsLastStep)
        {
            await _navigation.GoBackAsync();
            return;
        }

        _timerService.Reset();   // stop the old step's timer
        _session.Next();
        ShowCurrentStep();
    }

    [RelayCommand]
    private void Back()
    {
        if (_session is null) return;

        _timerService.Reset();
        _session.Previous();
        ShowCurrentStep();
    }

    // One button for start / pause / resume. Its label changes with the state.
    [RelayCommand]
    private void ToggleTimer()
    {
        // Only steps with a positive timer can use this.
        if (_session?.CurrentStep.TimerSeconds is not int seconds || seconds <= 0) return;

        if (_timerService.IsRunning)
        {
            _timerService.Pause();
            TimerButtonText = "Resume";
        }
        else if (_timerStartedForStep)
        {
            _timerService.Resume();
            TimerButtonText = "Pause";
        }
        else
        {
            _timerService.Start(TimeSpan.FromSeconds(seconds));
            _timerStartedForStep = true;
            TimerButtonText = "Pause";
        }
    }

    // Stops the timer and shows the full time again.
    [RelayCommand]
    private void ResetTimer()
    {
        _timerService.Reset();
        ShowCurrentStep();
    }

    // Called by the page when it disappears: stop listening and stop the timer.
    // Needed because the timer service lives for the whole app.
    public void Detach()
    {
        _timerService.Tick -= OnTimerTick;
        _timerService.Finished -= OnTimerFinished;
        _timerService.Reset();
    }

    // Copies the current step from the session into the properties the page shows.
    private void ShowCurrentStep()
    {
        if (_session is null) return;

        RecipeStep step = _session.CurrentStep;

        ProgressText = $"Step {_session.StepNumber} of {_session.Recipe.StepCount}";
        Instruction = step.Instruction;
        CanGoBack = !_session.IsFirstStep;
        NextButtonText = _session.IsLastStep ? "Finish" : "Next";

        HasTimer = step.HasTimer;
        TimerText = step.TimerSeconds is int seconds && seconds > 0
            ? FormatTime(TimeSpan.FromSeconds(seconds))
            : "";
        TimerButtonText = "Start timer";
        _timerStartedForStep = false;
    }

    // Runs about once per second while the timer is running.
    private void OnTimerTick(object? sender, EventArgs e)
    {
        TimerText = FormatTime(_timerService.Remaining);
    }

    // Runs once when the countdown reaches zero.
    private void OnTimerFinished(object? sender, EventArgs e)
    {
        TimerText = "Time's up!";
        TimerButtonText = "Start timer";
        _timerStartedForStep = false;
    }

    // 3.89 seconds left shows as 0:04, so the display never shows 0:00 early.
    private static string FormatTime(TimeSpan time)
    {
        int totalSeconds = (int)Math.Ceiling(time.TotalSeconds);
        return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
    }
}