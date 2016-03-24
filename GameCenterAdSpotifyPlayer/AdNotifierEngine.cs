using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameCenterAdNotifier.Common;
using GameCenterAdNotifier.Properties;
using XnaFan.ImageComparison;

namespace GameCenterAdNotifier
{
    internal sealed class AdNotifierEngine
    {
        private readonly int m_pollPeriodMilliseconds;

        private IEnumerable<IAdModule> m_modules;

        private Screen m_screenPlayingAd;

        private AdNotifierEngine(int pollPeriodMilliseconds)
        {
            m_pollPeriodMilliseconds = pollPeriodMilliseconds;
        }

        private void ImportModules()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var modules = Directory.GetDirectories(assemblyDirectory + "\\Modules");

            //An aggregate catalog that combines multiple catalogs
            var catalog = new AggregateCatalog();
            //Adds all the parts found in all assemblies in 
            //the same directory as the executing program 

            foreach (var module in modules)
            {
                catalog.Catalogs.Add(
                    new DirectoryCatalog(module));
            }

            //Create the CompositionContainer with the parts in the catalog
            var container = new CompositionContainer(catalog);

            //Fill the imports of this object
            container.ComposeParts(this);

            m_modules = container.GetExportedValues<IAdModule>();
        }

        public static Task<AdNotifierEngine> Create(int pollPeriodMilliseconds)
        {
            var engine = new AdNotifierEngine(pollPeriodMilliseconds);
            return engine.InitializeAsync();
        }

        private async Task<AdNotifierEngine> InitializeAsync()
        {
            await LoadModules();
            return this;
        }

        private async Task LoadModules()
        {
            ImportModules();

            var tasks = m_modules.Select(module => module.Initialize()).ToList();


            await Task.WhenAll(tasks);
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

        public void LoopForever()
        {
            var inProgressImage = Resources.CommercialBreakInProgress;
            while (true)
            {
                try
                {
                    foreach (var screen in Screen.AllScreens)
                    {
                        if (IsAdPlaying() && !IsAdPlaying(screen))
                        {
                            continue;
                        }

                        using (var currentScreen = CaptureScreen(screen))
                        {
                            using (var currentScreenResized = currentScreen.Resize(1920, 1080))
                            {
                                var difference = currentScreenResized.PercentageDifference(inProgressImage);

                                if (difference < 0.70)
                                {
                                    if (!IsAdPlaying())
                                    {
                                        Utilities.WriteLineWithTime(
                                            $"Commercial break started on display '{screen.DeviceName}' -- attempting to play!");
                                        m_screenPlayingAd = screen;
                                        NotifyModules(true, screen);
                                        break;
                                    }
                                }
                                else
                                {
                                    if (IsAdPlaying())
                                    {
                                        Utilities.WriteLineWithTime(
                                            $"Commercial break ended on display '{screen.DeviceName}' --  attempting to pause!");
                                        m_screenPlayingAd = null;
                                        NotifyModules(false, screen);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Utilities.WriteLineWithTime($"Unexpected Exception: {e.Message}");
                }

                Thread.Sleep(m_pollPeriodMilliseconds);
            }
        }

        private void NotifyModules(bool isStarted, Screen screen)
        {
            foreach (var module in m_modules)
            {
                if (isStarted)
                {
                    module.AdStarted(screen);
                }
                else
                {
                    module.AdEnded();
                }
            }
        }

        private bool IsAdPlaying()
        {
            return m_screenPlayingAd != null;
        }

        private bool IsAdPlaying(Screen screen)
        {
            return IsAdPlaying() && m_screenPlayingAd.Equals(screen);
        }
    }
}