using System.IO;
using System.Text.Json;
using DeepL;

namespace WatchedAnimeList.Helpers
{
    public static class LocalizationHelper
    {
        private static string curLanguage = "en";
        private static string[] Languages = Array.Empty<string>();
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new();
        public static event EventHandler? OnLanguageChanged;

        public static void Initialize()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var localisationsDirectory = Path.Combine(baseDirectory, "Languages");

            var languagesFilesName = Directory.GetFiles(localisationsDirectory);
            if (languagesFilesName.Length == 0) Debug.Log("Не знайдено жодного файлу мови", NotificationType.Error);
            Languages = new string[languagesFilesName.Length];
            int i = 0;

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (string langFileName in languagesFilesName)
            {
                Languages[i++] = Path.GetFileNameWithoutExtension(langFileName);
                var json = File.ReadAllText(langFileName);
                var jsonTranslations = JsonSerializer.Deserialize<Dictionary<string, string>>(json, jsonOptions);
                if (jsonTranslations is null)
                    Debug.Ex("jsonTranslations is null");

                Translations[Path.GetFileNameWithoutExtension(langFileName)] = jsonTranslations;
            }
        }

        public static  string[] GetLanguages() => Languages;

        public static string GetTranslation(string keyWord)
        {
            if (Translations.TryGetValue(curLanguage, out var dict) && dict.TryGetValue(keyWord, out var value))
                return value;

            Debug.Log($"Не знайдено переклад для ключа: {keyWord}", NotificationType.Warning);
            return keyWord;
        }


        public static string CurrentLanguage => curLanguage;

        public static  void SetLanguage(string _lang)
        {
            if (!Languages.Contains(_lang))
            {
                Debug.Log($"Не знайдено мову: {_lang}", NotificationType.Warning);
                return;
            }

            curLanguage = _lang;
            OnLanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static async Task<string> TranslateText(string textToTranslate)
        {
            return await Translate(textToTranslate, curLanguage);
        }
        public static async Task<string> TranslateText(string textToTranslate, string targetLang)
        {
            return await Translate(textToTranslate, targetLang);
        }
        private static async Task<string> Translate(string textToTranslate, string targetLang)
        {
            var client = new Translator("49d710dd-2897-4129-b171-2ea0548043c8:fx");
            var translatedText = await client.TranslateTextAsync(
                textToTranslate,
                LanguageCode.EnglishAmerican,
                targetLang);
            return translatedText.ToString();
        }
    }
}
