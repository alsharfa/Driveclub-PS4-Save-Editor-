using System.Globalization;
using System.Reflection;

namespace DriveclubSaveEditor;

internal static class ProgressionData
{
    internal sealed record PlayerRankRow(int Rank, ulong FameThreshold, string? CapTag);
    internal sealed record ClubLevelRow(int Level, ulong FameThreshold);
    internal sealed record EliteRow(int Row, ulong FameRequired, uint TourStarsRequired, uint TargetTimeMs, string TrackId, string VehicleId);
    internal sealed record VehicleLevelRow(int Level, IReadOnlyList<ulong> Thresholds);

    internal static IReadOnlyList<PlayerRankRow> PlayerRanks { get; } = LoadPlayerRanks();
    internal static IReadOnlyList<ClubLevelRow> ClubLevels { get; } = LoadClubLevels();
    internal static IReadOnlyList<EliteRow> EliteRows { get; } = LoadEliteRows();
    internal static IReadOnlyList<string> VehicleColumns { get; } = LoadVehicleTable().Columns;
    internal static IReadOnlyList<VehicleLevelRow> VehicleLevels { get; } = LoadVehicleTable().Rows;


    internal static int DriverRankForFame(ulong fame)
    {
        // EBOOT v1.28 rank calculation counts qualifying base player.csv rows,
        // then qualifying elite.csv FameRequired rows for the player category.
        int rank = PlayerRanks.Count(x => x.FameThreshold <= fame);
        rank += EliteRows.Count(x => x.FameRequired <= fame);
        return Math.Clamp(rank, 1, 120);
    }

    internal static ulong FameForDriverRank(int rank)
    {
        rank = Math.Clamp(rank, 1, 120);
        if (rank <= PlayerRanks.Count)
            return PlayerRanks[rank - 1].FameThreshold;

        int eliteIndex = rank - PlayerRanks.Count - 1;
        return EliteRows[eliteIndex].FameRequired;
    }

    internal static ulong? FameForNextDriverRank(ulong fame)
    {
        int rank = DriverRankForFame(fame);
        if (rank >= 120)
            return null;
        return FameForDriverRank(rank + 1);
    }

    private static IReadOnlyList<PlayerRankRow> LoadPlayerRanks()
    {
        string[] lines = ReadEmbeddedText("player.csv")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rows = new List<PlayerRankRow>(lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            string? cap = null;
            int bracket = raw.IndexOf('[');
            if (bracket >= 0)
            {
                int close = raw.IndexOf(']', bracket + 1);
                if (close < 0)
                    throw new InvalidDataException($"Invalid player.csv cap marker on row {i + 1}.");
                cap = raw.Substring(bracket + 1, close - bracket - 1);
                raw = raw[..bracket];
            }

            if (!ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong fame))
                throw new InvalidDataException($"Invalid player.csv value on row {i + 1}.");
            rows.Add(new PlayerRankRow(i + 1, fame, cap));
        }

        if (rows.Count != 60)
            throw new InvalidDataException($"Unexpected player.csv row count {rows.Count}; expected 60.");
        return rows;
    }

    private static IReadOnlyList<ClubLevelRow> LoadClubLevels()
    {
        string[] lines = ReadEmbeddedText("club.csv")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rows = new List<ClubLevelRow>(lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!ulong.TryParse(lines[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong fame))
                throw new InvalidDataException($"Invalid club.csv value on row {i + 1}.");
            rows.Add(new ClubLevelRow(i + 1, fame));
        }
        return rows;
    }

    private static IReadOnlyList<EliteRow> LoadEliteRows()
    {
        string[] lines = ReadEmbeddedText("elite.csv")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rows = new List<EliteRow>(lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            if (parts.Length != 5 ||
                !ulong.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong fame) ||
                !uint.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint stars) ||
                !uint.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out uint timeMs))
                throw new InvalidDataException($"Invalid elite.csv row {i + 1}.");

            rows.Add(new EliteRow(i + 1, fame, stars, timeMs, parts[3], parts[4]));
        }
        return rows;
    }

    private static (IReadOnlyList<string> Columns, IReadOnlyList<VehicleLevelRow> Rows) LoadVehicleTable()
    {
        string[] lines = ReadEmbeddedText("vehicles.csv")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            throw new InvalidDataException("vehicles.csv is empty.");

        string[] header = lines[0].Split(',');
        if (header.Length < 2 || !header[0].Equals("VEHICLE LEVEL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unexpected vehicles.csv header.");

        var columns = header.Skip(1).ToArray();
        var rows = new List<VehicleLevelRow>(lines.Length - 1);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(',');
            if (parts.Length != header.Length ||
                !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
                throw new InvalidDataException($"Invalid vehicles.csv row {i + 1}.");

            var values = new ulong[columns.Length];
            for (int j = 0; j < values.Length; j++)
            {
                if (!ulong.TryParse(parts[j + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[j]))
                    throw new InvalidDataException($"Invalid vehicles.csv numeric value at row {i + 1}, column {j + 2}.");
            }
            rows.Add(new VehicleLevelRow(level, values));
        }

        return (columns, rows);
    }

    private static string ReadEmbeddedText(string fileName)
    {
        Assembly assembly = typeof(ProgressionData).Assembly;
        string? resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(".Data." + fileName, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
            throw new FileNotFoundException($"Embedded progression resource '{fileName}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Embedded progression resource '{resource}' could not be opened.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
