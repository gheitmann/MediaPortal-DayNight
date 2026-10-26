using System;
using System.IO;
using System.Windows.Forms;
using MediaPortal.GUI.Library;

namespace Backdrop.DayNight
{
    internal sealed class DayNightWindow : GUIWindow
    {
        internal const int WindowId = 87950;
        private bool opening;
        public DayNightWindow() { GetID = WindowId; }
        public override bool Init() { return Load(GUIGraphicsContext.GetThemedSkinFile(@"\DayNight.xml")); }

        internal static bool EnsureRegistered()
        {
            var existing = GUIWindowManager.GetWindow(WindowId);
            if (existing != null) return existing is DayNightWindow;
            try
            {
                string path = GUIGraphicsContext.GetThemedSkinFile(@"\DayNight.xml");
                if (!File.Exists(path)) return false;
                GUIWindow window = new DayNightWindow();
                if (!window.Init()) return false;
                GUIWindowManager.Add(ref window);
                return Object.ReferenceEquals(GUIWindowManager.GetWindow(WindowId), window);
            }
            catch (Exception error) { Log.Warn("DayNight: configuration window unavailable: {0}", error.Message); return false; }
        }

        protected override void OnPageLoad()
        {
            base.OnPageLoad();
            if (opening || GUIGraphicsContext.form == null) return;
            opening = true;
            GUIGraphicsContext.form.BeginInvoke((MethodInvoker)delegate
            {
                try { if (GUIWindowManager.ActiveWindow == WindowId) DayNightSkinActions.Configure(); }
                finally
                {
                    opening = false;
                    if (GUIWindowManager.ActiveWindow == WindowId) GUIWindowManager.ShowPreviousWindow();
                }
            });
        }
    }
}
