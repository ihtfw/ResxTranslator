using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

class CompletionChoiceMessage
{
    [JsonPropertyName("content")] public string Content { get; set; }
}

class CompletionChoice
{
    [JsonPropertyName("message")] public CompletionChoiceMessage Message { get; set; }
}

class CompletionResponse
{
    [JsonPropertyName("choices")] public List<CompletionChoice> Choices { get; set; } = new();
}

public class Translator
{
    private readonly string _apiKey;
    private readonly HttpClient _client = new HttpClient();

    public Translator(string apiKey)
    {
        _apiKey = apiKey;
    }

    public string Model { get; set; } = "gpt-4o";

    public string Prompt { get; set; } = """
                                         don't include original line in result.
                                         don't explain.
                                         don't add language prefix.
                                         keep templates.
                                         Translate whole line as is.
                                         Keep case.
                                         from language ISO 639-1 '{0}' to language ISO 639-1 '{1}'
                                         Text to translate: {2}
                                         """;

    public int MaxTokens { get; set; } = 100;

    /// <summary>
    /// Translate the value from one language to another via ChatGPT API.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<string> TranslateAsync(string from, string to, string value)
    {
        var url = $"https://api.openai.com/v1/chat/completions";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        var payload = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = string.Format(Prompt, from, to, value)
                }
            }
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var completion = JsonSerializer.Deserialize<CompletionResponse>(content);
        if (completion == null || completion.Choices.Count == 0)
        {
            throw new NotImplementedException("No translation available.");
        }

        var completionChoice = completion.Choices[0];
        if (completionChoice.Message == null)
        {
            throw new NotImplementedException("No translation available.");
        }

        if (string.IsNullOrWhiteSpace(completionChoice.Message.Content))
        {
            throw new NotImplementedException("No translation available.");
        }
        return completionChoice.Message.Content;
    }
}