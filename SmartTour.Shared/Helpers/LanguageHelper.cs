using System.Collections.Generic;
using System.Linq;

namespace SmartTour.Shared.Helpers;

public class LanguageItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;

    public string DisplayName => $"{Flag} {Name} ({NativeName})";
}

public static class LanguageHelper
{
    public static List<LanguageItem> GetAvailableLanguages()
    {
        return new List<LanguageItem>
        {
            // Châu Á
            new() { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", Flag = "🇻🇳" },
            new() { Code = "en", Name = "English", NativeName = "English", Flag = "🇺🇸" },
            new() { Code = "zh", Name = "Chinese", NativeName = "中文 (简体)", Flag = "🇨🇳" },
            new() { Code = "ja", Name = "Japanese", NativeName = "日本語", Flag = "🇯🇵" },
            new() { Code = "ko", Name = "Korean", NativeName = "한국어", Flag = "🇰🇷" },
            new() { Code = "th", Name = "Thai", NativeName = "ไทย", Flag = "🇹🇭" },
            new() { Code = "lo", Name = "Lao", NativeName = "ລາວ", Flag = "🇱🇦" },
            new() { Code = "km", Name = "Khmer", NativeName = "ខ្មែર", Flag = "🇰🇭" },
            new() { Code = "ms", Name = "Malay", NativeName = "Bahasa Melayu", Flag = "🇲🇾" },
            new() { Code = "id", Name = "Indonesian", NativeName = "Bahasa Indonesia", Flag = "🇮🇩" },
            new() { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", Flag = "🇮🇳" },
            new() { Code = "bn", Name = "Bengali", NativeName = "বাংলা", Flag = "🇧🇩" },
            new() { Code = "tl", Name = "Filipino", NativeName = "Tagalog", Flag = "🇵🇭" },
            
            // Châu Âu
            new() { Code = "fr", Name = "French", NativeName = "Français", Flag = "🇫🇷" },
            new() { Code = "de", Name = "German", NativeName = "Deutsch", Flag = "🇩🇪" },
            new() { Code = "ru", Name = "Russian", NativeName = "Русский", Flag = "🇷🇺" },
            new() { Code = "es", Name = "Spanish", NativeName = "Español", Flag = "🇪🇸" },
            new() { Code = "it", Name = "Italian", NativeName = "Italiano", Flag = "🇮🇹" },
            new() { Code = "pt", Name = "Portuguese", NativeName = "Português", Flag = "🇵🇹" },
            new() { Code = "nl", Name = "Dutch", NativeName = "Nederlands", Flag = "🇳🇱" },
            new() { Code = "sv", Name = "Swedish", NativeName = "Svenska", Flag = "🇸🇪" },
            new() { Code = "no", Name = "Norwegian", NativeName = "Norsk", Flag = "��" },
            new() { Code = "da", Name = "Danish", NativeName = "Dansk", Flag = "🇩🇰" },
            new() { Code = "fi", Name = "Finnish", NativeName = "Suomi", Flag = "🇫🇮" },
            new() { Code = "pl", Name = "Polish", NativeName = "Polski", Flag = "��🇱" },
            new() { Code = "uk", Name = "Ukrainian", NativeName = "Українська", Flag = "🇺🇦" },
            new() { Code = "cs", Name = "Czech", NativeName = "Čeština", Flag = "🇨🇿" },
            new() { Code = "tr", Name = "Turkish", NativeName = "Türkçe", Flag = "🇹🇷" },
            new() { Code = "el", Name = "Greek", NativeName = "Ελληνικά", Flag = "🇬🇷" },

            // Trung Đông & Châu Phi
            new() { Code = "ar", Name = "Arabic", NativeName = "العربية", Flag = "🇸🇦" },
            new() { Code = "he", Name = "Hebrew", NativeName = "עברית", Flag = "🇮🇱" },
            new() { Code = "fa", Name = "Persian", NativeName = "فارسی", Flag = "🇮🇷" },
            new() { Code = "sw", Name = "Swahili", NativeName = "Kiswahili", Flag = "🇰🇪" },
            new() { Code = "af", Name = "Afrikaans", NativeName = "Afrikaans", Flag = "��" }
        };
    }

    public static string GetFlagByCode(string code)
    {
        var lang = GetAvailableLanguages().FirstOrDefault(l => l.Code.Equals(code, System.StringComparison.InvariantCultureIgnoreCase));
        return lang?.Flag ?? "🌐";
    }
}
