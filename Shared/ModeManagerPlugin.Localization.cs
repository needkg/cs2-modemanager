namespace ModeManager;

public sealed partial class ModeManagerPlugin
{
    private MessageLocalizer _messages = MessageLocalizer.Create("en");

    private string Msg(MessageKey key) => _messages.Get(key);

    private string Msg(MessageKey key, params object?[] args) => _messages.Format(key, args);

    private void ApplyLanguage(string? languageCode)
    {
        _messages = MessageLocalizer.Create(languageCode);
        LogInfo(Msg(MessageKey.LogLanguageSet, _messages.LanguageCode));
    }
}
