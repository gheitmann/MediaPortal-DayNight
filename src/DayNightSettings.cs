using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Backdrop.DayNight
{
    // Keep existing numeric values stable for old settings and integrations.
    public enum BackgroundMode { Automatic, Day, Night, Disabled, Morning, Noon }

    public sealed class DayNightSettings
    {
        public BackgroundMode Mode = BackgroundMode.Automatic;
        public TimeSpan MorningStarts = TimeSpan.FromHours(6);
        public TimeSpan NoonStarts = TimeSpan.FromHours(12);
        public TimeSpan DayStarts = TimeSpan.FromHours(15);
        public TimeSpan NightStarts = TimeSpan.FromHours(19);

        public string Period(DateTime localTime)
        {
            if (Mode == BackgroundMode.Day) return "day";
            if (Mode == BackgroundMode.Morning) return "morning";
            if (Mode == BackgroundMode.Noon) return "noon";
            if (Mode != BackgroundMode.Automatic) return "night";
            try { Validate(); } catch (InvalidDataException) { return "night"; }
            TimeSpan[] starts = { MorningStarts, NoonStarts, DayStarts, NightStarts };
            string[] periods = { "morning", "noon", "day", "night" };
            double elapsed = Double.MaxValue;
            string period = "night";
            for (int i = 0; i < starts.Length; i++)
            {
                double difference = (localTime.TimeOfDay.TotalMinutes - starts[i].TotalMinutes + 1440) % 1440;
                if (difference < elapsed) { elapsed = difference; period = periods[i]; }
            }
            return period;
        }

        public DayNightSettings Copy() { return (DayNightSettings)MemberwiseClone(); }

        public void Validate()
        {
            if (!Enum.IsDefined(typeof(BackgroundMode), Mode)) throw new InvalidDataException("Invalid mode.");
            TimeSpan[] starts = { MorningStarts, NoonStarts, DayStarts, NightStarts };
            double total = 0;
            for (int i = 0; i < starts.Length; i++)
            {
                if (starts[i] < TimeSpan.Zero || starts[i] >= TimeSpan.FromDays(1) || starts[i].Ticks % TimeSpan.TicksPerMinute != 0)
                    throw new InvalidDataException("Use minute-precision times within one day.");
                double difference = (starts[(i + 1) % 4].TotalMinutes - starts[i].TotalMinutes + 1440) % 1440;
                if (difference == 0) throw new InvalidDataException("Start times must be different.");
                total += difference;
            }
            if (total != 1440) throw new InvalidDataException("Use cyclic order: morning, noon, day, night.");
        }

        internal static XmlDocument ReadXml(string path)
        {
            var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            var doc = new XmlDocument { XmlResolver = null };
            using (var reader = XmlReader.Create(path, options)) doc.Load(reader);
            return doc;
        }

        public static DayNightSettings Load(string path)
        {
            if (!File.Exists(path)) return new DayNightSettings();
            var doc = ReadXml(path);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "dayNight" || (root.GetAttribute("version") != "1" && root.GetAttribute("version") != "2"))
                throw new InvalidDataException("Unsupported Day/Night settings format.");
            BackgroundMode mode;
            TimeSpan day, night;
            if (!Enum.TryParse(root.GetAttribute("mode"), true, out mode) || !Enum.IsDefined(typeof(BackgroundMode), mode)
                || !TimeSpan.TryParseExact(root.GetAttribute("dayStarts"), @"hh\:mm", CultureInfo.InvariantCulture, out day)
                || !TimeSpan.TryParseExact(root.GetAttribute("nightStarts"), @"hh\:mm", CultureInfo.InvariantCulture, out night)
                || day >= TimeSpan.FromDays(1) || night >= TimeSpan.FromDays(1) || day == night)
                throw new InvalidDataException("Invalid Day/Night mode or times.");
            var result = new DayNightSettings { Mode = mode, DayStarts = day, NightStarts = night };
            if (root.GetAttribute("version") == "1")
            {
                // Preserve the old bright interval's endpoints when adding two phases.
                int duration = (int)((night.TotalMinutes - day.TotalMinutes + 1440) % 1440);
                if (duration < 3) throw new InvalidDataException("The legacy interval is too short for four distinct phases.");
                result.MorningStarts = day;
                result.NoonStarts = TimeSpan.FromMinutes((day.TotalMinutes + duration * 6 / 13) % 1440);
                result.DayStarts = TimeSpan.FromMinutes((day.TotalMinutes + duration * 9 / 13) % 1440);
            }
            else
            {
                TimeSpan morning, noon;
                if (!TimeSpan.TryParseExact(root.GetAttribute("morningStarts"), @"hh\:mm", CultureInfo.InvariantCulture, out morning)
                    || !TimeSpan.TryParseExact(root.GetAttribute("noonStarts"), @"hh\:mm", CultureInfo.InvariantCulture, out noon))
                    throw new InvalidDataException("Invalid morning or noon time.");
                result.MorningStarts = morning; result.NoonStarts = noon;
            }
            result.Validate();
            return result;
        }

        public void Save(string path)
        {
            Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartElement("dayNight");
                    writer.WriteAttributeString("version", "2");
                    writer.WriteAttributeString("mode", Mode.ToString());
                    writer.WriteAttributeString("morningStarts", MorningStarts.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("noonStarts", NoonStarts.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("dayStarts", DayStarts.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("nightStarts", NightStarts.ToString(@"hh\:mm", CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    public static class SkinSupport
    {
        public static string ResolvePeriod(string skinDirectory, string requested)
        {
            if (requested != "morning" && requested != "noon") return requested;
            try
            {
                var root = DayNightSettings.ReadXml(Path.Combine(skinDirectory, "Media", "DayNight", "skin-support.xml")).DocumentElement;
                foreach (string period in root.GetAttribute("periods").Split(','))
                    if (period.Trim() == requested) return requested;
            }
            catch { return "night"; }
            // A contract-1 skin with two image sets can still show a bright image.
            return "day";
        }
        public static bool Check(string skinDirectory, out string reason)
        {
            try
            {
                string marker = Path.Combine(skinDirectory, "Media", "DayNight", "skin-support.xml");
                if (!File.Exists(marker)) { reason = "This skin does not declare Day/Night support. Its backgrounds remain unchanged."; return false; }
                var root = DayNightSettings.ReadXml(marker).DocumentElement;
                if (root == null || root.Name != "dayNightSkin" || root.GetAttribute("contract") != "1")
                    throw new InvalidDataException("Unsupported skin integration contract.");
                string media = Path.GetFullPath(Path.Combine(skinDirectory, "Media")) + Path.DirectorySeparatorChar;
                var assets = root.SelectNodes("asset");
                if (assets.Count == 0) throw new InvalidDataException("No background assets declared.");
                foreach (XmlElement asset in assets)
                {
                    string relative = asset.GetAttribute("path");
                    string full = Path.GetFullPath(Path.Combine(media, relative.Replace('/', Path.DirectorySeparatorChar)));
                    if (String.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)
                        || !full.StartsWith(media, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
                        throw new InvalidDataException("A declared background is missing or outside the skin media folder: " + relative);
                }
                reason = "This skin supports Day/Night backgrounds.";
                return true;
            }
            catch (Exception error)
            {
                reason = "Day/Night support is unavailable: " + error.Message + " Original backgrounds will be used.";
                return false;
            }
        }
    }
}
