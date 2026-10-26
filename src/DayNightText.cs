using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using MediaPortal.GUI.Library;

namespace Backdrop.DayNight
{
    public static class DayNightText
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Languages = Load();
        public static string CultureCode
        {
            get
            {
                try
                {
                    string code = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
                    if (!String.IsNullOrEmpty(code)) return code;
                }
                catch { }
                return CultureInfo.CurrentUICulture.Name;
            }
        }
        public static string Get(string key) { return Get(key, CultureCode); }
        public static string Get(string key, string culture)
        {
            string code = (culture ?? "en").ToLowerInvariant();
            Dictionary<string, string> language;
            string value;
            if (!Languages.TryGetValue(code, out language))
            {
                code = code.Split('-')[0];
                if (code == "nb" || code == "nn") code = "no";
                if (code == "zh") code = "zh-cn";
                if (code == "cy") code = "cy-gb";
                if (!Languages.TryGetValue(code, out language)) language = Languages["en"];
            }
            return language.TryGetValue(key, out value) ? value : Languages["en"][key];
        }
        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var doc = new XmlDocument { XmlResolver = null };
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DayNight.Languages.xml")) doc.Load(stream);
            foreach (XmlElement language in doc.SelectNodes("/languages/language"))
            {
                var entries = new Dictionary<string, string>();
                foreach (XmlElement entry in language.SelectNodes("text")) entries.Add(entry.GetAttribute("key"), entry.InnerText);
                result.Add(language.GetAttribute("code"), entries);
            }
            return result;
        }
    }
}
