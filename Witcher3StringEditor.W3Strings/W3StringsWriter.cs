using Witcher3StringEditor.W3Strings.Model;
using Witcher3StringEditor.W3Strings.Primitives;

namespace Witcher3StringEditor.W3Strings;

public static class W3StringsWriter
{
    private static byte[] Write(W3StringsFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var payloads = ResolvePayloads(file, out var lengths);
        var offsets = ResolveOffsets(file, lengths);
        var buffer = BuildStringBuffer(file, payloads, lengths, offsets);
        return AssembleContainer(file, lengths, offsets, buffer);
    }

    public static void WriteFile(W3StringsFile file, string path)
    {
        File.WriteAllBytes(path, Write(file));
    }

    private static byte[][] ResolvePayloads(W3StringsFile file, out int[] lengths)
    {
        var magic = file.Magic;
        var unit = file.Unit;
        var payloads = new byte[file.Strings.Count][];
        lengths = new int[file.Strings.Count];
        for (var i = 0; i < file.Strings.Count; i++)
            payloads[i] = PayloadFor(file.Strings[i], magic, unit, out lengths[i]);

        return payloads;
    }

    private static uint[] ResolveOffsets(W3StringsFile file, int[] lengths)
    {
        var offsets = new uint[file.Strings.Count];
        var parsedLayout = file.Strings.Count > 0 &&
                           file.Strings.All(s => s.StoredBytes is not null);

        if (parsedLayout)
        {
            for (var i = 0; i < file.Strings.Count; i++) offsets[i] = file.Strings[i].Offset;
            return offsets;
        }

        uint cursor = 0;
        for (var i = 0; i < file.Strings.Count; i++)
        {
            offsets[i] = cursor;
            cursor += (uint)lengths[i] + 1;
        }

        return offsets;
    }

    private static byte[] BuildStringBuffer(W3StringsFile file, byte[][] payloads, int[] lengths, uint[] offsets)
    {
        var unit = file.Unit;
        long bufferEnd = 0;
        for (var i = 0; i < file.Strings.Count; i++)
            bufferEnd = Math.Max(bufferEnd, (offsets[i] + lengths[i] + 1) * unit);

        var declared = file.DeclaredBufferUnits * unit;
        var buffer = new byte[Math.Max(bufferEnd, declared)];
        for (var i = 0; i < file.Strings.Count; i++)
        {
            var at = (int)(offsets[i] * (uint)unit);
            payloads[i].CopyTo(buffer, at);
            // terminator is already zero-filled, but be explicit
            for (var k = 0; k < unit; k++) buffer[at + payloads[i].Length + k] = 0;
        }

        return buffer;
    }

    private static byte[] AssembleContainer(W3StringsFile file, int[] lengths, uint[] offsets, byte[] buffer)
    {
        var magic = file.Magic;
        var count3 = (uint)(buffer.Length / file.Unit);

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(W3StringsFormat.MagicBytes);
        w.Write(file.Version);
        w.Write(file.Key1);

        WriteCount(w, (uint)file.Strings.Count);
        for (var i = 0; i < file.Strings.Count; i++)
        {
            w.Write(file.Strings[i].Id ^ magic);
            w.Write(offsets[i]);
            w.Write((uint)lengths[i]);
        }

        WriteCount(w, (uint)file.Keys.Count);
        foreach (var k in file.Keys)
        {
            w.Write(k.KeyHash);
            w.Write(k.Id ^ magic);
        }

        WriteCount(w, count3);
        w.Write(buffer);
        if (file.Trailer.Length > 0) w.Write(file.Trailer);
        w.Write(file.Key2);

        w.Flush();
        return ms.ToArray();
    }

    private static byte[] PayloadFor(W3StringEntry s, uint magic, int unit, out int length)
    {
        var untouched = s.OriginalValue is null || s.Value == s.OriginalValue;

        if (!untouched || s.PlainBytes is null || s.PlainBytes.Length % unit != 0)
            return PayloadCodec.Encode(s.Value, magic, unit, out length);
        length = s.PlainBytes.Length / unit;
        if (s.Length != 0 && s.Length != length) return PayloadCodec.Encode(s.Value, magic, unit, out length);
        var stored = (byte[])s.PlainBytes.Clone();
        if (unit == 2) PayloadCodec.XorUtf16(stored, length, magic);
        else PayloadCodec.XorUtf8(stored, length, magic);
        return stored;
    }

    private static void WriteCount(BinaryWriter w, uint value)
    {
        w.Write(VariableLengthCodec.Write(value));
    }
}