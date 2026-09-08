using System.Globalization;

namespace DriveclubSaveEditor;

/// <summary>
/// Conservative editor for Driveclub's CustomisationS1/S2 profile strings.
/// The original text is returned byte-for-byte when no confirmed field was actually changed.
/// When a field is changed, unknown lines, blank lines, line ordering and the original newline
/// convention are preserved as closely as possible.
/// </summary>
internal sealed class CustomisationProfile
{
    internal sealed record PaintSetting(int FinishCode, int PaletteIndex);
    internal sealed record BadgeLayer(int Layer, int PaintIndex, string AssetId, float Scale);

    private readonly string _originalText;
    private readonly string _newline;
    private readonly List<string> _lines;
    private bool _dirty;

    internal bool IsDirty => _dirty;
    internal string OriginalText => _originalText;

    internal CustomisationProfile(string text)
    {
        _originalText = text ?? string.Empty;
        _newline = _originalText.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : (_originalText.Contains('\r') && !_originalText.Contains('\n') ? "\r" : "\n");

        string normalized = _originalText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        _lines = normalized.Split('\n', StringSplitOptions.None).ToList();
    }

    internal PaintSetting GetPaint(int index)
    {
        string prefix = $"paint {index} ";
        string? line = FindLine(prefix);
        if (line is null) return new PaintSetting(0, 0);
        string[] p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 4 && int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int finish)
            && int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int colour)
            ? new PaintSetting(finish, colour)
            : new PaintSetting(0, 0);
    }

    internal void SetPaint(int index, int finishCode, int paletteIndex)
    {
        string prefix = $"paint {index} ";
        string? existing = FindLine(prefix);
        if (existing is null && finishCode == 0 && paletteIndex == 0)
            return;
        if (existing is not null && GetPaint(index) == new PaintSetting(finishCode, paletteIndex))
            return;
        ReplaceOrAppend(prefix, $"paint {index} {finishCode} {paletteIndex}");
    }

    internal int GetRaceNumber()
    {
        string? line = FindLine("race_number ");
        if (line is null) return 0;
        string[] p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 2 && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
    }

    internal void SetRaceNumber(int number)
    {
        string? existing = FindLine("race_number ");
        if (existing is null && number == 0)
            return;
        if (existing is not null && GetRaceNumber() == number)
            return;
        ReplaceOrAppend("race_number ", $"race_number {number}");
    }

    internal string GetSlot(string slot, int index, string fallback = "off")
    {
        string prefix = $"slot {slot} {index} ";
        string? line = FindLine(prefix);
        if (line is null) return fallback;
        string[] p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 4 ? p[3] : fallback;
    }

    /// <summary>
    /// Replaces only the asset id. If the slot already exists, its trailing flag/extra tokens
    /// are retained unless an explicit trailingFlag is supplied.
    /// </summary>
    internal void SetSlot(string slot, int index, string assetId, int? trailingFlag = null)
    {
        string prefix = $"slot {slot} {index} ";
        string? existing = FindLine(prefix);
        string naturalDefault = slot.Equals("design", StringComparison.Ordinal) ? "livery_blank_01" : "off";
        if (existing is null && !trailingFlag.HasValue && string.Equals(assetId, naturalDefault, StringComparison.Ordinal))
            return;
        if (existing is not null && string.Equals(GetSlot(slot, index), assetId, StringComparison.Ordinal) && !trailingFlag.HasValue)
            return;

        string tail;
        if (trailingFlag.HasValue)
        {
            tail = trailingFlag.Value.ToString(CultureInfo.InvariantCulture);
        }
        else if (existing is not null)
        {
            string[] p = existing.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            tail = p.Length >= 5 ? string.Join(' ', p.Skip(4)) : "0";
        }
        else
        {
            tail = "0";
        }

        ReplaceOrAppend(prefix, $"slot {slot} {index} {assetId} {tail}");
    }

    internal BadgeLayer GetBadgeLayer(int layer)
    {
        string prefix = $"badge_layer {layer} ";
        string? line = FindLine(prefix);
        if (line is null) return new BadgeLayer(layer, 0, "none", 1.0f);
        string[] p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length >= 5 && int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int paint)
            && float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
            return new BadgeLayer(layer, paint, p[3], scale);
        return new BadgeLayer(layer, 0, "none", 1.0f);
    }

    internal void SetBadgeLayer(int layer, int paintIndex, string assetId, float scale)
    {
        string prefix = $"badge_layer {layer} ";
        string? existing = FindLine(prefix);
        if (existing is null && paintIndex == 0 && string.Equals(assetId, "none", StringComparison.Ordinal) && Math.Abs(scale - 1.0f) < 0.0000005f)
            return;
        if (existing is not null)
        {
            BadgeLayer current = GetBadgeLayer(layer);
            if (current.PaintIndex == paintIndex && string.Equals(current.AssetId, assetId, StringComparison.Ordinal)
                && Math.Abs(current.Scale - scale) < 0.0000005f)
                return;
        }

        ReplaceOrAppend(prefix,
            $"badge_layer {layer} {paintIndex} {assetId} {scale.ToString("0.000000", CultureInfo.InvariantCulture)}");
    }

    internal string Build()
    {
        if (!_dirty)
            return _originalText;
        return string.Join(_newline, _lines);
    }

    private string? FindLine(string prefix) =>
        _lines.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));

    private void ReplaceOrAppend(string prefix, string replacement)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].StartsWith(prefix, StringComparison.Ordinal))
            {
                if (string.Equals(_lines[i], replacement, StringComparison.Ordinal))
                    return;
                _lines[i] = replacement;
                _dirty = true;
                return;
            }
        }

        // If the original string ended in a newline, Split() leaves one empty final line.
        // Insert before it so that the trailing newline convention is retained.
        int insertAt = _lines.Count > 0 && _lines[^1].Length == 0 ? _lines.Count - 1 : _lines.Count;
        _lines.Insert(insertAt, replacement);
        _dirty = true;
    }
}
