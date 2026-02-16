using System;

namespace ModeManager;

internal static class ConfigValidator
{
    public static void ValidateOrThrow(ModeManagerConfig config)
    {
        var messages = MessageLocalizer.Create(config.Language);

        ValidateLanguageOrThrow(config, messages);
        ConfigRulesValidator.ValidateOrThrow(config, messages);
        ConfigEnvironmentValidator.ValidateOrThrow(config, messages);
    }

    private static void ValidateLanguageOrThrow(ModeManagerConfig config, MessageLocalizer messages)
    {
        if (MessageLocalizer.IsSupported(config.Language))
            return;

        throw new Exception(messages.Format(
            MessageKey.ValidationLanguageUnsupported,
            config.Language ?? string.Empty,
            string.Join(", ", MessageLocalizer.SupportedLanguages)));
    }
}
