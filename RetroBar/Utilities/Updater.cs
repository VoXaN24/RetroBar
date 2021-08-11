using ManagedShell.Common.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RetroBar.Utilities
{
    class Updater : IDisposable
    {
        private readonly HttpClient httpClient = new HttpClient();

        private string _downloadUrl = "https://github.com/dremin/RetroBar";
        private string _versionUrl = "";
        private int _updateInterval = 86400000;
        
        private Version _currentVersion;

        private NotifyIcon notifyIcon;
        private System.Timers.Timer updateCheck;

        public Updater()
        {
            _currentVersion = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version;

            updateCheck = new System.Timers.Timer(10000);
            updateCheck.Elapsed += UpdateCheck_Elapsed;
            updateCheck.AutoReset = true;
            updateCheck.Start();
        }

        private async void UpdateCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // don't check again until the desired time
            updateCheck.Interval = _updateInterval;

            bool updateAvailable = await IsUpdateAvailable();

            if (!updateAvailable)
            {
                return;
            }

            ShowNotifyIcon();

            // if an update has been found, stop checking
            updateCheck.Stop();
        }

        public void Dispose()
        {
            updateCheck?.Stop();
        }

        private void ShowNotifyIcon()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                notifyIcon = new NotifyIcon(new System.ComponentModel.Container());
                notifyIcon.MouseClick += NotifyIcon_Click;
                notifyIcon.Icon = SystemIcons.Information;
                notifyIcon.Text = "RetroBar Update Available";

                notifyIcon.BalloonTipTitle = "RetroBar Update Available";
                notifyIcon.BalloonTipText = "Click here to download the new version.";
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;

                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(10000);
            });
        }

        private void NotifyIcon_Click(object sender, MouseEventArgs e)
        {
            // TODO: Click doesn't work. Try creating a native window and using direct Shell_NotifyIcon?
            Process.Start(_downloadUrl);
        }

        private async Task<bool> IsUpdateAvailable()
        {
            try
            {
                string newVersionStr = await httpClient.GetStringAsync(_versionUrl);

                if (Version.TryParse(newVersionStr, out Version newVersion))
                {
                    if (newVersion > _currentVersion)
                    {
                        return true;
                    }
                }
                else
                {
                    ShellLogger.Info($"Updater: Unable to parse new version file");
                }

            }
            catch (Exception e)
            {
                ShellLogger.Info($"Updater: Unable to check for updates: {e.Message}");
            }

            return false;
        }
    }
}
