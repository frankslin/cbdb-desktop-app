namespace Cbdb.App.Core;

public interface ILocalizationService {
    UiLanguage CurrentLanguage { get; }
    event EventHandler? LanguageChanged;

    void SetLanguage(UiLanguage language);
    string Get(string key);
}
