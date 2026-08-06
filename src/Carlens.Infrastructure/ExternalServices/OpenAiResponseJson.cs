using System.Text;
using System.Text.Json;

namespace Carlens.Infrastructure.ExternalServices;

internal static class OpenAiResponseJson
{
    public static string ExtractOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);

        if (document.RootElement.TryGetProperty(
                "output_text",
                out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (!document.RootElement.TryGetProperty("output", out var outputElement) ||
            outputElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var outputItem in outputElement.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentElement.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement) &&
                    textElement.ValueKind == JsonValueKind.String)
                {
                    builder.Append(textElement.GetString());
                }
            }
        }

        return builder.ToString();
    }
}
