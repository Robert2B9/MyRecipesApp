using System;
using RecipeApp.Domain.Models;

namespace RecipeApp.Domain.Cooking;

public class CookingSession
{
    public Recipe Recipe { get; }
    public CookingSession(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (recipe.Steps.Count == 0) throw new ArgumentException("Recipe must have at least one step");
        if (recipe.Ingredients.Count == 0) throw new ArgumentException("Recipe must have at least one ingredient");
        Recipe = recipe;
    }
    public int CurrentStepIndex { get; private set; }
    public RecipeStep CurrentStep
    {
        get { return Recipe.Steps[CurrentStepIndex]; }
    }
    public bool IsFirstStep
    {
        get { return CurrentStepIndex == 0; }
    }
    public bool IsLastStep 
    {
        get { return CurrentStepIndex == Recipe.Steps.Count - 1; }
    }
    public int StepNumber
    {
        get { return CurrentStepIndex + 1; }
    }

    public void Next()
    {
        if (!IsLastStep) CurrentStepIndex++;
    }

    public void Previous()
    {
        if(!IsFirstStep) CurrentStepIndex--;
    }
}