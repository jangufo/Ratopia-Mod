using RatopiaMod.YunQing.All.Configuration;

namespace RatopiaMod.YunQing.All.Localization
{
    internal static class CheatPanelTranslationProvider
    {
        public static CheatPanelTranslator Translator { get; } =
            new CheatPanelTranslator(new TranslationFileStore(PluginConstants.TranslationFilePath));
    }
}
