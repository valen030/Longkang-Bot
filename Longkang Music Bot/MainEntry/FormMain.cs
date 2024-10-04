using Microsoft.VisualBasic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LKGMusicBot.MainEntry
{
    public class ButtonActionString
    {
        public const string DisconnectActionString = "Disconnect";
        public const string ConnectActionString = "Connect";
    }

    public partial class FormMain : Form
    {
        private MusicBot m_MusicBot;

        public FormMain(MusicBot mBot)
        {
            InitializeComponent();

            m_MusicBot = mBot;
        }

        public void SetConnectionStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetConnectionStatus), new object[] { status });
                return;
            }

            lblStatus.Text = status;

            if (status.Equals(ConnectionStatusString.DisconnectedString))
            {
                lblStatus.BackColor = Color.Red;
                btnStart.Text = ButtonActionString.ConnectActionString;
            }
            else if (status.Equals(ConnectionStatusString.ConnectingString) || status.Equals(ConnectionStatusString.DisconnectingString))
            {
                lblStatus.BackColor = Color.Yellow;
                btnStart.Text = "Progressing...";
            }
            else if (status.Equals(ConnectionStatusString.ConnectedString))
            {
                lblStatus.BackColor = Color.Green;
                btnStart.Text = ButtonActionString.DisconnectActionString;
            }
        }

        public void SetConsoleText(string log)
        {
            Console.WriteLine(log);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Program.Run();
        }
    }
}
