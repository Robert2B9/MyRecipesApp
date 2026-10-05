using System;
using Microsoft.Maui.Dispatching;
using RecipeApp.Domain.Services;

namespace RecipeApp.Infrastructure.Timers;

/// <summary>
/// A single countdown timer. Only one countdown exists at a time: calling Start
/// while one is running replaces it.
///
/// How it works: a MAUI dispatcher timer "wakes us up" about once per second.
/// It does NOT hold the time left. We work that out ourselves from a deadline
/// (the moment the countdown should end), so a late or skipped wake-up never
/// makes the countdown drift.
///
/// Because the dispatcher timer ticks on the UI thread, the events below are
/// also raised on the UI thread, so a ViewModel can update bound properties
/// directly from its handlers.
/// </summary>
public class CountdownTimerService : ITimerService
{
    // The three states the timer can be in. This replaces juggling several
    // booleans, and makes every method easy to reason about.
    private enum TimerState
    {
        Idle,    // nothing started yet, or finished, or reset
        Running, // counting down
        Paused   // stopped part way, Remaining holds what is left
    }

    // The MAUI timer that wakes us up every second.
    private readonly IDispatcherTimer _timer;

    // Current state of the countdown.
    private TimerState _state = TimerState.Idle;

    // The full length passed to Start. Remembered so Reset can restore it.
    private TimeSpan _duration = TimeSpan.Zero;

    // The moment the countdown ends. Only meaningful while Running.
    private DateTimeOffset _deadline;

    public CountdownTimerService(IDispatcher dispatcher)
    {
        // Create the timer ONCE and reuse it. Pausing/resuming is just
        // Stop()/Start() on this same timer.
        // If DI cannot resolve IDispatcher at startup, replace the constructor
        // parameter with: Dispatcher.GetForCurrentThread() ?? throw new InvalidOperationException()
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.IsRepeating = true;

        // Subscribe ONCE here. Subscribing inside Start would stack handlers
        // and make the countdown run faster each time.
        _timer.Tick += OnTimerTick;
    }

    /// <summary>Time left. While Running it is refreshed on every tick.</summary>
    public TimeSpan Remaining { get; private set; } = TimeSpan.Zero;
    
    /// <summary>True only while actively counting down (false when paused).</summary>
    public bool IsRunning => _state == TimerState.Running;

    /// <summary>Raised about once per second while running, and after Start/Pause/Reset.</summary>
    public event EventHandler? Tick;

    /// <summary>Raised exactly once when the countdown reaches zero.</summary>
    public event EventHandler? Finished;

    /// <summary>
    /// Starts a new countdown. If one is already running or paused, it is
    /// silently replaced. Throws if duration is zero or negative.
    /// </summary>
    public void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");

        // Stop any countdown already in progress before replacing it.
        _timer.Stop();

        _duration = duration;
        Remaining = duration;

        // The deadline is "now + duration". Every tick compares against this.
        _deadline = DateTimeOffset.UtcNow + duration;

        _state = TimerState.Running;
        _timer.Start();

        // Tell listeners straight away so the UI shows the full time at once
        // instead of waiting a second for the first tick.
        RaiseTick();
    }

    /// <summary>
    /// Pauses a running countdown. Does nothing if the timer is not running.
    /// </summary>
    public void Pause()
    {
        if (_state != TimerState.Running)
            return;

        _timer.Stop();

        // Save exactly how much time is left. The deadline is meaningless
        // while paused, so Resume will build a fresh one from this value.
        Remaining = TimeLeftUntilDeadline();

        _state = TimerState.Paused;
        RaiseTick(); // refresh the UI with the exact paused value
    }

    /// <summary>
    /// Continues a paused countdown. Does nothing unless the timer is paused.
    /// </summary>
    public void Resume()
    {
        if (_state != TimerState.Paused)
            return;

        // New deadline = now + whatever was left when we paused.
        _deadline = DateTimeOffset.UtcNow + Remaining;

        _state = TimerState.Running;
        _timer.Start();
    }

    /// <summary>
    /// Stops the countdown and puts Remaining back to the full duration from
    /// the last Start. The timer is then idle: call Start to begin again.
    /// Does nothing useful if Start was never called (Remaining stays zero).
    /// </summary>
    public void Reset()
    {
        _timer.Stop();
        _state = TimerState.Idle;
        Remaining = _duration;
        RaiseTick();
    }

    // Called by the dispatcher timer roughly once per second while running.
    private void OnTimerTick(object? sender, EventArgs e)
    {
        // Guard: ignore stray ticks if we are no longer running. This also
        // protects against Finished firing twice.
        if (_state != TimerState.Running)
            return;

        TimeSpan left = TimeLeftUntilDeadline();

        if (left <= TimeSpan.Zero)
        {
            // Countdown is over. Stop everything BEFORE raising events, so a
            // handler that calls Start() again is not undone by us afterwards.
            _timer.Stop();
            _state = TimerState.Idle;
            Remaining = TimeSpan.Zero;

            RaiseTick();                          // let the UI show 0:00
            Finished?.Invoke(this, EventArgs.Empty);
            return;
        }

        // Still counting. Update the property first, then raise the event,
        // so listeners reading Remaining see the fresh value.
        Remaining = left;
        RaiseTick();
    }

    // Time between now and the deadline, never below zero.
    private TimeSpan TimeLeftUntilDeadline()
    {
        TimeSpan left = _deadline - DateTimeOffset.UtcNow;
        return left < TimeSpan.Zero ? TimeSpan.Zero : left;
    }

    // Single place that raises Tick, so the null-check lives in one spot.
    private void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
}