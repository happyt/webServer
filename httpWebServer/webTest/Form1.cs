using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using httpWebServer;


namespace webTest
{
    public partial class Form1 : Form
    {

        /// <summary>
        /// For browser status page
        /// </summary>
        private HttpWebServer http;

        /// <summary>
        /// Reply to update form
        /// </summary>
        /// <param name="text"></param>
        public delegate void SetTextCallback(string text);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                StopStartWeb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                    http.portNumber = "80";
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
        /// </summary>
        /// <param name="strCommand"></param>
        private void ParseCommand(string strCommand)
        {

        }

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
