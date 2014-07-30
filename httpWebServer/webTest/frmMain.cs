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
                    http.MessageHandler = http_ProcessMessage;
                    http.Start();
                    btnStartStop.Text = "Stop http";
                    http.controlMessage += new clientEventHandler(http_controlMessage);
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

        //=======================================================================
        //
        // Process an application page
        //
        //=======================================================================

        // TODO need some try catch blocks here!

        //SS.MessageHandler = ProcessMessage;
        //    SS.StatusMessage += new SocketEventHandler(StatusEvent);
        //    SS.ReplyMessage += new SocketEventHandler(ReplyEvent);

        private string http_ProcessMessage(string webMethod, string message)
        {
            string processedMessage = message;
            SetStatus("Received:" + message);

            // ========================================
            // process the application file type here
            // ========================================
            //
            int n = message.IndexOf("?");
            string fileString = message.Substring(0, n).Replace("//",String.Empty);

            // get parameters, if there, or empty string
            string paramString = message.Substring(n + 1);
            string[] parameters = paramString.Split('&');
            string outputString = "";

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (parameters.Count() > 0)
            {
                foreach (string s in parameters)
                {
                    string[] a = s.Split('=');
                    if (a.Count() == 2)
                    {
                        dictionary.Add(a[0], a[1]);
                    }
                }
            }

            int chosen = 0;
            bool notFound = true;

            if (dictionary.ContainsKey("name")) {
                // find the entry
                int p = 0;
                string searchName = dictionary["name"];

                while (p < scoring.Count() && notFound) {

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
                
                if (parameters.Count() > 2 && dictionary.ContainsKey("value") && dictionary.ContainsKey("scores")) 
                {
                    int.TryParse(dictionary["value"], out v);
                    scoring.Add(new score(dictionary["name"], dictionary["scores"], v));
                    chosen = scoring.Count()-1;
                }
            }
            else if (parameters.Count() > 2 && dictionary.ContainsKey("value") && dictionary.ContainsKey("scores"))
            {
                // overwrite current data, again normally would use POST
                int.TryParse(dictionary["value"], out v);
                scoring[chosen].scores =  dictionary["scores"];
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
            // example code...JSON strings from class
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
                for (int i = 0; i < parameters.Count(); i++)
                {
                    outputString += parameters[i] + ":";
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
        //  Receive a command message
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
