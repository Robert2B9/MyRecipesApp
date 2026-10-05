namespace RecipeApp.Domain.Models;

public record RecipeStep(string Instruction, int? TimerSeconds)
{ 
    public bool HasTimer => TimerSeconds is > 0;
}
