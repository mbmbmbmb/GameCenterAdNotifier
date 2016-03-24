using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using GameCenterAdNotifier.Common;
using GameCenterAdNotifier.Properties;

namespace GameCenterAdNotifier
{
    class Program
    {
        private static ProgramSettings m_settings;

        public const double StreamRes = (double)16 / (double)9;


        private static void CheckScreenResolutions()
        {
            foreach(var screen in Screen.AllScreens)
            {
                double screenRes = (double)screen.Bounds.Size.Width / (double)screen.Bounds.Size.Height;

                ScreenShotInfo info = new ScreenShotInfo(screen);

                string resolution = String.Empty;

                if(Math.Abs(screenRes - StreamRes) < .001)
                {
                    resolution = "is 16:9";
                }
                else if(screenRes > StreamRes)
                {
                    resolution = "is wider than 16:9";
                }
                else if(screenRes < StreamRes)
                {
                    resolution = "is taller than 16:9";
                }

                Utilities.WriteLineWithTime($"Display '{screen.DeviceName}' {resolution} ({screen.Bounds.Size.Width}x{screen.Bounds.Size.Height}). Screen Shot Info: {info.Width}x{info.Height} capture. {info.XOffSet}x{info.YOffSet} offset.");
            }
        }     

        [STAThread]
        static void Main(string[] args)
        {
            m_settings = Properties.ProgramSettings.Default;

            PrintVersionAndSettings();

            CheckScreenResolutions();

            var engine = AdNotifierEngine.Create(m_settings.PollPeriodMilliseconds);

            engine.Result.LoopForever();
        }

        private static void PrintVersionAndSettings()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;

            Utilities.WriteLineWithTime($"GameCenterAdSpotifyPlayer Version {version}");
            Utilities.WriteLineWithTime($"PollPeriodMilliseconds: {m_settings.PollPeriodMilliseconds}ms");
        }

    }
}