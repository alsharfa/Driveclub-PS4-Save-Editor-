using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace DriveclubSaveEditor;

public sealed class DriveclubSave
{
    public const int ExpectedSaveVersion = 65519;

    private const uint EntityGameSettings = 0x1E32;
    private const uint EntityStatsStore = 0x1E33;
    private const uint EntityRank = 0x1E34;
    private const uint EntityGameSession = 0x6AEE;
    private const uint EntityPlayerProfile = 0xAE0E;
    private const uint EntityUnknownAe1d = 0xAE1D;
    private const uint EntityLeaderboard = 0xCDDE;
    private const uint EntityFame = 0xFAEC;
    private const uint EntityStoreCatalog = 0xA1A4510E;

    // Verified v1.28 entity chain. Region support is title-ID neutral, but the save must still
    // be the same Driveclub container revision. Requiring the exact component sequence avoids
    // accepting an unrelated/corrupt file that merely happens to contain a few known entity IDs.
    private static readonly uint[] ExpectedEntityOrder =
    [
        EntityGameSettings,
        EntityStatsStore,
        EntityRank,
        EntityGameSession,
        EntityPlayerProfile,
        EntityUnknownAe1d,
        EntityLeaderboard,
        EntityFame,
        EntityStoreCatalog
    ];

    public sealed class FloatEntry
    {
        public string Name { get; init; } = "";
        public float Value { get; set; }
    }

    public sealed class StringEntry
    {
        public string Name { get; init; } = "";
        public string Value { get; set; } = "";
    }

    public sealed class StatsStoreEntry
    {
        public string Name { get; init; } = "";
        public int Kind { get; init; }
        public ulong Flags { get; init; }

        public ulong? Value1 { get; set; }
        public uint? Value2 { get; set; }
        public uint? Value3 { get; set; }
        public uint? Value4 { get; set; }
        public uint? Value5 { get; set; }

        internal int Value1Offset { get; init; } = -1;
        internal int Value2Offset { get; init; } = -1;
        internal int Value3Offset { get; init; } = -1;
        internal int Value4Offset { get; init; } = -1;
        internal int Value5Offset { get; init; } = -1;
    }

    public sealed class ScalarEntry
    {
        public string Name { get; init; } = "";
        public string Type { get; init; } = "";
        public long SignedValue { get; set; }
        public ulong UnsignedValue { get; set; }
        public bool IsUnsigned { get; init; }
        internal int Offset { get; init; }
        internal int Width { get; init; }
    }

    public sealed class StoreCatalogEntry
    {
        public int Index { get; init; }
        public int Value { get; set; }
        internal int Offset { get; init; }
    }

    private sealed class EntityInfo
    {
        public uint Type { get; init; }
        public int Version { get; init; }
        public int Size { get; init; }
        public int HeaderOffset { get; init; }
        public int DataOffset => HeaderOffset + 12;
        public int EndOffset => DataOffset + Size;
    }

    private byte[] _original = Array.Empty<byte>();
    private readonly List<EntityInfo> _entities = new();

    private int _floatStart;
    private int _floatEnd;
    private int _stringStart;
    private int _stringEnd;

    public string FilePath { get; private set; } = "";
    public int SaveVersion { get; private set; }
    public int ProfileVersion { get; private set; }
    public bool ChecksumValid { get; private set; }
    public string StoredChecksumHex { get; private set; } = "";
    public string ComputedChecksumHex { get; private set; } = "";

    public IReadOnlyList<FloatEntry> FloatEntries => _floatEntries;
    public IReadOnlyList<StringEntry> StringEntries => _stringEntries;
    public IReadOnlyList<StatsStoreEntry> StatsStoreEntries => _statsStoreEntries;
    public IReadOnlyList<ScalarEntry> RankEntries => _rankEntries;
    public IReadOnlyList<ScalarEntry> FameEntries => _fameEntries;
    public IReadOnlyList<StoreCatalogEntry> StoreCatalogEntries => _storeCatalogEntries;

