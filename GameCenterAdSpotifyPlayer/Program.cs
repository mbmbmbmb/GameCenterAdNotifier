using SpotifyAPI.Local;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using XnaFan.ImageComparison;

namespace GameCenterAdSpotifyPlayer
{
    class Program
    {
        private static Screen m_adOnScreen;

        private static readonly TimeSpan m_connectSleep = TimeSpan.FromSeconds(5);

        private static bool m_playing;

        private static Properties.ProgramSettings m_settings;

        private static readonly SpotifyLocalAPI m_spotify;

        public const double StreamRes = (double)16 / (double)9;

        private static object m_playingSyncObj = new object();

        private static void CheckScreenResolutions()
        {
            foreach(var screen in Screen.AllScreens)
            {
                double screenRes = (double)screen.Bounds.Size.Width / (double)screen.Bounds.Size.Height;

                ScreenShotInfo info = new ScreenShotInfo(screen);

                string resolution = String.Empty;

                if(screenRes == StreamRes)
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

                WriteLineWithTime($"Display '{screen.DeviceName}' {resolution} ({screen.Bounds.Size.Width}x{screen.Bounds.Size.Height}). Screen Shot Info: {info.Width}x{info.Height} capture. {info.XOffSet}x{info.YOffSet} offset.");
            }
        }

        private static void CalculateScreenCaptureValues(Screen screen)
        {

            double screenRes = (double)screen.Bounds.Size.Width / (double)screen.Bounds.Size.Height;

            if (screenRes == StreamRes)
            {
                WriteLineWithTime($"Display '{screen.DeviceName}' is 16:9 ({screen.Bounds.Size.Width}x{screen.Bounds.Size.Height})");
            }
            else if (screenRes > StreamRes)
            {
                WriteLineWithTime($"Display '{screen.DeviceName}' is wider than 16:9 ({screen.Bounds.Size.Width}x{screen.Bounds.Size.Height})");
            }
            else if (screenRes < StreamRes)
            {
                WriteLineWithTime($"Display '{screen.DeviceName}' is taller than 16:9 ({screen.Bounds.Width}x{screen.Bounds.Height})");
            }
        }

        private static Bitmap CaptureScreen(Screen screen)
        {
            var ssInfo = new ScreenShotInfo(screen);

            //Create a new bitmap.
            var bmpScreenshot = new Bitmap(ssInfo.Width,
                                           ssInfo.Height,
                                           PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
            {

                // Take the screenshot from the upper left corner to the right bottom corner.
                gfxScreenshot.CopyFromScreen(screen.Bounds.X + ssInfo.XOffSet,
                                            screen.Bounds.Y + ssInfo.YOffSet,
                                            0,
                                            0,
                                            new Size(ssInfo.Width, ssInfo.Height),
                                            CopyPixelOperation.SourceCopy);
            }

            return bmpScreenshot;
        }

        internal static void Connect()
        {
            WriteLineWithTime("Attempting to connect to Spotify...");

            while (true)
            {
                try
                {
                    if (ConnectInternal())
                    {
                        var status = m_spotify.GetStatus();
                        m_playing = status.Playing;
                        try
                        {
                            var now = DateTime.UtcNow;
                            var serverTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(status.ServerTime);
                            var difference = now - serverTime;
                            WriteLineWithTime($"Connected to Spotify! Online: {status.Online}. Play Enabled: {status.PlayEnabled}");
                            WriteLineWithTime($"Playing: {status.Playing}. Running: {status.Running}. Track Length: {status.Track.Length}");
                            WriteLineWithTime($"Client Version: '{status.ClientVersion}'.");
                            WriteLineWithTime($"ServerTime: '{serverTime}' ");
                            WriteLineWithTime($"LocalTime:  '{DateTime.UtcNow}' -- Difference: {difference.TotalMilliseconds}ms");
                        }
                        catch(Exception e)
                        {
                            WriteLineWithTime($"Exception logging the Connect status: {e.Message}.");
                        }
                        return;
                    }

                    WriteLineWithTime("Failed to connect to Spotify. Attempting to reconnect in 10 seconds.");
                }
                catch (Exception e)
                {
                    WriteLineWithTime($"Unexpected exception connecting to Spotify: {e.Message}. Attempting to reconnect in {m_connectSleep}.");
                }

                Thread.Sleep(m_connectSleep);
            }
        }

        private static bool ConnectInternal()
        {
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                WriteLineWithTime("Spotify isn't running. Trying to start it...");
                try
                {
                    SpotifyLocalAPI.RunSpotify();
                }
                catch (Exception e)
                {
                    WriteLineWithTime($"Error starting spotify: {e.Message}");
                    return false;
                }
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                WriteLineWithTime("Spotify Web Helper isn't running. Trying to start it...");
                try
                {
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                }
                catch (Exception e)
                {
                    WriteLineWithTime($"Error starting Spotify Web Helper: {e.Message}");
                    return false;
                }
            }

            return m_spotify.Connect();
        }

        private static void LoopForever()
        {
            var inProgressImage = Properties.Resources.CommercialBreakInProgress;
            while (true)
            {
                try
                {
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        lock (m_playingSyncObj)
                        {
                            if(m_playing && m_adOnScreen != null && m_adOnScreen != screen)
                            {
                                continue;
                            }
                        }

                        using (var currentScreen = CaptureScreen(screen))
                        {
                            var difference = currentScreen.PercentageDifference(inProgressImage);

                            if (difference < 0.70)
                            {
                                lock (m_playingSyncObj)
                                {
                                    if (!m_playing)
                                    {
                                        WriteLineWithTime($"Commercial break started on display '{screen.DeviceName}' -- attempting to play!");
                                        m_adOnScreen = screen;
                                        m_spotify.Play();
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                lock (m_playingSyncObj)
                                {
                                    if (m_playing)
                                    {
                                        WriteLineWithTime($"Commercial break ended on display '{screen.DeviceName}' --  attempting to pause!");
                                        m_adOnScreen = null;
                                        m_spotify.Pause();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    WriteLineWithTime($"Unexpected Exception: {e.Message}");
                }

                Thread.Sleep(m_settings.PollPeriodMilliseconds);
            }
        }

        static Program()
        {
            m_settings = Properties.ProgramSettings.Default;
            m_spotify = new SpotifyLocalAPI();
            m_spotify.OnPlayStateChange += OnPlayerStateChanged;
            m_spotify.ListenForEvents = true;
        }

        [STAThread]
        static void Main(string[] args)
        {
            m_settings = Properties.ProgramSettings.Default;

            PrintVersionAndSettings();

            CheckScreenResolutions();

            Connect();

            LoopForever();
        }

        private static void PrintVersionAndSettings()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;

            WriteLineWithTime($"GameCenterAdSpotifyPlayer Version {version}");
            WriteLineWithTime($"PollPeriodMilliseconds: {m_settings.PollPeriodMilliseconds}ms");
        }

        private static void OnPlayerStateChanged(object sender, PlayStateEventArgs e)
        {
            lock (m_playingSyncObj)
            {
                WriteLineWithTime($"New Spotify Playing state. New Value: {e.Playing}, Old : {m_playing}.");
                if(!e.Playing)
                {
                    m_adOnScreen = null;
                }
                m_playing = e.Playing;
            }
        }

        public static void WriteLineWithTime(string message)
        {
            Console.WriteLine($"[{DateTime.Now}]: {message}");
        }

    }
}