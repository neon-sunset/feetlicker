﻿using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using U8.InteropServices;

namespace Warpskimmer;

public readonly record struct Command(
    CommandKey Key,
    U8String Value)
{
    private static ReadOnlySpan<byte> PRIVMSG => "PRIVMSG"u8;
    private static ReadOnlySpan<byte> CLEARCHAT => "CLEARCHAT"u8;

    public static readonly Command Ping =            new(CommandKey.Ping,            u8("PING"));
    public static readonly Command Pong =            new(CommandKey.Pong,            u8("PONG"));
    public static readonly Command Join =            new(CommandKey.Join,            u8("JOIN"));
    public static readonly Command Part =            new(CommandKey.Part,            u8("PART"));
    public static readonly Command Privmsg =         new(CommandKey.Privmsg,         u8("PRIVMSG"));
    public static readonly Command Whisper =         new(CommandKey.Whisper,         u8("WHISPER"));
    public static readonly Command Clearchat =       new(CommandKey.Clearchat,       u8("CLEARCHAT"));
    public static readonly Command Clearmsg =        new(CommandKey.Clearmsg,        u8("CLEARMSG"));
    public static readonly Command GlobalUserState = new(CommandKey.GlobalUserState, u8("GLOBALUSERSTATE"));
    public static readonly Command HostTarget =      new(CommandKey.HostTarget,      u8("HOSTTARGET"));
    public static readonly Command Notice =          new(CommandKey.Notice,          u8("NOTICE"));
    public static readonly Command Reconnect =       new(CommandKey.Reconnect,       u8("RECONNECT"));
    public static readonly Command RoomState =       new(CommandKey.RoomState,       u8("ROOMSTATE"));
    public static readonly Command UserNotice =      new(CommandKey.UserNotice,      u8("USERNOTICE"));
    public static readonly Command UserState =       new(CommandKey.UserState,       u8("USERSTATE"));
    public static readonly Command Capability =      new(CommandKey.Capability,      u8("CAP"));
    public static readonly Command RplWelcome =      new(CommandKey.RplWelcome,      u8("001"));
    public static readonly Command RplYourHost =     new(CommandKey.RplYourHost,     u8("002"));
    public static readonly Command RplCreated =      new(CommandKey.RplCreated,      u8("003"));
    public static readonly Command RplMyInfo =       new(CommandKey.RplMyInfo,       u8("004"));
    public static readonly Command RplNamReply =     new(CommandKey.RplNamReply,     u8("353"));
    public static readonly Command RplEndOfNames =   new(CommandKey.RplEndOfNames,   u8("366"));
    public static readonly Command RplMotd =         new(CommandKey.RplMotd,         u8("372"));
    public static readonly Command RplMotdStart =    new(CommandKey.RplMotdStart,    u8("375"));
    public static readonly Command RplEndOfMotd =    new(CommandKey.RplEndOfMotd,    u8("376"));

    public static Command Parse(ref U8String source)
    {
        var deref = source;
        var command =
            deref.StartsWith(PRIVMSG) ? Privmsg :
            deref.StartsWith(CLEARCHAT) ? Clearchat :
            ParseSlow(deref);

        source = TrimEnd(deref, command.Value.Length);
        return command;
    }

    public static Command ParseSlow(U8String source)
    {
        var rest = source.AsSpan(1);
        return source[0] switch
        {
            (byte)'P' => rest switch
            {
                _ when rest.StartsWith("ING"U8) => Ping,
                _ when rest.StartsWith("ONG"U8) => Pong,
                _ when rest.StartsWith("ART"U8) => Part,
                _ => Unknown(source)
            },
            (byte)'J' when rest.StartsWith("OIN"u8) => Join,
            (byte)'W' when rest.StartsWith("HISPER"u8) => Whisper,
            (byte)'C' => rest switch
            {
                _ when rest.StartsWith("LEARMSG"u8) => Clearmsg,
                _ when rest.StartsWith("AP"u8) => Capability,
                _ => Unknown(source)
            },
            (byte)'G' when rest.StartsWith("LOBALUSERSTATE"u8) => GlobalUserState,
            (byte)'H' when rest.StartsWith("OSTTARGET"u8) => HostTarget,
            (byte)'N' when rest.StartsWith("OTICE"u8) => Notice,
            (byte)'R' => rest switch
            {
                _ when rest.StartsWith("ECONNECT"u8) => Reconnect,
                _ when rest.StartsWith("OOMSTATE"u8) => RoomState,
                _ => Unknown(source)
            },
            (byte)'U' => rest switch
            {
                _ when rest.StartsWith("SERNOTICE"u8) => UserNotice,
                _ when rest.StartsWith("SERSTATE"u8) => UserState,
                _ => Unknown(source)
            },
            (byte)'0' => rest switch
            {
                _ when rest.StartsWith("01"u8) => RplWelcome,
                _ when rest.StartsWith("02"u8) => RplYourHost,
                _ when rest.StartsWith("03"u8) => RplCreated,
                _ when rest.StartsWith("04"u8) => RplMyInfo,
                _ => Unknown(source)
            },
            (byte)'3' => rest switch
            {
                _ when rest.StartsWith("53"u8) => RplNamReply,
                _ when rest.StartsWith("66"u8) => RplEndOfNames,
                _ when rest.StartsWith("72"u8) => RplMotd,
                _ when rest.StartsWith("75"u8) => RplMotdStart,
                _ when rest.StartsWith("76"u8) => RplEndOfMotd,
                _ => Unknown(source)
            },
            _ => Unknown(source)
        };
    }

    public static Command Unknown(U8String source)
    {
        return new(CommandKey.Unknown, source.SplitFirst((byte)' ').Segment);
    }

    public bool Equals(Command value)
    {
        return Key != CommandKey.Unknown ? Key == value.Key : Value == value.Value;
    }

    public override int GetHashCode()
    {
        return Key != CommandKey.Unknown ? Key.GetHashCode() : Value.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static U8String TrimEnd(U8String source, int length)
    {
        if (source.Length > length)
        {
            if (source[length] == (byte)' ')
            {
                return U8Marshal.SliceUnsafe(source, length + 1);
            }

            ThrowHelper.ThrowFormatException();
        }

        return source;
    }
}

public enum CommandKey
{
    Unknown,
    Ping,
    Pong,
    /// <summary>
    /// Join channel
    /// </summary>
    Join,
    /// <summary>
    /// Leave channel
    /// </summary>
    Part,
    /// <summary>
    /// Twitch Private Message
    /// </summary>
    Privmsg,
    // Twitch extensions
    /// <summary>
    /// Send message to a single user
    /// </summary>
    Whisper,
    /// <summary>
    /// Purge a user's messages
    /// </summary>
    Clearchat,
    /// <summary>
    /// Single message removal
    /// </summary>
    Clearmsg,
    /// <summary>
    /// Sent upon successful authentication (PASS/NICK command)
    /// </summary>
    GlobalUserState,
    /// <summary>
    /// Channel starts or stops host mode
    /// </summary>
    HostTarget,
    /// <summary>
    /// General notices from the server
    /// </summary>
    Notice,
    /// <summary>
    /// Rejoins channels after a restart
    /// </summary>
    Reconnect,
    /// <summary>
    /// Identifies the channel's chat settings
    /// </summary>
    RoomState,
    /// <summary>
    /// Announces Twitch-specific events to the channel
    /// </summary>
    UserNotice,
    /// <summary>
    /// Identifies a user's chat settings or properties
    /// </summary>
    UserState,
    /// <summary>
    /// Requesting an IRC capability
    /// </summary>
    Capability,
    // Numeric commands
    RplWelcome,
    RplYourHost,
    RplCreated,
    RplMyInfo,
    RplNamReply,
    RplEndOfNames,
    RplMotd,
    RplMotdStart,
    RplEndOfMotd,
}
