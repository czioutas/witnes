namespace Api.Application.Settings;

public class LlmSettings
{
    /// <summary>
    /// The base URL for the LLM service
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// The model name to use (e.g., llama3:8b-instruct, gpt-4, etc.)
    /// </summary>
    public string Model { get; set; } = "llama3:8b-instruct";

    /// <summary>
    /// Timeout in seconds for LLM API calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum tokens for LLM response
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Temperature for LLM sampling (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// Whether to use streaming responses
    /// </summary>
    public bool UseStreaming { get; set; } = false;
}
