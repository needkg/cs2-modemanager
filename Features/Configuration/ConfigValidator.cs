using System;
using System.Collections.Generic;

namespace ModeManager;

internal static class ConfigValidator
{
    public static void ValidateOrThrow(ModeManagerConfig config)
    {
        var messages = MessageLocalizer.Create(config.Language);

        if (!MessageLocalizer.IsSupported(config.Language))
        {
            throw new Exception(messages.Format(
                MessageKey.ValidationLanguageUnsupported,
                config.Language ?? string.Empty,
                string.Join(", ", MessageLocalizer.SupportedLanguages)));
        }

        if (config.Modes == null || config.Modes.Count == 0)
            throw new Exception(messages.Get(MessageKey.ValidationModesRequired));

        if (string.IsNullOrWhiteSpace(config.ResetCommand))
            throw new Exception(messages.Get(MessageKey.ValidationResetCommandRequired));

        if (config.VoteRatio <= 0 || config.VoteRatio > 1.0)
            throw new Exception(messages.Get(MessageKey.ValidationVoteRatioRange));

        if (config.VoteMinPlayers < 1)
            throw new Exception(messages.Get(MessageKey.ValidationVoteMinPlayersRange));

        if (config.VoteDurationSeconds < 5 || config.VoteDurationSeconds > 300)
            throw new Exception(messages.Get(MessageKey.ValidationVoteDurationRange));

        if (config.SwitchCooldownSeconds < 0 || config.SwitchCooldownSeconds > 600)
            throw new Exception(messages.Get(MessageKey.ValidationSwitchCooldownRange));

        if (config.SwitchDelaySeconds < 0 || config.SwitchDelaySeconds > 600)
            throw new Exception(messages.Get(MessageKey.ValidationSwitchDelayRange));

        foreach (KeyValuePair<string, ModeDefinition> entry in config.Modes)
        {
            var modeKey = entry.Key;
            var mode = entry.Value;

            if (mode == null)
                throw new Exception(messages.Format(MessageKey.ValidationModeNull, modeKey));

            if (string.IsNullOrWhiteSpace(mode.Key))
                mode.Key = modeKey;

            if (string.IsNullOrWhiteSpace(mode.DisplayName))
                mode.DisplayName = mode.Key;

            if (string.IsNullOrWhiteSpace(mode.ExecCommand))
                throw new Exception(messages.Format(MessageKey.ValidationExecCommandRequired, modeKey));

            if (!string.IsNullOrWhiteSpace(mode.DefaultMap))
            {
                var map = mode.DefaultMap.Trim();
                if (map.Contains(' ') || map.Contains('"'))
                    throw new Exception(messages.Format(MessageKey.ValidationDefaultMapInvalid, modeKey, map));
            }

            if (mode.GameType is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameTypeInvalid, modeKey, mode.GameType));

            if (mode.GameMode is < 0 or > 20)
                throw new Exception(messages.Format(MessageKey.ValidationGameModeInvalid, modeKey, mode.GameMode));
        }
    }
}
