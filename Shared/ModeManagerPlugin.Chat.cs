using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private static readonly Regex _colorTokenRegex = new(@"\{([A-Za-z0-9_-]+)\}", RegexOptions.Compiled);

    private static readonly Dictionary<string, string> _chatColorTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = ChatColorReset,

        ["dark_red"] = "\u0002",
        ["darkred"] = "\u0002",
        ["red"] = "\u0007",
        ["light_red"] = "\u000F",
        ["lightred"] = "\u000F",
        ["orange"] = "\u0010",
        ["gold"] = "\u000A",
        ["yellow"] = "\u0009",

        ["green"] = "\u0004",
        ["light_green"] = "\u0006",
        ["lightgreen"] = "\u0006",
        ["olive"] = "\u0005",

        ["blue"] = "\u000C",
        ["light_blue"] = "\u000B",
        ["lightblue"] = "\u000B",
        ["cyan"] = "\u000B",
        ["dark_cyan"] = "\u0003",
        ["darkcyan"] = "\u0003",
        ["teal"] = "\u0003",

        ["purple"] = "\u000D",
        ["pink"] = "\u000E",
        ["grey"] = "\u0008",
        ["gray"] = "\u0008",
        ["team"] = "\u0003"
    };

    private const string ChatColorReset = "\u0001";
    private const string DefaultChatPrefix = "[Mode]";

    private string ResolveChatPrefix()
    {
        var configuredPrefix = Msg(MessageKey.ChatPrefix);
        if (string.IsNullOrWhiteSpace(configuredPrefix) ||
            string.Equals(configuredPrefix, nameof(MessageKey.ChatPrefix), StringComparison.Ordinal))
            return DefaultChatPrefix;

        return configuredPrefix.Trim();
    }

    private string PrefixMessageForChat(string message)
    {
        var prefix = ReplaceColorTokens(ResolveChatPrefix(), out var hasTokenColors);
        if (hasTokenColors && !prefix.EndsWith(ChatColorReset, StringComparison.Ordinal))
            prefix += ChatColorReset;
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = DefaultChatPrefix;

        return string.IsNullOrWhiteSpace(message)
            ? prefix
            : $"{prefix} {message}";
    }

    private string PrefixMessageForConsole(string message)
    {
        var prefix = StripColorTokens(ResolveChatPrefix());
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = DefaultChatPrefix;

        var plainMessage = StripChatControlCodes(StripColorTokens(message));
        return string.IsNullOrWhiteSpace(plainMessage)
            ? prefix
            : $"{prefix} {plainMessage}";
    }

    private string MsgForChat(MessageKey key, params object?[] args) =>
        ColorizeForChat(Msg(key, args));

    private void ReplyTone(CommandInfo cmd, string message)
    {
        var payload = IsChatContext(cmd)
            ? PrefixMessageForChat(ColorizeForChat(message))
            : PrefixMessageForConsole(message);

        cmd.ReplyToCommand(payload);
    }

    private void ReplyTone(CommandInfo cmd, MessageKey key, params object?[] args)
    {
        var message = Msg(key, args);
        ReplyTone(cmd, message);
    }

    private void TellTone(CCSPlayerController player, string message) =>
        player.PrintToChat(PrefixMessageForChat(ColorizeForChat(message)));

    private void TellTone(CCSPlayerController player, MessageKey key, params object?[] args) =>
        player.PrintToChat(PrefixMessageForChat(MsgForChat(key, args)));

    private void ChatTone(MessageKey key, params object?[] args) =>
        ChatAll(MsgForChat(key, args));

    private static string ColorizeForChat(string message)
    {
        var tokenized = ReplaceColorTokens(message, out var hasTokenColors);

        if (hasTokenColors && !tokenized.EndsWith(ChatColorReset, StringComparison.Ordinal))
            tokenized += ChatColorReset;

        return tokenized;
    }

    private static string StripColorTokens(string message) =>
        _colorTokenRegex.Replace(message, match =>
        {
            var token = NormalizeColorToken(match.Groups[1].Value);
            return _chatColorTokens.ContainsKey(token) ? string.Empty : match.Value;
        });

    private static string ReplaceColorTokens(string message, out bool hasTokenColors)
    {
        var foundTokenColor = false;

        var replaced = _colorTokenRegex.Replace(message, match =>
        {
            var token = NormalizeColorToken(match.Groups[1].Value);
            if (_chatColorTokens.TryGetValue(token, out var colorCode))
            {
                foundTokenColor = true;
                return colorCode;
            }

            return match.Value;
        });

        hasTokenColors = foundTokenColor;
        return replaced;
    }

    private static string NormalizeColorToken(string token) =>
        (token ?? string.Empty).Trim().Replace("-", "_");

    private static bool IsChatContext(CommandInfo cmd)
    {
        try
        {
            var context = cmd.CallingContext.ToString();
            return !string.IsNullOrWhiteSpace(context) &&
                   context.Contains("chat", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }
}
