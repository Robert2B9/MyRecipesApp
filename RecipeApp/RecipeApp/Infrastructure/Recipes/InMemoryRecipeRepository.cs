using System.Collections.Generic;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.Infrastructure.Recipes;

/// <summary>
/// Hardcoded sample recipes so the app works without any network or storage.
/// Timer values are in seconds. A null timer means the step has no timer.
/// Lower the numbers if you want a faster demo.
/// </summary>
public class InMemoryRecipeRepository : IRecipeRepository
{
    private readonly IReadOnlyList<Recipe> _recipes = new List<Recipe>
    {
        new Recipe(
            "Scrambled eggs",
            new[] { "3 eggs", "1 tbsp butter", "Salt", "Pepper" },
            new[]
            {
                new RecipeStep("Crack the eggs into a bowl and whisk with a pinch of salt.", null),
                new RecipeStep("Melt the butter in a pan on medium heat.", 30),
                new RecipeStep("Pour in the eggs and stir slowly until they start to set.", 60),
                new RecipeStep("Take the pan off the heat, season with pepper and serve.", null)
            }),

        new Recipe(
            "Simple tomato pasta",
            new[] { "200 g pasta", "1 can crushed tomatoes", "1 garlic clove", "Olive oil", "Salt" },
            new[]
            {
                new RecipeStep("Bring a large pot of salted water to a boil.", null),
                new RecipeStep("Cook the pasta according to the package.", 600),
                new RecipeStep("Meanwhile, fry the chopped garlic in olive oil for a minute.", 60),
                new RecipeStep("Add the crushed tomatoes and let the sauce simmer.", 300),
                new RecipeStep("Drain the pasta, mix it with the sauce and serve.", null)
            })
    };

    public IReadOnlyList<Recipe> GetAll() => _recipes;
}