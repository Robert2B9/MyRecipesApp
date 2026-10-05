using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RecipeApp.ViewModels;
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] string _greeting = "Hello";
    [ObservableProperty] int _count = 0;
    
    [RelayCommand]
    private void Increment()
    {
        Count++;
        Greeting = $"Hello {Count}!";
    }


}