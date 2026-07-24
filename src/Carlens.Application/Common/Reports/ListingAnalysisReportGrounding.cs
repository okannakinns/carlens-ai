using System.Globalization;
using System.Text.RegularExpressions;

namespace Carlens.Application.Common.Reports;

public static class ListingAnalysisReportGrounding
{
    private const string VerifiedDamageNote =
        "İlanın hasar tablosunda tüm parçaların orijinal olduğu; boyalı, lokal boyalı, " +
        "değişmiş veya belirtilmemiş parça ile tramer tutarı bulunmadığı beyan ediliyor. " +
        "Bu beyan ekspertizde doğrulanmalı.";

    private static readonly string[] NonOriginalStatusNames =
    [
        "Lokal boyalı",
        "Boyalı",
        "Değişmiş",
        "Belirtilmemiş"
    ];

    public static string SanitizeSummary(
        string summary,
        string? damageInformation)
    {
        if (!DeclaresAllPanelsOriginal(damageInformation))
        {
            return summary.Trim();
        }

        var sentences = Regex
            .Split(summary.Trim(), @"(?<=[.!?])\s+")
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .ToList();
        var supportedSentences = sentences
            .Where(sentence => !ContainsUnsupportedDamageConflict(sentence))
            .ToList();

        if (supportedSentences.Count == sentences.Count)
        {
            return summary.Trim();
        }

        var supportedSummary = string.Join(" ", supportedSentences).Trim();
        return string.IsNullOrWhiteSpace(supportedSummary)
            ? VerifiedDamageNote
            : $"{supportedSummary} {VerifiedDamageNote}";
    }

    public static IReadOnlyList<string> SanitizeItems(
        IEnumerable<string> items,
        string? damageInformation)
    {
        var normalizedItems = items
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();

        if (!DeclaresAllPanelsOriginal(damageInformation))
        {
            return normalizedItems;
        }

        var result = new List<string>(normalizedItems.Count);
        var verifiedNoteAdded = false;

        foreach (var item in normalizedItems)
        {
            if (!ContainsUnsupportedDamageConflict(item))
            {
                result.Add(item);
                continue;
            }

            if (!verifiedNoteAdded)
            {
                result.Add(VerifiedDamageNote);
                verifiedNoteAdded = true;
            }
        }

        return result.Count > 0 ? result : [VerifiedDamageNote];
    }

    public static bool DeclaresAllPanelsOriginal(string? damageInformation)
    {
        if (string.IsNullOrWhiteSpace(damageInformation))
        {
            return false;
        }

        var lines = damageInformation
            .ReplaceLineEndings("\n")
            .Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
        var originalLine = lines.FirstOrDefault(line =>
            line.StartsWith("Orijinal:", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(originalLine) ||
            GetStatusValue(originalLine).Equals(
                "Yok",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return NonOriginalStatusNames.All(statusName =>
        {
            var statusLine = lines.FirstOrDefault(line =>
                line.StartsWith(
                    $"{statusName}:",
                    StringComparison.OrdinalIgnoreCase));

            return statusLine is not null &&
                   GetStatusValue(statusLine).Equals(
                       "Yok",
                       StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool ContainsUnsupportedDamageConflict(string value)
    {
        var normalized = value.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        var mentionsDamage =
            normalized.Contains("boya") ||
            normalized.Contains("değiş") ||
            normalized.Contains("orijinal") ||
            normalized.Contains("orjinal") ||
            normalized.Contains("parça") ||
            normalized.Contains("kaporta") ||
            normalized.Contains("hasar");
        var allegesConflict =
            normalized.Contains("çeliş") ||
            normalized.Contains("tutarsız") ||
            normalized.Contains("güven sorunu");

        return mentionsDamage && allegesConflict;
    }

    private static string GetStatusValue(string statusLine)
    {
        var separatorIndex = statusLine.IndexOf(':');
        return separatorIndex < 0
            ? string.Empty
            : statusLine[(separatorIndex + 1)..].Trim();
    }
}
