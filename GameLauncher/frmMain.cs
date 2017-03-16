using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace GameLauncher
{
    public partial class frmMain : Form
    {
        iniParser options;
        public frmMain()
        {
            InitializeComponent();
            options = new iniParser("launcher.ini");
            Image i = Image.FromFile("logo.png");
            picImage.Image = i;
        }

        private String getResponse(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = "GameLauncher";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        private void cmdLaunch_Click(object sender, EventArgs e)
        {
            cmdLaunch.Enabled = false;
            cmdLaunch.Text = "Launching...";
            lblStatus.Text = "Checking for latest version...";
            Application.DoEvents();
            String releasejson = getResponse("https://api.github.com/repos/" + options.getValue("Launcher", "repo") + "/releases");
            dynamic release = JToken.Parse(releasejson);
            String asseturl = release[0].assets[0].url.ToString();

            String assetjson = getResponse(asseturl);
            dynamic asset = JToken.Parse(assetjson);
            String downloadurl = asset.browser_download_url.ToString();
            DateTime updatetime = DateTime.Parse(asset.updated_at.ToString());

            DateTime currenttime = DateTime.Parse(options.getValue("Game", "update"));

            if (DateTime.Compare(currenttime, updatetime) < 0)
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                client.Headers.Add("User-Agent", "GameLauncher");
                client.DownloadFileAsync(new Uri(downloadurl), "game.exe");

                options.setValue("Game", "update", asset.updated_at.ToString());
            } else
            {
                Process.Start("game.exe");
                Application.Exit();
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Process.Start("game.exe");
            options.Save("launcher.ini");
            Application.Exit();
            //throw new NotImplementedException();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = e.BytesReceived;
            double totalBytes = e.TotalBytesToReceive;
            double percentage = bytesIn / totalBytes * 100;
            progressBar1.Value = (int)percentage;

            lblStatus.Text = String.Format("{0}/{1} bytes downloaded...", bytesIn, totalBytes);
        }
    }
}
