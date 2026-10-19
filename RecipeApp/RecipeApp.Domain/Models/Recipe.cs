namespace RecipeApp.Domain.Models;

public record Recipe
{
    public string Title { get; }
    public IReadOnlyList<string> Ingredients;
    public IReadOnlyList<RecipeStep> Steps;
    public int StepCount => Steps.Count;

    public Recipe(string title, IReadOnlyList<string> ingredients, IReadOnlyList<RecipeStep> steps)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(ingredients);
        ArgumentNullException.ThrowIfNull(steps);
        if (ingredients.Count == 0) throw new ArgumentException("Recipe must have at least one ingredient");
        if (steps.Count == 0) throw new ArgumentException("Recipe must have at least one step");
        Title = title;
        Ingredients = ingredients;
        Steps = steps;
    }
}