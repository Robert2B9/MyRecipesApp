using System.Collections.Generic;
using RecipeApp.Domain.Models;

namespace RecipeApp.Domain.Services;

/// <summary>
/// Where recipes come from. The UI only knows this interface, so the source
/// (hardcoded list now, a database or AI later) can change without touching it.
/// </summary>
public interface IRecipeRepository
{
    IReadOnlyList<Recipe> GetAll();
}