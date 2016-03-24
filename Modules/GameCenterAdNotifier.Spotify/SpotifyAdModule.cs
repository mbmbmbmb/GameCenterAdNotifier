using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameCenterAdNotifier.Common;
using SpotifyAPI.Local;

namespace GameCenterAdNotifier.Spotify
{
    [Export(typeof (IAdModule))]
    public class SpotifyAdModule : IAdModule
    {
        private readonly TimeSpan m_connectSleep = TimeSpan.FromSeconds(5);

        private readonly SpotifyLocalAPI m_spotify;

        private bool m_playing;

        public SpotifyAdModule()
        {
            m_spotify = new SpotifyLocalAPI();
            m_spotify.OnPlayStateChange += OnPlayerStateChanged;
            m_spotify.ListenForEvents = true;
        }

        public string Title => "Spotify";

        public Task AdEnded()
        {
            var tcs = new TaskCompletionSource<bool>();

            m_spotify.Pause();

            tcs.SetResult(true);
            return tcs.Task;
        }

        public Task AdStarted(Screen screen)
        {
            var tcs = new TaskCompletionSource<bool>();

            m_spotify.Play();

            tcs.SetResult(true);
            return tcs.Task;
        }

        public Task Initialize()
        {
            var tcs = new TaskCompletionSource<bool>();

            ConnectLoop();

            try
            {
                var status = m_spotify.GetStatus();
                m_playing = status.Playing;

                if (m_playing)
                {
                    m_spotify.Pause();
                }

                var now = DateTime.UtcNow;
                var serverTime = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(status.ServerTime);
                var difference = now - serverTime;
                Utilities.WriteLineWithTime(
                    $"Connected to Spotify! Online: {status.Online}. Play Enabled: {status.PlayEnabled}");
                Utilities.WriteLineWithTime(
                    $"Playing: {status.Playing}. Running: {status.Running}. Track Not Null : {status.Track != null}");
                Utilities.WriteLineWithTime($"Client Version: '{status.ClientVersion}'.");
                Utilities.WriteLineWithTime($"ServerTime: '{serverTime}' ");
                Utilities.WriteLineWithTime(
                    $"LocalTime:  '{DateTime.UtcNow}' -- Difference: {difference.TotalMilliseconds}ms");
            }
            catch (Exception e)
            {
                Utilities.WriteLineWithTime($"Exception logging the Connect status: {e.Message}.");
            }

            tcs.SetResult(true);


            return tcs.Task;
        }

        private void OnPlayerStateChanged(object sender, PlayStateEventArgs e)
        {
            Utilities.WriteLineWithTime($"New Spotify Playing state. New Value: {e.Playing}, Old : {m_playing}.");
            m_playing = e.Playing;
        }

        private void ConnectLoop()
        {
            Utilities.WriteLineWithTime("Attempting to connect to Spotify...");

            while (true)
            {
                try
                {
                    if (ConnectInternal())
                    {
                        return;
                    }

                    Utilities.WriteLineWithTime("Failed to connect to Spotify. Attempting to reconnect in 10 seconds.");
                }
                catch (Exception e)
                {
                    Utilities.WriteLineWithTime(
                        $"Unexpected exception connecting to Spotify: {e.Message}. Attempting to reconnect in {m_connectSleep}.");
                }

                Thread.Sleep(m_connectSleep);
            }
        }

        private bool ConnectInternal()
        {
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                Utilities.WriteLineWithTime("Spotify isn't running. Trying to start it...");
                try
                {
                    SpotifyLocalAPI.RunSpotify();
                }
                catch (Exception e)
                {
                    Utilities.WriteLineWithTime($"Error starting spotify: {e.Message}");
                    return false;
                }
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                Utilities.WriteLineWithTime("Spotify Web Helper isn't running. Trying to start it...");
                try
                {
                    SpotifyLocalAPI.RunSpotifyWebHelper();
                }
                catch (Exception e)
                {
                    Utilities.WriteLineWithTime($"Error starting Spotify Web Helper: {e.Message}");
                    return false;
                }
            }

            return m_spotify.Connect();
        }
    }
}