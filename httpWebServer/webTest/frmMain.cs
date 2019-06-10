using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using httpWebServer;

namespace webTest
{
    public partial class frmMain : Form
    {
        /// <summary>
        /// For browser status page
        /// </summary>
        private HttpWebServer http;
        Dictionary<string, string> @params = new Dictionary<string, string>();

        private List<score> scoring = new List<score>();

        /// <summary>
        /// Reply to update form
        /// </summary>
        /// <param name="text"></param>
        public delegate void SetTextCallback(string text);

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            string strAppName = Assembly.GetExecutingAssembly().GetName().Name.ToString();
            string strAppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = String.Format("{0} v{1}", strAppName, strAppVersion);

            try
            {
                StopStartWeb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            scoring.Add(new score("NOT FOUND", "blank", 0));
        }

        //======================================================================

        /// <summary>
        /// Handle a button click to start/stop the service
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartStop_Click(object sender, EventArgs e)
        {
            StopStartWeb();

            MessageBox.Show("After...");

        }
 
        /// <summary>
        /// Start/Stop the http service
        /// </summary>
        private void StopStartWeb()
        {
            try
            {
                if (http == null)
                {
                    http = new HttpWebServer();
                    http.portNumber = "8081";
                    http.DataHandler = http_ReturnData;
                    http.CommandHandler = http_ProcessCommand;
                    string[] commands = { "abc", "def" };
                    http.commandList = commands;         // available commands
                    http.Start();
                    btnStartStop.Text = "Stop http";
                }
                else
                {
                    http.Stop();
                    btnStartStop.Text = "Start http";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Closing http error: " + ex.Message);
            }
        }

        public void SplitParameters(string paramString)
        {
            string[] parameters = paramString.Split('&');
            @params.Clear();

            if (parameters.Count() > 0)
            {
                foreach (string s in parameters)
                {
                    string[] a = s.Split('=');
                    if (a.Count() == 2)
                    {
                        @params.Add(a[0], a[1]);
                    }
                }
            }
        }

        //=======================================================================
        //
        // Process an application page
        //
        //=======================================================================

        // TODO need some try catch blocks here!

        //SS.MessageHandler = ProcessMessage;
        //    SS.StatusMessage += new SocketEventHandler(StatusEvent);
        //    SS.ReplyMessage += new SocketEventHandler(ReplyEvent);

        private string http_ReturnData(string webMethod, string rawUrl)
        {
            string processedMessage = rawUrl;
            SetStatus("Received:" + rawUrl);
            string outputString = "";
            string fileString = rawUrl;    // default
            string paramString = "";

            int n = rawUrl.IndexOf("?");
            if (n > 0)
            {
                fileString = rawUrl.Substring(0, n).Replace("//", String.Empty);
                if (n > rawUrl.Length)
                {
                    paramString = rawUrl.Substring(n + 1);
                }
            }
            // get parameters, if there, or empty string
            SplitParameters(paramString);

            int chosen = 0;
            bool notFound = true;

            // Simple example storing and retrieving score items from a list

            if (@params.ContainsKey("name"))            // example code for "abc.json?name=Smith
            {
                // find the entry, return the scores
                int p = 0;
                string searchName = @params["name"];

                while (p < scoring.Count() && notFound)
                {

                    if (scoring[p].name == searchName)
                    {
                        notFound = false;
                        chosen = p;
                    }
                    else
                    {
                        p++;
                    }
                }
            }

            int v = 0;
            if (notFound)
            {
                // add an entry , usually would use POST, but just add if don't find it

                if (@params.Count() > 2 && @params.ContainsKey("value") && @params.ContainsKey("scores"))
                {
                    int.TryParse(@params["value"], out v);
                    scoring.Add(new score(@params["name"], @params["scores"], v));
                    chosen = scoring.Count() - 1;
                }
            }
            else if (@params.Count() > 2 && @params.ContainsKey("value") && @params.ContainsKey("scores"))
            {
                // overwrite current data, again normally would use POST
                int.TryParse(@params["value"], out v);
                scoring[chosen].scores = @params["scores"];
                scoring[chosen].value = v;
            }

            //
            // example code...xml strings from class
            //
            if (fileString.Contains(".xml"))
            {
                processedMessage = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><player>{replace this}</player>";

                outputString = "<name>" + scoring[chosen].name + "</name>";
                outputString += "<scores>" + scoring[chosen].scores + "</scores>";
                outputString += "<value>" + scoring[chosen].value.ToString() + "</value>";

                processedMessage = processedMessage.Replace("{replace this}", outputString);
            }

            //
            // example code...simple JSON strings from class
            //
            else if (fileString.Contains(".json"))
            {
                processedMessage = "{{replace this}}";

                outputString = "\"name\":\"" + scoring[chosen].name + "\",";
                outputString += "\"scores\":\"" + scoring[chosen].scores + "\",";
                outputString += "\"value\":" + scoring[chosen].value.ToString();

                processedMessage = processedMessage.Replace("{replace this}", outputString);
            }

            //
            // example code...basic strings
            //

            else
            {
                processedMessage = "<html>Something processed: {replace this}</html>";
                foreach (var s in @params.Keys)
                {
                    outputString += s + ":" + @params[s] + ",";
                }
                processedMessage = processedMessage.Replace("{replace this}", outputString);
            }

            //
            // send reply
            //

            SetStatus("Process Reply:" + processedMessage);
            return processedMessage;
        }

        //=======================================================================
        //
        // Process a command request
        //
        //=======================================================================

        // TODO need some try catch blocks here!

        //SS.MessageHandler = ProcessMessage;
        //    SS.StatusMessage += new SocketEventHandler(StatusEvent);
        //    SS.ReplyMessage += new SocketEventHandler(ReplyEvent);

        private string http_ProcessCommand(string webMethod, string message)
        {
            string processedCommand = "OK";
            SetStatus("Received cmd:" + message);
            string outputString = "";

            // =========================================
            // process the application command type here
            // =========================================
            //
            string fileString = message;
            int n = message.IndexOf("?");
            if (n > 0)
            {
                fileString = message.Substring(0, n).Replace("//", String.Empty);
            }

            // get parameters, if there, or empty string
            string paramString = message.Substring(n + 1);
            SplitParameters(paramString);

            // Switch on the different commands here
            // Send to other devices etc

            //
            // send reply
            //

            SetStatus("Process Reply:" + processedCommand);
            return processedCommand;
        }

        //=======================================================================
        //
        //  Receive a command event
        //
        //=======================================================================

        /// <summary>
        /// Event handler for returned messages
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void http_controlMessage(object sender, HttpCommandArgs e)
        {
            try
            {
                SetStatus(e.m_Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Deal with the http command
            ParseCommand(e.m_Message);
        }

        /// <summary>
        /// Do any work for a command in this section
        /// No reply is returned to the web page, the response is set at startup
        /// </summary>
        /// <param name="strCommand"></param>
        private void ParseCommand(string strCommand)
        {

        }

        //=======================================================================
        //
        // Monitor the status log
        //
        //=======================================================================

        /// <summary>
        /// Display a reply from the server
        /// </summary>
        /// <param name="text"></param>
        private void SetStatus(string text)
        {
            string s = text;

            if (this.labStatus.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetStatus);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.labStatus.Text = "<" + s + ">";
                LogMessage(s);
            }
        }

        /// <summary>
        /// For a logging section
        /// just put into a scrolling list for now
        /// </summary>
        /// <param name="s"></param>
        private void LogMessage(string s)
        {
            this.txtHistory.Text += s + Environment.NewLine;
            this.txtHistory.SelectionStart = txtHistory.Text.Length;
            this.txtHistory.ScrollToCaret();
        }
    }
}
