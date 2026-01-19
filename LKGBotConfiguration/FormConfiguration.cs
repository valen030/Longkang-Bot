using System.Diagnostics;
using System.Text.RegularExpressions;

using LKGBotConfiguration.Helper;

using LKGServiceBot;

namespace LKGBotConfiguration
{
    public partial class FormConfiguration : Form
    {
        private string _exeFolder = string.Empty;
        private string _workerFolder = string.Empty;
        private string _serverFolder = string.Empty;

        private ConfigSetting _configSettings;
        private YouTubeHelper _youtubeHelper;
        private SettingsHelper _settingsHelper;

        public FormConfiguration()
        {
            InitializeComponent();

            _exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            _workerFolder = Path.Combine(_exeFolder, "WorkerService");
            _serverFolder = Path.Combine(_exeFolder, "WorkerService", "Server");
            _youtubeHelper = new YouTubeHelper(_workerFolder);
            _settingsHelper = new SettingsHelper(_workerFolder);
        }

        private async Task CheckJavaAsync()
        {
            try
            {
                await JavaHelper.EnsureJava17InstalledAsync(status =>
                {
                    // Update UI safely
                    this.Invoke(() => lblJavaStatus.Text = status);
                });

                chkboxJava.Checked = JavaHelper.IsJava17Installed();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on CheckJavaAsync(): {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task YouTubeVerificationAsync()
        {
            try
            {
                var verified = _youtubeHelper.CheckYoutubeRefreshToken();
                UpdateYouTubeUI(verified);

                if (!verified)
                {
                    // google device code 
                    _ = Task.Run(async () =>
                    {
                        await _youtubeHelper.StartWorkerAndMonitorDeviceCodeAsync(
                            onCodeFound: code =>
                            {
                                // Update the UI safely
                                this.Invoke(() =>
                                {
                                    UpdateYouTubeStatus(code, true);
                                    Clipboard.SetText(code);
                                    lblVerify.Visible = true;
                                });
                            });

                        await _youtubeHelper.MonitorLogForOAuthTokenAsync(token =>
                        {
                            _youtubeHelper.UpdateYoutubeRefreshToken(token);
                        });

                        verified = _youtubeHelper.CheckYoutubeRefreshToken();
                        UpdateYouTubeUI(verified);

                        ServiceHelper.StopWorkerService();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on YouTubeVerification() : {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateYouTubeUI(bool verified)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(UpdateYouTubeUI), verified);
                return;
            }

            if (!verified)
            {
                lblTips.Visible = true;
                UpdateYouTubeStatus("Loading...", false);
                chkboxYouTube.Checked = false;
            }
            else
            {
                chkboxYouTube.Checked = true;
                lblTips.Visible = lblYouTubeStatus.Visible = lblVerify.Visible = false;
            }
        }

        private void UpdateYouTubeStatus(string code, bool canCopy)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, bool>(UpdateYouTubeStatus), code, canCopy);
                return;
            }

            lblYouTubeStatus.Text = $"(Code : {code})" + (canCopy ? " - Click to Copy" : string.Empty);
        }

        private void UpdateServiceStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateServiceStatus), status);
                return;
            }

            lblServiceStatus.Text = $"({status})";
        }

        private void LoadSettings()
        {
            _configSettings = _settingsHelper.Load();

            edtBotToken.Text = _configSettings.DiscordToken;
            edtPrefix.Text = _configSettings.Prefix.ToString();
            edtStatus.Text = _configSettings.GameStatus;
            edtClientSecret.Text = _configSettings.ClientID;
        }

        private async void FormConfiguration_Load(object sender, EventArgs e)
        {
            await CheckJavaAsync();
            await YouTubeVerificationAsync();

            if (chkboxJava.Checked && chkboxYouTube.Checked)
                UpdateServiceStatus(ServiceHelper.IsWorkerRunning() ? "Running" : "Stopped");

            LoadSettings();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void lblVerify_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.google.com/device",
                UseShellExecute = true
            });
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!ServiceHelper.IsWorkerRunning())
                ServiceHelper.StartWorkerService(_workerFolder);

            UpdateServiceStatus("Running");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (ServiceHelper.IsWorkerRunning())
                ServiceHelper.StopWorkerService();

            UpdateServiceStatus("Stopped");
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            _configSettings.DiscordToken = edtBotToken.Text;
            _configSettings.ClientID = edtClientSecret.Text;
            _configSettings.GameStatus = edtStatus.Text;

            if (!string.IsNullOrEmpty(edtPrefix.Text))
                _configSettings.Prefix = edtPrefix.Text[0];

            try
            {
                _settingsHelper.Save(_configSettings);
                MessageBox.Show("Settings saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void lblYouTubeStatus_Click(object sender, EventArgs e)
        {
            var match = Regex.Match(lblYouTubeStatus.Text, @"Code\s*:\s*(.*?)\)");
            if (match.Success)
            {
                string code = match.Groups[1].Value.Trim();
                Clipboard.SetText(code);
            }
        }

        private void lblInvite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var clientID = edtClientSecret.Text.Trim();

            if (string.IsNullOrWhiteSpace(clientID))
            {
                MessageBox.Show("Application ID is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var url = string.Format(Const.DiscordInviteURL, Uri.EscapeDataString(clientID));
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void btnPasteToken_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText()) return;

            var clipboardText = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(clipboardText) || clipboardText.Length > 200) return;

            edtBotToken.Text = clipboardText;
        }

        private void btnPasteSecret_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText()) return;

            var clipboardText = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(clipboardText) || clipboardText.Length > 200) return;

            edtClientSecret.Text = clipboardText;
        }

        private void FormConfiguration_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Open the URL in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = Const.HelpURL,
                UseShellExecute = true
            });
        }
    }
}
