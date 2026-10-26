using System;
using System.Globalization;
using System.IO;
using MediaPortal.Dialogs;
using MediaPortal.GUI.Library;

namespace Backdrop.DayNight
{
    public static class DayNightSkinActions
    {
        internal static readonly BackgroundMode[] Modes = { BackgroundMode.Automatic, BackgroundMode.Morning, BackgroundMode.Noon, BackgroundMode.Day, BackgroundMode.Night, BackgroundMode.Disabled };
        public static string Configure()
        {
            var plugin = DayNightPlugin.Current;
            if (plugin == null || GUIGraphicsContext.form == null || GUIGraphicsContext.form.InvokeRequired) return String.Empty;
            int parent = GUIWindowManager.ActiveWindow;
            try
            {
                var value = DayNightSettings.Load(DayNightPlugin.SettingsPath).Copy();
                while (true)
                {
                    string[] items = {
                        DayNightText.Get("Mode") + ": " + DayNightText.Get(value.Mode.ToString()),
                        DayNightText.Get("Morning") + ": " + Format(value.MorningStarts),
                        DayNightText.Get("Noon") + ": " + Format(value.NoonStarts),
                        DayNightText.Get("Day") + ": " + Format(value.DayStarts),
                        DayNightText.Get("Night") + ": " + Format(value.NightStarts),
                        DayNightText.Get("Save"), DayNightText.Get("Cancel") };
                    int selected = Choose(DayNightText.Get("Title"), items, 0, parent);
                    if (selected < 0 || selected == 6) return String.Empty;
                    if (selected == 0)
                    {
                        string[] names = Array.ConvertAll(Modes, m => DayNightText.Get(m.ToString()));
                        int index = Choose(DayNightText.Get("Mode"), names, Array.IndexOf(Modes, value.Mode), parent);
                        if (index >= 0) value.Mode = Modes[index];
                    }
                    else if (selected == 5)
                    {
                        try { value.Save(DayNightPlugin.SettingsPath); plugin.ReloadNow(); return String.Empty; }
                        catch (InvalidDataException) { ShowError(DayNightText.Get("Invalid"), parent); }
                        catch (Exception error) { Log.Warn("DayNight: in-skin save failed: {0}", error.Message); ShowError(DayNightText.Get("Error"), parent); }
                    }
                    else
                    {
                        TimeSpan[] times = { value.MorningStarts, value.NoonStarts, value.DayStarts, value.NightStarts };
                        string[] phases = { "Morning", "Noon", "Day", "Night" };
                        TimeSpan edited;
                        if (PickTime(phases[selected - 1], times[selected - 1], parent, out edited))
                        {
                            switch (selected)
                            {
                                case 1: value.MorningStarts = edited; break;
                                case 2: value.NoonStarts = edited; break;
                                case 3: value.DayStarts = edited; break;
                                case 4: value.NightStarts = edited; break;
                            }
                        }
                    }
                }
            }
            catch (Exception error) { Log.Warn("DayNight: in-skin configuration failed: {0}", error.Message); ShowError(DayNightText.Get("Error"), parent); }
            return String.Empty;
        }

        private static string Format(TimeSpan value) { return value.ToString(@"hh\:mm", CultureInfo.InvariantCulture); }
        private static int Choose(string heading, string[] values, int current, int parent)
        {
            var dialog = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU) as GUIDialogMenu;
            if (dialog == null) return -1;
            dialog.Reset(); dialog.SetHeading(heading);
            foreach (string value in values) dialog.Add(value);
            dialog.SelectedLabel = Math.Max(0, current); dialog.DoModal(parent);
            return dialog.SelectedLabel;
        }
        private static bool PickTime(string phase, TimeSpan value, int parent, out TimeSpan edited)
        {
            edited = value;
            string[] hours = new string[24], minutes = new string[60];
            for (int i = 0; i < hours.Length; i++) hours[i] = i.ToString("00");
            for (int i = 0; i < minutes.Length; i++) minutes[i] = i.ToString("00");
            int hour = Choose(DayNightText.Get(phase) + " · " + DayNightText.Get("Hour"), hours, value.Hours, parent);
            if (hour < 0) return false;
            int minute = Choose(DayNightText.Get(phase) + " · " + DayNightText.Get("Minute"), minutes, value.Minutes, parent);
            if (minute < 0) return false;
            edited = new TimeSpan(hour, minute, 0); return true;
        }
        private static void ShowError(string text, int parent)
        {
            var dialog = GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK) as GUIDialogOK;
            if (dialog == null) return;
            dialog.SetHeading(DayNightText.Get("Title")); dialog.SetLine(1, text); dialog.DoModal(parent);
        }
    }
}