    /// <summary>
    /// Effective player Fame used by the v1.28 EBOOT player-rank path.
    /// The game getter for player category 0 returns serialized Fame[2] + Fame[1].
    /// </summary>
    public long EffectivePlayerFame
    {
        get
        {
            if (_fameEntries.Count < 3) return 0;
            return checked(_fameEntries[2].SignedValue + _fameEntries[1].SignedValue);
        }
    }

    /// <summary>
    /// Sets a coherent, zero-pending player Fame state for the mapped player category.
    /// The later serialized Fame channel (Fame[3]) is left untouched.
    /// </summary>
    public void SetEffectivePlayerFame(long fame)
    {
        if (fame < 0)
            throw new ArgumentOutOfRangeException(nameof(fame), "Player Fame cannot be negative.");
        if (_fameEntries.Count < 3)
            throw new InvalidDataException("FameComponent does not contain the expected player Fame fields.");

        // Normalized state: base/snapshot = target, pending = 0, total = target.
        // The EBOOT getter then returns Fame[2] + Fame[1] == target.
        _fameEntries[0].SignedValue = fame;
        _fameEntries[1].SignedValue = 0;
        _fameEntries[2].SignedValue = fame;
    }

    private readonly List<FloatEntry> _floatEntries = new();
    private readonly List<StringEntry> _stringEntries = new();
    private readonly List<StatsStoreEntry> _statsStoreEntries = new();
    private readonly List<ScalarEntry> _rankEntries = new();
    private readonly List<ScalarEntry> _fameEntries = new();
    private readonly List<StoreCatalogEntry> _storeCatalogEntries = new();

    public static DriveclubSave Load(string path)
    {
        var save = new DriveclubSave
        {
            FilePath = path,
            _original = File.ReadAllBytes(path)
        };
        save.Parse();
        return save;
    }

    private void Parse()
    {
        _entities.Clear();
        _floatEntries.Clear();
        _stringEntries.Clear();
        _statsStoreEntries.Clear();
        _rankEntries.Clear();
        _fameEntries.Clear();
        _storeCatalogEntries.Clear();

        if (_original.Length < 32)
            throw new InvalidDataException("File is too small to be a Driveclub profile.sav.");

        // Region-neutral validation: Driveclub profile.sav does not need a CUSA/title-ID check here.
        // Accept every regional save that uses the verified v1.28 container revision, then validate
        // the component layout/end marker below. This keeps cross-region support safe.
        SaveVersion = BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(0, 4));
        if (SaveVersion != ExpectedSaveVersion)
            throw new InvalidDataException($"Unsupported Driveclub save format version {SaveVersion}. This editor supports compatible v1.28 saves from all regions (save version {ExpectedSaveVersion}).");

        byte[] storedChecksum = _original.AsSpan(4, 8).ToArray();
        byte[] md5 = MD5.HashData(_original.AsSpan(12));
        byte[] computedChecksum = md5.AsSpan(0, 8).ToArray();
        ChecksumValid = storedChecksum.AsSpan().SequenceEqual(computedChecksum);
        StoredChecksumHex = Convert.ToHexString(storedChecksum);
        ComputedChecksumHex = Convert.ToHexString(computedChecksum);

