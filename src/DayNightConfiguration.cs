using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MediaPortal.Configuration;

namespace Backdrop.DayNight
{
    internal sealed class DayNightConfiguration : Form
    {
        private readonly string settingsPath;
        private readonly ComboBox mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly DateTimePicker morning = TimeField(), noon = TimeField(), day = TimeField(), night = TimeField();
        private static DateTimePicker TimeField()
        { return new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Dock = DockStyle.Fill }; }

        internal DayNightConfiguration(string path)
        {
            settingsPath = path; Text = DayNightText.Get("Title");
            Font = SystemFonts.MessageBoxFont; AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(650, 420); FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; MaximizeBox = MinimizeBox = false;
            if (DayNightText.CultureCode.StartsWith("he", StringComparison.OrdinalIgnoreCase))
            { RightToLeft = RightToLeft.Yes; RightToLeftLayout = true; }
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 8 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            foreach (BackgroundMode value in DayNightSkinActions.Modes) mode.Items.Add(DayNightText.Get(value.ToString()));
            AddRow(grid, DayNightText.Get("Mode"), mode, 0);
            AddRow(grid, DayNightText.Get("Morning"), morning, 1); AddRow(grid, DayNightText.Get("Noon"), noon, 2);
            AddRow(grid, DayNightText.Get("Day"), day, 3); AddRow(grid, DayNightText.Get("Night"), night, 4);
            var help = new Label { Dock = DockStyle.Fill, Text = DayNightText.Get("Times"), AutoSize = false };
            grid.Controls.Add(help, 0, 5); grid.SetColumnSpan(help, 2);
            var status = new Label { Dock = DockStyle.Fill, AutoSize = false };
            grid.Controls.Add(status, 0, 6); grid.SetColumnSpan(status, 2);
            try
            {
                string skin;
                using (var profile = new MediaPortal.Profile.Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
                    skin = profile.GetValueAsString("skin", "name", "DefaultWideHD");
                string reason;
                bool supported = SkinSupport.Check(Path.Combine(Config.GetFolder(Config.Dir.Skin), skin), out reason);
                status.Text = DayNightText.Get("Skin") + ": " + skin + Environment.NewLine + DayNightText.Get(supported ? "Supported" : "Unsupported");
            }
            catch { status.Text = DayNightText.Get("Unsupported"); }
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = new Button { Text = DayNightText.Get("Cancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
            var save = new Button { Text = DayNightText.Get("Save"), AutoSize = true };
            actions.Controls.Add(cancel); actions.Controls.Add(save);
            grid.Controls.Add(actions, 0, 7); grid.SetColumnSpan(actions, 2); Controls.Add(grid);
            AcceptButton = save; CancelButton = cancel;
            DayNightSettings current;
            try { current = DayNightSettings.Load(path); }
            catch { current = new DayNightSettings { Mode = BackgroundMode.Disabled }; status.Text += Environment.NewLine + DayNightText.Get("Invalid"); }
            mode.SelectedIndex = Array.IndexOf(DayNightSkinActions.Modes, current.Mode);
            morning.Value = DateTime.Today.Add(current.MorningStarts); noon.Value = DateTime.Today.Add(current.NoonStarts);
            day.Value = DateTime.Today.Add(current.DayStarts); night.Value = DateTime.Today.Add(current.NightStarts);
            mode.SelectedIndexChanged += delegate { EnableTimes(); }; EnableTimes();
            save.Click += delegate
            {
                try
                {
                    var updated = new DayNightSettings { Mode = DayNightSkinActions.Modes[mode.SelectedIndex],
                        MorningStarts = MinuteValue(morning), NoonStarts = MinuteValue(noon), DayStarts = MinuteValue(day), NightStarts = MinuteValue(night) };
                    updated.Save(settingsPath); DialogResult = DialogResult.OK; Close();
                }
                catch (Exception error)
                { MessageBox.Show(this, DayNightText.Get(error is InvalidDataException ? "Invalid" : "Error"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
        }
        private static TimeSpan MinuteValue(DateTimePicker field) { return new TimeSpan(field.Value.Hour, field.Value.Minute, 0); }
        private void EnableTimes() { morning.Enabled = noon.Enabled = day.Enabled = night.Enabled = mode.SelectedIndex == 0; }
        private static void AddRow(TableLayoutPanel grid, string text, Control field, int row)
        {
            grid.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
            field.Margin = new Padding(3, 6, 3, 6); grid.Controls.Add(field, 1, row);
        }
    }
}

