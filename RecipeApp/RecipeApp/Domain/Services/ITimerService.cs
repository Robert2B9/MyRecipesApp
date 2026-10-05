using System;

namespace RecipeApp.Domain.Services;
//Interface for different Timers
public interface ITimerService
{
    TimeSpan Remaining { get; }
    bool IsRunning { get; }
    //Should only be called when no other timer is running, throw InvalidOperationException
    void Start(TimeSpan duration);
    //Shouldn't be called if nothing is running
    void Pause();
    //Resumes a paused timer
    void Resume();
    //Resets differently depending on what timer it is
    void Reset();

    event EventHandler? Tick;
    event EventHandler? Finished;
    

}