        ProfileVersion = BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(12, 4));

        int pos = 16;
        for (int i = 0; i < ExpectedEntityOrder.Length; i++)
        {
            EnsureRemaining(_original, pos, 12);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(_original.AsSpan(pos, 4));
            int version = BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(pos + 4, 4));
            int size = BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(pos + 8, 4));
            if (type != ExpectedEntityOrder[i])
                throw new InvalidDataException($"Unexpected Driveclub entity #{i + 1}: 0x{type:X8}; expected 0x{ExpectedEntityOrder[i]:X8}. The file is not the supported v1.28 profile layout.");
            if (size < 0)
                throw new InvalidDataException($"Entity 0x{type:X8} has a negative size.");
            EnsureRemaining(_original, pos + 12, size);

            _entities.Add(new EntityInfo
            {
                Type = type,
                Version = version,
                Size = size,
                HeaderOffset = pos
            });
            pos += 12 + size;
        }

        EnsureRemaining(_original, pos, 4);
        int endMarker = BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(pos, 4));
        if (endMarker != 0xFFFF)
            throw new InvalidDataException($"Invalid Driveclub save end marker 0x{endMarker:X8}; expected 0x0000FFFF.");
        if (pos + 4 != _original.Length)
            throw new InvalidDataException("Unexpected trailing data after Driveclub profile end marker.");

        ParseStatsStore(RequireEntity(EntityStatsStore));
        ParseRank(RequireEntity(EntityRank));
        ParsePlayerProfile(RequireEntity(EntityPlayerProfile));
        ParseFame(RequireEntity(EntityFame));
        ParseStoreCatalog(RequireEntity(EntityStoreCatalog));
    }

    private EntityInfo RequireEntity(uint type)
    {
        return _entities.FirstOrDefault(x => x.Type == type)
            ?? throw new InvalidDataException($"Required Driveclub entity 0x{type:X8} was not found.");
    }

    private void ParseStatsStore(EntityInfo entity)
    {
        int p = entity.DataOffset;
        ulong count = ReadUInt64(_original, ref p, entity.EndOffset);
        if (count > 100_000)
            throw new InvalidDataException($"Invalid StatsStore count: {count}.");

        for (ulong i = 0; i < count; i++)
        {
            string name = ReadString4(_original, ref p, entity.EndOffset);
            int kind = ReadInt32(_original, ref p, entity.EndOffset);
            ulong flags = ReadUInt64(_original, ref p, entity.EndOffset);

            ulong? v1 = null;
            uint? v2 = null, v3 = null, v4 = null, v5 = null;
            int o1 = -1, o2 = -1, o3 = -1, o4 = -1, o5 = -1;

            if ((flags & 1) != 0)
            {
                o1 = p;
                v1 = ReadUInt64(_original, ref p, entity.EndOffset);
            }
            if ((flags & 2) != 0)
            {
                o2 = p;
                v2 = ReadUInt32(_original, ref p, entity.EndOffset);
            }
            if ((flags & 4) != 0)
            {
                o3 = p;
                v3 = ReadUInt32(_original, ref p, entity.EndOffset);
            }
            if ((flags & 8) != 0)
            {
                o4 = p;
                v4 = ReadUInt32(_original, ref p, entity.EndOffset);
            }
            if ((flags & 0x10) != 0)
            {
                o5 = p;
                v5 = ReadUInt32(_original, ref p, entity.EndOffset);
            }

            _statsStoreEntries.Add(new StatsStoreEntry
            {
                Name = name,
                Kind = kind,
                Flags = flags,
                Value1 = v1,
                Value2 = v2,
                Value3 = v3,
                Value4 = v4,
                Value5 = v5,
                Value1Offset = o1,
                Value2Offset = o2,
                Value3Offset = o3,
                Value4Offset = o4,
                Value5Offset = o5
            });
        }

        if (p != entity.EndOffset)
            throw new InvalidDataException($"StatsStore parse ended at 0x{p:X}, expected 0x{entity.EndOffset:X}.");
    }

    private void ParseRank(EntityInfo entity)
    {
        if (entity.Size != 20)
            throw new InvalidDataException($"Unexpected RankComponent size {entity.Size}; expected 20.");

        for (int i = 0; i < 5; i++)
        {
            int offset = entity.DataOffset + i * 4;
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_original.AsSpan(offset, 4));
            _rankEntries.Add(new ScalarEntry
            {
                Name = $"Rank[{i}]",
                Type = "uint32",
                UnsignedValue = value,
                IsUnsigned = true,
                Offset = offset,
                Width = 4
            });
        }
    }

    private void ParsePlayerProfile(EntityInfo entity)
    {
        var floatSig = BuildPrefixSignature(16, "best_in_accolade_group_cars");
        _floatStart = Find(_original, floatSig, entity.DataOffset, entity.EndOffset);
        if (_floatStart < 0)
            throw new InvalidDataException("Driveclub 16-entry profile float table was not found inside PlayerProfileComponent.");

        int p = _floatStart;
        int floatCount = ReadInt32(_original, ref p, entity.EndOffset);
        if (floatCount != 16)
            throw new InvalidDataException($"Unexpected profile float count: {floatCount}.");

        for (int i = 0; i < floatCount; i++)
        {
            string name = ReadString4(_original, ref p, entity.EndOffset);
            EnsureRemaining(_original, p, 4, entity.EndOffset);
            float value = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(p, 4)));
            p += 4;
            _floatEntries.Add(new FloatEntry { Name = name, Value = value });
        }
        _floatEnd = p;

        var stringSig = BuildPrefixSignature(100, "AssetNewness");
        _stringStart = Find(_original, stringSig, entity.DataOffset, entity.EndOffset);
        if (_stringStart < 0)
            throw new InvalidDataException("Driveclub 100-entry profile string table was not found inside PlayerProfileComponent.");

        p = _stringStart;
        int stringCount = ReadInt32(_original, ref p, entity.EndOffset);
        if (stringCount != 100)
            throw new InvalidDataException($"Unexpected profile string count: {stringCount}.");

        for (int i = 0; i < stringCount; i++)
        {
            string name = ReadString4(_original, ref p, entity.EndOffset);
            int valueLength = ReadInt32(_original, ref p, entity.EndOffset);
            if (valueLength < 0 || valueLength > 4_000_000)
                throw new InvalidDataException($"Invalid string value length for '{name}': {valueLength}.");
            if (p > entity.EndOffset - valueLength)
                throw new EndOfStreamException($"String value for '{name}' exceeds PlayerProfileComponent.");
            string value = Encoding.UTF8.GetString(_original, p, valueLength);
            p += valueLength;
            _stringEntries.Add(new StringEntry { Name = name, Value = value });
        }
        _stringEnd = p;

        if (_floatEnd > _stringStart)
            throw new InvalidDataException("Unexpected Driveclub PlayerProfile table ordering.");
        if (_stringEnd > entity.EndOffset)
            throw new InvalidDataException("Driveclub PlayerProfile string table exceeds component bounds.");
    }

    private void ParseFame(EntityInfo entity)
    {
        if (entity.Size != 40)
            throw new InvalidDataException($"Unexpected FameComponent size {entity.Size}; expected 40.");

        int p = entity.DataOffset;
        AddSignedScalar(_fameEntries, "Fame[0]", "int64", p, 8); p += 8;
        AddSignedScalar(_fameEntries, "Fame[1]", "int64", p, 8); p += 8;
        AddSignedScalar(_fameEntries, "Fame[2]", "int64", p, 8); p += 8;
        AddSignedScalar(_fameEntries, "FameBool[0]", "int32/bool", p, 4); p += 4;
        AddSignedScalar(_fameEntries, "Fame[3]", "int64", p, 8); p += 8;
        AddSignedScalar(_fameEntries, "FameBool[1]", "int32/bool", p, 4); p += 4;
    }

    private void AddSignedScalar(List<ScalarEntry> target, string name, string type, int offset, int width)
    {
        long value = width == 8
            ? BinaryPrimitives.ReadInt64LittleEndian(_original.AsSpan(offset, 8))
            : BinaryPrimitives.ReadInt32LittleEndian(_original.AsSpan(offset, 4));

        target.Add(new ScalarEntry
        {
            Name = name,
            Type = type,
            SignedValue = value,
            IsUnsigned = false,
            Offset = offset,
            Width = width
        });
    }

    private void ParseStoreCatalog(EntityInfo entity)
    {
        int p = entity.DataOffset;
        int count = ReadInt32(_original, ref p, entity.EndOffset);
        if (count < 0 || count > 100_000)
            throw new InvalidDataException($"Invalid StoreCatalog count: {count}.");
        if (entity.Size != 4 + count * 4)
            throw new InvalidDataException("Unexpected StoreCatalogComponent size.");

        for (int i = 0; i < count; i++)
        {
            int offset = p;
            int value = ReadInt32(_original, ref p, entity.EndOffset);
            _storeCatalogEntries.Add(new StoreCatalogEntry { Index = i, Value = value, Offset = offset });
        }
    }

    public void Save(string path, bool makeBackup = true)
    {
        if (_floatEntries.Count != 16 || _stringEntries.Count != 100)
            throw new InvalidOperationException("The parsed PlayerProfile table counts changed unexpectedly.");

        using var ms = new MemoryStream(_original.Length + 4096);

        WriteInt32(ms, SaveVersion);
        ms.Write(new byte[8], 0, 8); // checksum placeholder
        WriteInt32(ms, ProfileVersion);

        foreach (var entity in _entities)
        {
            byte[] componentData = BuildEntityData(entity);

            WriteUInt32(ms, entity.Type);
            WriteInt32(ms, entity.Version);
            WriteInt32(ms, componentData.Length);
            ms.Write(componentData, 0, componentData.Length);
        }

        WriteInt32(ms, 0xFFFF);
        byte[] output = ms.ToArray();

        byte[] md5 = MD5.HashData(output.AsSpan(12));
        md5.AsSpan(0, 8).CopyTo(output.AsSpan(4, 8));
        ValidateSerializedOutput(output);

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException($"The save folder does not exist: {directory}");

        if (makeBackup && File.Exists(fullPath))
            File.Copy(fullPath, fullPath + ".bak", true);

        string temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var fs = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.Write(output, 0, output.Length);
                fs.Flush(flushToDisk: true);
            }

            File.Move(temp, fullPath, true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }

        // Re-read the bytes that actually reached disk and parse them again. A successful Save()
        // therefore means both the in-memory serializer and the final on-disk file passed validation.
        byte[] written = File.ReadAllBytes(fullPath);
        if (!written.AsSpan().SequenceEqual(output))
            throw new IOException("The save written to disk did not match the validated serialized data. Restore the .bak backup before using the file in-game.");

        FilePath = fullPath;
        _original = written;
        Parse();
    }

    private byte[] BuildEntityData(EntityInfo entity)
    {
        return entity.Type switch
        {
            EntityStatsStore => BuildStatsStoreBlock(),
            EntityRank => BuildScalarComponent(entity, _rankEntries),
            EntityPlayerProfile => BuildPlayerProfileComponent(entity),
            EntityFame => BuildScalarComponent(entity, _fameEntries),
            EntityStoreCatalog => BuildStoreCatalogBlock(),
            _ => _original.AsSpan(entity.DataOffset, entity.Size).ToArray()
        };
    }

    private byte[] BuildStatsStoreBlock()
    {
        using var ms = new MemoryStream();
        WriteUInt64(ms, (ulong)_statsStoreEntries.Count);

        foreach (var item in _statsStoreEntries)
        {
            WriteString4(ms, item.Name);
            WriteInt32(ms, item.Kind);

            ulong flags = item.Flags;
            flags = item.Value1.HasValue ? flags | 1UL : flags & ~1UL;
            flags = item.Value2.HasValue ? flags | 2UL : flags & ~2UL;
            flags = item.Value3.HasValue ? flags | 4UL : flags & ~4UL;
            flags = item.Value4.HasValue ? flags | 8UL : flags & ~8UL;
            flags = item.Value5.HasValue ? flags | 0x10UL : flags & ~0x10UL;
            WriteUInt64(ms, flags);

            if (item.Value1.HasValue) WriteUInt64(ms, item.Value1.Value);
            if (item.Value2.HasValue) WriteUInt32(ms, item.Value2.Value);
            if (item.Value3.HasValue) WriteUInt32(ms, item.Value3.Value);
            if (item.Value4.HasValue) WriteUInt32(ms, item.Value4.Value);
            if (item.Value5.HasValue) WriteUInt32(ms, item.Value5.Value);
        }

        return ms.ToArray();
    }

    private byte[] BuildScalarComponent(EntityInfo entity, IEnumerable<ScalarEntry> entries)
    {
        byte[] data = _original.AsSpan(entity.DataOffset, entity.Size).ToArray();

        foreach (var item in entries)
        {
            int relativeOffset = item.Offset - entity.DataOffset;
            if (relativeOffset < 0 || relativeOffset > data.Length - item.Width)
                throw new InvalidDataException($"Invalid offset for {item.Name}.");

            if (item.IsUnsigned)
            {
                if (item.Width != 4 || item.UnsignedValue > uint.MaxValue)
                    throw new InvalidDataException($"Invalid value for {item.Name}.");
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(relativeOffset, 4), (uint)item.UnsignedValue);
            }
            else if (item.Width == 8)
            {
                BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(relativeOffset, 8), item.SignedValue);
            }
            else if (item.Width == 4)
            {
                if (item.SignedValue < int.MinValue || item.SignedValue > int.MaxValue)
                    throw new InvalidDataException($"Invalid value for {item.Name}.");
                BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(relativeOffset, 4), (int)item.SignedValue);
            }
            else
            {
                throw new InvalidDataException($"Unsupported scalar width for {item.Name}.");
            }
        }

        return data;
    }

    private byte[] BuildPlayerProfileComponent(EntityInfo entity)
    {
        byte[] originalComponent = _original.AsSpan(entity.DataOffset, entity.Size).ToArray();

        int localFloatStart = _floatStart - entity.DataOffset;
        int localFloatEnd = _floatEnd - entity.DataOffset;
        int localStringStart = _stringStart - entity.DataOffset;
        int localStringEnd = _stringEnd - entity.DataOffset;

        if (localFloatStart < 0 || localFloatEnd < localFloatStart ||
            localStringStart < localFloatEnd || localStringEnd < localStringStart ||
            localStringEnd > originalComponent.Length)
            throw new InvalidDataException("Invalid PlayerProfile table boundaries.");

        byte[] floatBlock = BuildFloatBlock();
        if (floatBlock.Length != localFloatEnd - localFloatStart)
            throw new InvalidDataException("Profile float table changed size unexpectedly.");

        byte[] stringBlock = BuildStringBlock();

        using var ms = new MemoryStream(originalComponent.Length + stringBlock.Length - (localStringEnd - localStringStart));
        ms.Write(originalComponent, 0, localFloatStart);
        ms.Write(floatBlock, 0, floatBlock.Length);
        ms.Write(originalComponent, localFloatEnd, localStringStart - localFloatEnd);
        ms.Write(stringBlock, 0, stringBlock.Length);
        ms.Write(originalComponent, localStringEnd, originalComponent.Length - localStringEnd);
        return ms.ToArray();
    }

    private byte[] BuildStoreCatalogBlock()
    {
        using var ms = new MemoryStream();
        WriteInt32(ms, _storeCatalogEntries.Count);
        foreach (var item in _storeCatalogEntries)
            WriteInt32(ms, item.Value);
        return ms.ToArray();
    }

    private static void ValidateSerializedOutput(byte[] data)
    {
        if (data.Length < 20)
            throw new InvalidDataException("Serialized output is too small.");

        byte[] md5 = MD5.HashData(data.AsSpan(12));
        if (!data.AsSpan(4, 8).SequenceEqual(md5.AsSpan(0, 8)))
            throw new InvalidDataException("Internal checksum verification failed after serialization.");

        int pos = 16;
        for (int i = 0; i < ExpectedEntityOrder.Length; i++)
        {
            EnsureRemaining(data, pos, 12);
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(pos, 4));
            int size = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos + 8, 4));
            if (type != ExpectedEntityOrder[i])
                throw new InvalidDataException($"Serialized entity #{i + 1} has type 0x{type:X8}; expected 0x{ExpectedEntityOrder[i]:X8}.");
            if (size < 0)
                throw new InvalidDataException("Serialized entity has a negative size.");
            EnsureRemaining(data, pos + 12, size);
            pos += 12 + size;
        }

        EnsureRemaining(data, pos, 4);
        int end = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
        if (end != 0xFFFF || pos + 4 != data.Length)
            throw new InvalidDataException("Serialized Driveclub entity chain/end marker is invalid.");
    }

    public StringEntry? FindString(string name) =>
        _stringEntries.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

    public FloatEntry? FindFloat(string name) =>
        _floatEntries.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

    public StatsStoreEntry? FindStats(string name) =>
        _statsStoreEntries.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.Ordinal));

    private byte[] BuildFloatBlock()
    {
        using var ms = new MemoryStream();
        WriteInt32(ms, _floatEntries.Count);
        foreach (var item in _floatEntries)
        {
            WriteString4(ms, item.Name);
            Span<byte> raw = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(raw, BitConverter.SingleToInt32Bits(item.Value));
            ms.Write(raw);
        }
        return ms.ToArray();
    }

    private byte[] BuildStringBlock()
    {
        using var ms = new MemoryStream();
        WriteInt32(ms, _stringEntries.Count);
        foreach (var item in _stringEntries)
        {
            WriteString4(ms, item.Name);
            byte[] value = Encoding.UTF8.GetBytes(item.Value ?? "");
            WriteInt32(ms, value.Length);
            ms.Write(value, 0, value.Length);
        }
        return ms.ToArray();
    }

    private static byte[] BuildPrefixSignature(int count, string firstKey)
    {
        byte[] key = Encoding.UTF8.GetBytes(firstKey);
        byte[] sig = new byte[8 + key.Length];
        BinaryPrimitives.WriteInt32LittleEndian(sig.AsSpan(0, 4), count);
        BinaryPrimitives.WriteInt32LittleEndian(sig.AsSpan(4, 4), key.Length);
        key.CopyTo(sig, 8);
        return sig;
    }

    private static int Find(byte[] data, byte[] needle, int start, int endExclusive)
    {
        if (needle.Length == 0 || start < 0 || endExclusive > data.Length || start > endExclusive)
            return -1;
        if (needle.Length > endExclusive - start)
            return -1;

        for (int i = start; i <= endExclusive - needle.Length; i++)
        {
            if (data[i] != needle[0]) continue;
            bool ok = true;
            for (int j = 1; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j])
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return i;
        }
        return -1;
    }

    private static int ReadInt32(byte[] data, ref int pos)
    {
        EnsureRemaining(data, pos, 4);
        int value = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
        pos += 4;
        return value;
    }

    private static uint ReadUInt32(byte[] data, ref int pos)
    {
        EnsureRemaining(data, pos, 4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(pos, 4));
        pos += 4;
        return value;
    }

    private static ulong ReadUInt64(byte[] data, ref int pos)
    {
        EnsureRemaining(data, pos, 8);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(pos, 8));
        pos += 8;
        return value;
    }

    private static string ReadString4(byte[] data, ref int pos, int endExclusive)
    {
        int len = ReadInt32(data, ref pos, endExclusive);
        if (len < 0 || len > 1_000_000)
            throw new InvalidDataException($"Invalid string length: {len}.");
        if (pos > endExclusive - len)
            throw new EndOfStreamException("String exceeds component boundary.");
        string s = Encoding.UTF8.GetString(data, pos, len);
        pos += len;
        return s;
    }

    private static int ReadInt32(byte[] data, ref int pos, int endExclusive)
    {
        EnsureRemaining(data, pos, 4, endExclusive);
        int value = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4));
        pos += 4;
        return value;
    }

    private static uint ReadUInt32(byte[] data, ref int pos, int endExclusive)
    {
        EnsureRemaining(data, pos, 4, endExclusive);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(pos, 4));
        pos += 4;
        return value;
    }

    private static ulong ReadUInt64(byte[] data, ref int pos, int endExclusive)
    {
        EnsureRemaining(data, pos, 8, endExclusive);
        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(pos, 8));
        pos += 8;
        return value;
    }

    private static void WriteString4(Stream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteInt32(stream, bytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> raw = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(raw, value);
        stream.Write(raw);
    }

    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> raw = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(raw, value);
        stream.Write(raw);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> raw = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(raw, value);
        stream.Write(raw);
    }

    private static void EnsureRemaining(byte[] data, int pos, int count, int endExclusive)
    {
        if (endExclusive < 0 || endExclusive > data.Length || pos < 0 || count < 0 || pos > endExclusive - count)
            throw new EndOfStreamException("Unexpected end of Driveclub component data.");
    }

    private static void EnsureRemaining(byte[] data, int pos, int count)
    {
        if (pos < 0 || count < 0 || pos > data.Length - count)
            throw new EndOfStreamException("Unexpected end of Driveclub profile.sav.");
    }
}
