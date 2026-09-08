using System.Reflection;
using System.Text.RegularExpressions;

namespace DriveclubSaveEditor;

internal static class LiveryAssets
{
    internal sealed record Choice(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    internal static IReadOnlyList<Choice> Designs { get; } = LoadChoices("livery_designs.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> RaceBadges { get; } = LoadChoices("race_badges.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> RaceFonts { get; } = LoadChoices("race_fonts.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> ClubShields { get; } = LoadChoices("club_shields.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> ClubFlourishes { get; } = LoadChoices("club_flourishes.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> ClubSymbols { get; } = LoadChoices("club_symbols.txt", FriendlyAssetName);
    internal static IReadOnlyList<Choice> Stickers { get; } = LoadChoices("stickers.txt", FriendlyAssetName);

    internal static IReadOnlyList<Choice> ClubBadgeAssets { get; } =
        new[] { new Choice("none", "None") }
            .Concat(ClubShields)
            .Concat(ClubFlourishes)
            .Concat(ClubSymbols)
            .ToArray();

    internal static IReadOnlyList<Choice> StickerAssets { get; } =
        new[] { new Choice("off", "Off") }
            .Concat(Stickers)
            .ToArray();

    private static IReadOnlyList<Choice> LoadChoices(string fileName, Func<string, string> namer)
    {
        string[] lines = ReadEmbeddedText(fileName)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Distinct(StringComparer.Ordinal)
            .Select(x => new Choice(x, namer(x)))
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    internal static string FriendlyAssetName(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "None";
        if (id.Equals("off", StringComparison.OrdinalIgnoreCase) || id.Equals("none", StringComparison.OrdinalIgnoreCase))
            return "None";

        string s = id;
        s = Regex.Replace(s, "^livery_", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "^clubbadge_(shield|flourish|symbol)_", "$1 ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "^badge_", "Race Badge ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, "^font_", "Race Number Font ", RegexOptions.IgnoreCase);
        s = s.Replace('_', ' ');
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }

    internal static Choice? FindChoice(IEnumerable<Choice> choices, string? id)
    {
        if (id is null) return null;
        return choices.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
    }

    private static string ReadEmbeddedText(string fileName)
    {
        Assembly assembly = typeof(LiveryAssets).Assembly;
        string? resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(".Data." + fileName, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
            throw new FileNotFoundException($"Embedded customisation resource '{fileName}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Embedded customisation resource '{resource}' could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
