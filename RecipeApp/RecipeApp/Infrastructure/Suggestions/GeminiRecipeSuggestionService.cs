using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using RecipeApp.Domain.Models;
using RecipeApp.Domain.Services;

namespace RecipeApp.Infrastructure.Suggestions;

/// <summary>
/// Asks Gemini for recipes over its REST API and maps the answer to domain Recipes.
///
/// Flow: read API key -> send request with a JSON schema -> Gemini answers with JSON
/// that matches the schema -> deserialize to small DTOs -> map to Recipe/RecipeStep.
/// </summary>
public class GeminiRecipeSuggestionService : IRecipeSuggestionService
{
    // Tried in order. A 503 (overloaded) or 429 (rate limit) moves on to the next one.
    private static readonly string[] Models =
    {
        "gemini-3.8-flash",
        "gemini-3.5-flash",
        "gemini-3.1-flash-lite"
    };

    private static string EndpointFor(string model) =>
        "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent";

    // File in Resources/Raw (bundled into the app, kept out of git). Contents: { "GeminiApiKey": "..." }
    private const string SecretsFile = "secrets.json";

    // Tells the model what its job is and how to fill the schema.
    private const string SystemPrompt =
        "You are a cooking assistant inside a step-by-step recipe app. " +
        "Return exactly 3 different recipes that match the user's request " +
        "(ingredients they have, cuisine, dietary needs, and so on). " +
        "Follow dietary restrictions strictly. " +
        "Write each step as one short, clear action. " +
        "For steps that involve waiting or timing, set timerSeconds to the duration in seconds; " +
        "for every other step set timerSeconds to 0. " +
        "Answer in the same language as the user's request.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private string? _apiKey; // read once, then cached

    // HttpClient is supplied by DI (registered in MauiProgram).
    public GeminiRecipeSuggestionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Recipe>> SuggestAsync(
        string request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request))
            throw new RecipeSuggestionException("Type what you want to cook first.");

        string apiKey = await GetApiKeyAsync(cancellationToken);
        string requestJson = BuildRequestJson(request);
        string lastError = "";

        foreach (string model in Models)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointFor(model));
                httpRequest.Headers.Add("x-goog-api-key", apiKey);
                httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                    return MapToRecipes(ParseResponse(responseBody));

                int status = (int)response.StatusCode;
                lastError = $"{model} returned {status}: {Shorten(responseBody)}";

                if (status is 503 or 429 or 404)
                    continue; // temporary problem: try the next model

                throw new RecipeSuggestionException($"Gemini returned {status}: {Shorten(responseBody)}");
            }
            catch (HttpRequestException ex)
            {
                throw new RecipeSuggestionException("Could not reach Gemini. Check the internet connection.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new RecipeSuggestionException("Gemini took too long to answer. Try again.", ex);
            }
        }

        throw new RecipeSuggestionException("Gemini is busy right now. Try again in a minute. (" + lastError + ")");
        
    }

    // Builds the JSON we POST. responseSchema forces Gemini's answer into our exact shape.
    private static string BuildRequestJson(string userRequest)
    {
        var body = new
        {
            systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[] { new { parts = new[] { new { text = userRequest } } } },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            title = new { type = "STRING" },
                            ingredients = new { type = "ARRAY", items = new { type = "STRING" } },
                            steps = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        instruction = new { type = "STRING" },
                                        timerSeconds = new { type = "INTEGER" }
                                    },
                                    required = new[] { "instruction", "timerSeconds" }
                                }
                            }
                        },
                        required = new[] { "title", "ingredients", "steps" }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(body);
    }

    // Gemini wraps our JSON as TEXT inside candidates[0].content.parts[0].text.
    // Dig that out, then deserialize it into the DTOs below.
    private static List<RecipeDto> ParseResponse(string responseBody)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);

            string? json = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return JsonSerializer.Deserialize<List<RecipeDto>>(json ?? "[]", JsonOptions) ?? new List<RecipeDto>();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException
                                       or InvalidOperationException or IndexOutOfRangeException)
        {
            // Includes the case where Gemini blocked the answer and returned no candidates.
            throw new RecipeSuggestionException("Gemini gave an answer the app could not read. Try again.", ex);
        }
    }

    // DTO -> domain. Anything unusable (no title, steps or ingredients) is dropped
    // here, so the cooking screen never receives a broken recipe.
    private static List<Recipe> MapToRecipes(List<RecipeDto> dtos)
    {
        var recipes = new List<Recipe>();

        foreach (RecipeDto dto in dtos)
        {
            var steps = (dto.Steps ?? new List<StepDto>())
                .Where(step => !string.IsNullOrWhiteSpace(step.Instruction))
                // 0 (or less) means "no timer", which RecipeStep stores as null.
                .Select(step => new RecipeStep(step.Instruction, step.TimerSeconds > 0 ? step.TimerSeconds : null))
                .ToList();

            var ingredients = (dto.Ingredients ?? new List<string>())
                .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient))
                .ToList();

            if (string.IsNullOrWhiteSpace(dto.Title) || steps.Count == 0 || ingredients.Count == 0)
                continue;

            recipes.Add(new Recipe(dto.Title, ingredients, steps));
        }

        if (recipes.Count == 0)
            throw new RecipeSuggestionException("Gemini did not return a usable recipe. Try rephrasing.");

        return recipes;
    }

    // Reads the key from Resources/Raw/secrets.json inside the app package.
    // (User secrets live on the dev PC, so a phone or emulator cannot read them.)
    private async Task<string> GetApiKeyAsync(CancellationToken cancellationToken)
    {
        if (_apiKey is not null)
            return _apiKey;

        try
        {
            await using Stream stream = await FileSystem.OpenAppPackageFileAsync(SecretsFile);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            _apiKey = document.RootElement.GetProperty("GeminiApiKey").GetString();
        }
        catch (Exception ex) when (ex is FileNotFoundException or JsonException or KeyNotFoundException)
        {
            throw new RecipeSuggestionException(
                "Could not read the API key. Check Resources/Raw/secrets.json.", ex);
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new RecipeSuggestionException("The API key in secrets.json is empty.");

        return _apiKey;
    }

    // Keeps error messages short enough to display.
    private static string Shorten(string text) => text.Length <= 300 ? text : text[..300] + "...";

    // Shapes that match the JSON schema above. Only used inside this class.
    internal record StepDto(string Instruction, int TimerSeconds);
    internal record RecipeDto(string Title, List<string> Ingredients, List<StepDto> Steps);
}