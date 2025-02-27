using CommunityToolkit.Diagnostics;

using U8.InteropServices;

namespace Warpskimmer;

public record Message(
    Tag[]? Tags,
    Prefix? Prefix,
    Command Command,
    U8String? Channel,
    U8String? Parameters)
{
    public static Message Parse(U8String line)
    {
        line = line.StripSuffix("\r\n"u8);
        Guard.IsGreaterThan(line.Length, 0);

        var tags = Tag.ParseAll(ref line);
        var prefix = Prefix.Parse(ref line);
        var command = Command.Parse(ref line);
        var channel = ParseChannel(ref line);
        var parameters = line switch
        {
            [(byte)':', ..] => U8Marshal.SliceUnsafe(line, 1),
            { Length: > 0 } => line,
            _ => default(U8String?)
        };

        return new Message(tags, prefix, command, channel, parameters);
    }

    private static U8String? ParseChannel(ref U8String line)
    {
        var deref = line;
        if (deref is [(byte)'#', ..])
        {
            (var channel, line) = U8Marshal
                .SliceUnsafe(deref, 1)
                .SplitFirst((byte)' ');

            return channel;
        }

        return null;
    }
}
