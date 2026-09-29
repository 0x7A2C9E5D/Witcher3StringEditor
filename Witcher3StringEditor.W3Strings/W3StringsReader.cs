using System.Buffers.Binary;
using System.Text;
using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

public static class W3StringsReader
{
    private static W3StringsFile Read(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 16)
            throw new W3StringsException("file too small to be a w3strings container");

        for (var i = 0; i < 4; i++)
            if (data[i] != W3StringsFormat.MagicBytes[i])
                throw new W3StringsException(
                    $"bad magic: expected \"RTSW\", got \"{Encoding.ASCII.GetString(data, 0, 4)}\"");

        var file = new W3StringsFile
        {
            Version = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)),
            Key1 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(W3StringsFormat.Key1Offset)),
            Key2 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(data.Length - 2))
        };
        var unit = file.Unit;

        // ---- section counts ----
        var p = W3StringsFormat.FirstCountOffset;
        (var count1, p) = VariableLengthCodec.Read(data, p);
        var block1Start = p;
        var block1End = block1Start + (int)count1 * W3StringsFormat.Block1EntrySize;

        (var count2, p) = VariableLengthCodec.Read(data, block1End);
        var block2Start = p;
        var block2End = block2Start + (int)count2 * W3StringsFormat.Block2EntrySize;

        (var count3, p) = VariableLengthCodec.Read(data, block2End);
        var bufferStart = p;
        var bufferEndLong = bufferStart + count3 * unit;
        if (bufferEndLong > data.Length)
            throw new W3StringsException(
                $"string buffer overruns file ({bufferEndLong} > {data.Length})");
        var bufferEnd = (int)bufferEndLong;

        file.DeclaredBufferUnits = count3;
        var trailerLength = data.Length - 2 - bufferEnd;
        file.Trailer = trailerLength > 0
            ? [.. data.AsSpan(bufferEnd, trailerLength)]
            : [];

        // ---- resolve language / magic ----
        ResolveLanguage(file);

        // ---- block 1 ----
        for (var i = 0; i < count1; i++)
        {
            var o = block1Start + i * W3StringsFormat.Block1EntrySize;
            var storedId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(o));
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(o + 4));
            var length = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(o + 8));

            var abs = bufferStart + (int)(offset * (uint)unit);
            var byteLength = (int)(length * (uint)unit);
            var stored = data.AsSpan(abs, byteLength).ToArray();

            // Decode into a separate buffer so the stored (obfuscated) bytes are
            // retained as well; both are needed for a lossless round-trip.
            var plain = (byte[])stored.Clone();
            var value = PayloadCodec.Decode(plain, (int)length, file.Magic, unit);

            file.Strings.Add(new W3StringEntry
            {
                Id = storedId ^ file.Magic,
                Offset = offset,
                Length = length,
                Value = value,
                StoredBytes = stored,
                PlainBytes = plain,
                OriginalValue = value
            });
        }

        // ---- block 2 ----
        for (var i = 0; i < count2; i++)
        {
            var o = block2Start + i * W3StringsFormat.Block2EntrySize;
            file.Keys.Add(new W3KeyEntry
            {
                KeyHash = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(o)),
                Id = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(o + 4)) ^ file.Magic
            });
        }

        return file;
    }

    public static W3StringsFile ReadFile(string path)
    {
        return Read(File.ReadAllBytes(path));
    }

    private static void ResolveLanguage(W3StringsFile file)
    {
        // Full 32-bit key first.
        var language = W3Language.FromKey(file.Key);
        // key1 alone is reliable when tooling left a foreign key2 behind.
        language ??= W3Language.FromKey1(file.Key1);

        if (language is { } resolved)
        {
            file.Language = resolved;
            file.Magic = resolved.Magic;
            return;
        }

        file.Language = null;
        file.Magic = 0;
    }
}