using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common.Utils;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

[assembly: AssemblyTitle("DayNight Backgrounds")]
[assembly: AssemblyDescription("Optional day/night background state for cooperating MediaPortal skins")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: CompatibleVersion("1.39.0.0")]

namespace Backdrop.DayNight
{
    public sealed class DayNightPlugin : IPlugin, ISetupForm
    {
        internal static DayNightPlugin Current;
        private readonly object lifecycle = new object();
        private System.Threading.Timer timer;
        private bool running;
        private int generation;
        private int pending;
        private DayNightSettings settings = new DayNightSettings();
        private DateTime loadedStamp = DateTime.MinValue;
        private bool settingsLoaded;
        private bool settingsValid = true;
        private string lastStatus;

        internal static string SettingsPath { get { return Config.GetFile(Config.Dir.Config, "DayNight.xml"); } }

        public void Start()
        {
            lock (lifecycle)
            {
                if (running) return;
                running = true;
                generation++;
                Current = this;
                GUIWindowManager.OnActivateWindow += OnWindow;
                // One lightweight check per minute; never load textures in this timer.
                timer = new System.Threading.Timer(TimerTick, null, 0, Timeout.Infinite);
            }
        }

        private void OnWindow(int windowId) { RequestUpdate(null); }

        private void TimerTick(object unused)
        {
            RequestUpdate(null);
            lock (lifecycle)
            {
                if (!running || timer == null) return;
                DateTime now = DateTime.Now;
                // Align to the next wall-clock minute rather than drifting from startup.
                timer.Change(60000 - now.Second * 1000 - now.Millisecond, Timeout.Infinite);
            }
        }

        private void RequestUpdate(object unused)
        {
            int current;
            lock (lifecycle) { if (!running) return; current = generation; }
            var form = GUIGraphicsContext.form;
            if (form == null || form.IsDisposed || !form.IsHandleCreated || Interlocked.CompareExchange(ref pending, 1, 0) != 0) return;
            try
            {
                form.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        lock (lifecycle) { if (!running || generation != current) return; }
                        UpdateState();
                    }
                    catch (Exception error) { Log.Warn("DayNight: state update failed: {0}", error.Message); Publish("false", "disabled", "night"); }
                    finally { Interlocked.Exchange(ref pending, 0); }
                });
            }
            catch (InvalidOperationException) { Interlocked.Exchange(ref pending, 0); }
        }

        private void UpdateState()
        {
            string path = SettingsPath;
            DateTime stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (!settingsLoaded || stamp != loadedStamp)
            {
                try { settings = DayNightSettings.Load(path); settingsValid = true; }
                catch (Exception error)
                {
                    settings = new DayNightSettings { Mode = BackgroundMode.Disabled };
                    settingsValid = false;
                    Log.Warn("DayNight: invalid settings; original backgrounds retained: {0}", error.Message);
                }
                loadedStamp = stamp;
                settingsLoaded = true;
            }
            string reason;
            string skin = GUIGraphicsContext.SkinName;
            bool supported = !String.IsNullOrEmpty(skin) && SkinSupport.Check(Path.Combine(Config.GetFolder(Config.Dir.Skin), skin), out reason);
            string period = supported && settingsValid ? SkinSupport.ResolvePeriod(Path.Combine(Config.GetFolder(Config.Dir.Skin), skin), settings.Period(DateTime.Now)) : "night";
            string mode = settingsValid ? settings.Mode.ToString().ToLowerInvariant() : "disabled";
            Publish(supported ? "true" : "false", mode, period);
            SetChanged("#DayNight.Available", DayNightWindow.EnsureRegistered() ? "true" : "false");
            SetChanged("#DayNight.Label.Title", DayNightText.Get("Title"));
            string status = skin + ":" + supported + ":" + mode + ":" + period;
            if (lastStatus != status) { Log.Info("DayNight: {0}", status); lastStatus = status; }
        }

        internal void ReloadNow() { settingsLoaded = false; UpdateState(); }

        private static void SetChanged(string name, string value)
        {
            if (GUIPropertyManager.GetProperty(name) != value) GUIPropertyManager.SetProperty(name, value);
        }

        private static void Publish(string supported, string mode, string period)
        {
            // Period is set last, so consumers see a fully published state.
            SetChanged("#DayNight.Supported", supported);
            SetChanged("#DayNight.Mode", mode);
            SetChanged("#DayNight.Period", period);
        }

        public void Stop()
        {
            lock (lifecycle)
            {
                if (!running) return;
                running = false;
                generation++;
                if (Object.ReferenceEquals(Current, this)) Current = null;
                GUIWindowManager.OnActivateWindow -= OnWindow;
                if (timer != null) { timer.Dispose(); timer = null; }
            }
            var form = GUIGraphicsContext.form;
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
            MethodInvoker clear = delegate
            {
                lock (lifecycle) { if (running) return; }
                Publish("false", "disabled", "night");
                SetChanged("#DayNight.Available", "false");
            };
            try { if (form.InvokeRequired) form.BeginInvoke(clear); else clear(); }
            catch (InvalidOperationException) { }
        }

        public string PluginName() { return "Day/Night Backgrounds"; }
        public string Description() { return DayNightText.Get("Title"); }
        public string Author() { return "Backdrop contributors"; }
        public bool CanEnable() { return true; }
        public bool DefaultEnabled() { return true; }
        public bool HasSetup() { return true; }
        public int GetWindowId() { return -1; }
        public void ShowPlugin() { using (var dialog = new DayNightConfiguration(SettingsPath)) dialog.ShowDialog(); }
        public bool GetHome(out string text, out string image, out string focus, out string picture)
        { text = image = focus = picture = String.Empty; return false; }
    }
}
