using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace httpWebServer
{
    /// <summary>
    /// To allow messages to be sent to the host app
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void clientEventHandler(object sender, HttpCommandArgs e);

    public class HttpWebServer
    {
        // The version of the web server
        public string version = "1.0";

        /// <summary>
        /// Some pages for reference chat
        /// http://stackoverflow.com/questions/427326/httplistener-server-header-c
        /// http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx
        /// http://www.dreamincode.net/forums/topic/215467-c%23-httplistener-and-threading/

       
        /// </summary>

        private HttpListener listener;
        public string portNumber = "8081";
        private const string rootPath = @"./web";
        private const string defaultPage = @"default.htm";

        //===================================================================

        // to raise the message received
        public event clientEventHandler controlMessage;

        /// <summary>
        /// Invoke the message received event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCommand(HttpCommandArgs e)
        {
            // check to see that there is at least one event handler listening
            if (controlMessage != null) controlMessage(this, e);
        }

        /// <summary>
        /// Set up the parameters and raise a Reply message event
        /// </summary>
        /// <param name="strMessage">Message to be raised </param>
        private void RaiseCommand(string strMessage)
        {
            HttpCommandArgs s = new HttpCommandArgs();
            s.m_Message = strMessage;
            OnCommand(s);
        }

        //===================================================================

        /// <summary>
        /// Creates the http server and opens listener
        /// </summary>
        public void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://*:" + portNumber + "/");
            listener.Start();
            listener.BeginGetContext(ProcessRequest, listener);
            Console.WriteLine("Connection Started");
        }

        /// <summary>
        /// Stops listening
        /// </summary>
        public void Stop()
        {
            listener.Stop();
        }

        /// <summary>
        /// Process the web request
        /// </summary>
        /// <param name="result"></param>
        private void ProcessRequest(IAsyncResult result)
        {
            string responseString = "";
            string responseType = "text/html";
            string filetype = "";
            bool binary = false;
            byte[] buffer;
            int pos;

            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            string path = rootPath + request.RawUrl;
            if (path == rootPath + "/") path += "default.htm";

            filetype = Path.GetExtension(path);
            switch (filetype)
            {
                case (".png"):
                case (".jpg"):
                case (".jpeg"):
                case (".gif"):
                case (".ico"):
                    responseType = "image/" + filetype.Substring(1);    // leave off the decimal point
                    binary = true;
                    break;

                case (".htm"):
                case (".html"):
                case (".css"):
                    responseType = "text/html";
                    binary = false;
                    break;

                case (".js"):
                    responseType = "application/javascript";
                    binary = false;
                    break;

                case (".xml"):
                    responseType = "text/" + filetype.Substring(1);    // leave off the decimal point
                    binary = false;
                    break;

                case (".json"):
                    responseType = "application/json";
                    binary = false;
                    break;

                default:
                    break;
            }

            HttpListenerResponse response = context.Response;
            if (File.Exists(path))
            {
                if (binary)
                {
                    //Open image as byte stream to send to requestor
                    FileInfo fInfo = new FileInfo(path);
                    long numBytes = fInfo.Length;
                    FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    BinaryReader binaryReader = new BinaryReader(fStream);
                    buffer = binaryReader.ReadBytes((int)numBytes);
                }
                else
                {
                    StreamReader streamReader = new StreamReader(path);
                    responseString = streamReader.ReadToEnd();
                    buffer = Encoding.UTF8.GetBytes(responseString);
                }
            }
            else if ((pos = path.IndexOf("cmd=", 0)) > 0)
            {
                string command = path.Substring(pos + 4);
                RaiseCommand(command);
                responseString = "<html>OK</html>";
                buffer = Encoding.UTF8.GetBytes(responseString);
            }
            else
            {
                responseString = "<html>Unknown file: " + request.RawUrl + "</html>";
                RaiseCommand("Unknown");
                buffer = Encoding.UTF8.GetBytes(responseString);
            }
            
            response.ContentType = responseType;
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            listener.BeginGetContext(ProcessRequest, listener);
        }
    }
}
