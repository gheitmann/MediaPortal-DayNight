using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Backdrop.DayNight;
using MediaPortal.GUI.Library;

internal static class DayNightProbe
{
    private static int checks;
    private static string client;
    private static string source;
    private static void Check(bool valid, string name)
    { if (!valid) throw new Exception("FAIL: " + name); checks++; }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            client = args[0]; source = Path.Combine(args[1], "dist");
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs request)
            {
                string name = new AssemblyName(request.Name).Name + ".dll";
                foreach (string folder in new[] { source, client, Path.Combine(client, "plugins", "windows"), Path.Combine(client, "plugins", "process") })
                {
                    string path = Path.Combine(folder, name);
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            Run(args[1], args[2]);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string repo, string scratch)
    {
        Directory.CreateDirectory(scratch);
        var defaults = new DayNightSettings();
        foreach (BackgroundMode mode in Enum.GetValues(typeof(BackgroundMode)))
        {
            var settings = new DayNightSettings { Mode = mode, MorningStarts = new TimeSpan(6,30,0), NoonStarts = new TimeSpan(12,15,0), DayStarts = new TimeSpan(15,45,0), NightStarts = new TimeSpan(19,15,0) };
            string path = Path.Combine(scratch, "settings.xml");
            settings.Save(path); settings.Save(path);
            var loaded = DayNightSettings.Load(path);
            Check(loaded.Mode == mode && loaded.MorningStarts == settings.MorningStarts && loaded.NoonStarts == settings.NoonStarts && loaded.DayStarts == settings.DayStarts && loaded.NightStarts == settings.NightStarts, "mode/time round trip: " + mode);
            foreach (string time in new[] { "00:00", "06:29", "06:30", "12:14", "12:15", "15:44", "15:45", "19:14", "19:15", "23:59" })
            {
                var now = DateTime.Today.Add(TimeSpan.ParseExact(time, @"hh\:mm", CultureInfo.InvariantCulture));
                string expected = mode.ToString().ToLowerInvariant();
                if (mode == BackgroundMode.Disabled) expected = "night";
                if (mode == BackgroundMode.Automatic)
                    expected = now.TimeOfDay < settings.MorningStarts || now.TimeOfDay >= settings.NightStarts ? "night" : now.TimeOfDay < settings.NoonStarts ? "morning" : now.TimeOfDay < settings.DayStarts ? "noon" : "day";
                Check(loaded.Period(now) == expected, mode + " boundary " + time);
            }
        }
        var overnight = new DayNightSettings { MorningStarts=TimeSpan.FromHours(20),NoonStarts=TimeSpan.Zero,DayStarts=TimeSpan.FromHours(3),NightStarts=TimeSpan.FromHours(6) };
        string[] overnightTimes={"00:00","02:59","03:00","05:59","06:00","19:59","20:00","23:59"};
        string[] overnightExpected={"noon","noon","day","day","night","night","morning","morning"};
        for(int i=0;i<overnightTimes.Length;i++)
            Check(overnight.Period(DateTime.Today.Add(TimeSpan.ParseExact(overnightTimes[i],@"hh\:mm",CultureInfo.InvariantCulture)))==overnightExpected[i],"overnight "+overnightTimes[i]);
        Check(defaults.Period(DateTime.Today.AddHours(6))=="morning","default morning");
        Check(defaults.Period(DateTime.Today.AddHours(12))=="noon","default noon");
        Check(defaults.Period(DateTime.Today.AddHours(15))=="day","default afternoon");
        Check(defaults.Period(DateTime.Today.AddHours(19))=="night","default night");
        foreach(var invalid in new[] {
            new DayNightSettings { NoonStarts=TimeSpan.FromHours(6) },
            new DayNightSettings { NoonStarts=TimeSpan.FromHours(18),DayStarts=TimeSpan.FromHours(12) },
            new DayNightSettings { MorningStarts=TimeSpan.FromHours(6).Add(TimeSpan.FromMilliseconds(1)) } })
        {
            bool rejected=false;
            try { invalid.Save(Path.Combine(scratch,"invalid-time.xml")); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"invalid schedule rejected");
        }
        string bad=Path.Combine(scratch,"invalid.xml");
        foreach(string xml in new[] { "<broken", "<dayNight version='2' mode='Unexpected' morningStarts='06:00' noonStarts='12:00' dayStarts='15:00' nightStarts='19:00'/>", "<dayNight version='2' mode='Automatic' morningStarts='25:00' noonStarts='12:00' dayStarts='15:00' nightStarts='19:00'/>", "<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///invalid'>]><dayNight>&y;</dayNight>" })
        {
            File.WriteAllText(bad,xml);bool rejected=false;
            try { DayNightSettings.Load(bad); } catch { rejected=true; }
            Check(rejected,"invalid settings rejected");
        }
        string legacy=Path.Combine(scratch,"legacy.xml");
        File.WriteAllText(legacy,"<dayNight version='1' mode='Automatic' dayStarts='06:00' nightStarts='19:00'/>");
        var migrated=DayNightSettings.Load(legacy);
        Check(migrated.MorningStarts==TimeSpan.FromHours(6) && migrated.NoonStarts==TimeSpan.FromHours(12) && migrated.DayStarts==TimeSpan.FromHours(15) && migrated.NightStarts==TimeSpan.FromHours(19),"legacy default migration");
        File.WriteAllText(legacy,"<dayNight version='1' mode='Day' dayStarts='20:00' nightStarts='06:00'/>");
        migrated=DayNightSettings.Load(legacy);migrated.Validate();
        Check(migrated.Mode==BackgroundMode.Day && migrated.MorningStarts==TimeSpan.FromHours(20) && migrated.NightStarts==TimeSpan.FromHours(6),"legacy overnight endpoints retained");
        var catalogue=new XmlDocument();catalogue.Load(Path.Combine(repo,"src","Languages.xml"));
        int languages=0;
        foreach(XmlElement language in catalogue.SelectNodes("/languages/language"))
        {
            languages++;
            foreach(XmlElement entry in language.SelectNodes("text"))
                Check(DayNightText.Get(entry.GetAttribute("key"),language.GetAttribute("code"))==entry.InnerText,"translation "+language.GetAttribute("code")+"/"+entry.GetAttribute("key"));
        }
        Check(languages==37,"all 37 language catalogues");
        Check(DayNightText.Get("Morning","de-DE")=="Morgen" && DayNightText.Get("Noon","de-DE")=="Mittag","German period names");
        Check(DayNightText.Get("Morning","unknown-XX")=="Morning","unknown language English fallback");
        string reason;
        string fake = Path.Combine(scratch, "unsupported"); Directory.CreateDirectory(fake);
        Check(!SkinSupport.Check(fake, out reason), "unsupported skin fallback");
        string marker = Path.Combine(fake, "Media", "DayNight", "skin-support.xml"); Directory.CreateDirectory(Path.GetDirectoryName(marker));
        foreach (string xml in new[] { "<dayNightSkin contract='2'><asset path='x.png'/></dayNightSkin>", "<dayNightSkin contract='1'/>", "<dayNightSkin contract='1'><asset path='missing.png'/></dayNightSkin>", "<dayNightSkin contract='1'><asset path='../../../escape.png'/></dayNightSkin>" })
        {
            File.WriteAllText(marker, xml); Check(!SkinSupport.Check(fake, out reason), "invalid/incomplete skin fallback");
        }

        for (int minute=0; minute<1440; minute++)
        {
            string expected=minute<360 || minute>=1140 ? "night" : minute<720 ? "morning" : minute<900 ? "noon" : "day";
            Check(defaults.Period(DateTime.Today.AddMinutes(minute))==expected,"full-day minute "+minute);
        }
        Console.WriteLine("PASS: " + checks + " standalone assertions on " + (IntPtr.Size*8) + "-bit host.");
    }
}
