namespace Carlens.Infrastructure.ExternalServices;

public sealed class OpenAiOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "gpt-5.4-mini";
    public string? ApiKey { get; set; }
    public int MaxAnalyzedImages { get; set; } = 8;
    public string ImageDetail { get; set; } = "high";
    public int MaxOutputTokens { get; set; } = 1800;
    public decimal InputCostPerMillionTokensUsd { get; set; } = 0.75m;
    public decimal OutputCostPerMillionTokensUsd { get; set; } = 4.50m;
}
