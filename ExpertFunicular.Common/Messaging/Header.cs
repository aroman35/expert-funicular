using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExpertFunicular.Common.Messaging;

public readonly unsafe ref struct Header
{
    private const string CONTENT_LENGTH = "Content-Length";

    private Header(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {
        Key = key;
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Header Parse(Span<byte> utf8Raw)
    {
        // KEY=VALUE
        if (utf8Raw.IsEmpty)
            throw new InvalidOperationException("Header line is empty");
        var keyValueDelimiter = utf8Raw.IndexOf((byte)'=');
        if (keyValueDelimiter == -1)
            throw new InvalidOperationException("Header line is invalid");

        var keyLength = keyValueDelimiter;
        var valueLength = utf8Raw.Length - keyValueDelimiter - 1;
        scoped ref var keyUtf8 = ref utf8Raw[..keyValueDelimiter].GetPinnableReference();
        scoped ref var valueUtf8 = ref utf8Raw[(keyValueDelimiter + 1)..].GetPinnableReference();

        var keyReadonlySpanUtf8 = MemoryMarshal.CreateReadOnlySpan(in keyUtf8, keyLength);
        var valueReadonlySpanUtf8 = MemoryMarshal.CreateReadOnlySpan(in valueUtf8, valueLength);

        var keyArr = ArrayPool<char>.Shared.Rent(keyLength);
        var valueArr = ArrayPool<char>.Shared.Rent(valueLength);

        var keyBuffer = keyArr.AsSpan();
        var valueBuffer = valueArr.AsSpan();
        var keyDecodedLength = Encoding.UTF8.GetChars(keyReadonlySpanUtf8, keyBuffer);
        var valueDecodedLength = Encoding.UTF8.GetChars(valueReadonlySpanUtf8, valueBuffer);
        
        ArrayPool<char>.Shared.Return(keyArr, true);
        ArrayPool<char>.Shared.Return(valueArr, true);

        fixed (char* keyPtr = keyBuffer)
        {
            fixed (char* valuePtr = valueBuffer)
            {
                return new Header(
                    new ReadOnlySpan<char>(keyPtr, keyDecodedLength),
                    new ReadOnlySpan<char>(valuePtr, valueDecodedLength));
            }
        }
    }

    public static Header ContentLength(int length)
    {
        return new Header(CONTENT_LENGTH, length.ToString());
    }

    public ReadOnlySpan<char> Key { get; }
    public ReadOnlySpan<char> Value { get; }

    public override string ToString()
    {
        return Key.ToString() + '=' + Value.ToString();
    }
}