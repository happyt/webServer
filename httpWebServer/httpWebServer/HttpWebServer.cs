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
        /// <summary>
        /// Simple web server dll, to be self hosted in a parent application
        /// Full source at https://github.com/happyt/webServer
        /// 
        /// Some pages for reference chat
        /// http://stackoverflow.com/questions/427326/httplistener-server-header-c
        /// http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx
        /// http://www.dreamincode.net/forums/topic/215467-c%23-httplistener-and-threading/

       
        /// </summary>
        private string info = "Full source at https://github.com/happyt/webServer";

        private HttpListener listener;
        public string portNumber = "8081";

        // file type of the processed pages
        public string appFileType = ".lua";
        
        // if no file found, search for this string
        // raise an event with the command string
        // just reply with the OK reply below
        public string appCommand = "cmd=";
        public string appCmdResponse = "<html>OK</html>";
        public string appErrorResponse = "<html>No application handler!</html>";
        
        private const string rootPath = @"./web";
        private const string defaultPage = @"default.htm";

        //===================================================================

        // declare a Delegate
        public delegate string ProcessMessage(string type, string message);
        // use a instance of the declared Delegate
        public ProcessMessage MessageHandler;

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
            string pageString = "";
            string responseType = "text/html";
            string filetype = "";
            bool binary = false;
            byte[] buffer;
            int pos;
            StreamReader streamReader;

            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;

            string path = rootPath + request.RawUrl;
            string webMethod = request.HttpMethod;
            if (path == rootPath + "/") path += defaultPage;

            filetype = Path.GetExtension(path);
            // take off any parameters at the end of the url
            filetype = StripParameters(filetype);

            switch (filetype)
            {
                case (".png"):
                case (".jpg"):
                case (".jpeg"):
                case (".gif"):
                case (".tiff"):
                    responseType = "image/" + filetype.Substring(1);    // leave off the decimal point
                    binary = true;
                    break;

                case (".htm"):
                case (".html"):
                case (".htmls"):
                    responseType = "text/html";
                    binary = false;
                    break;

                case (".js"):
                    responseType = "application/javascript";
                    binary = false;
                    break;

                case (".ico"):
                    responseType = "image/x-icon";
                    binary = false;
                    break;

                case (".xml"):
                case (".txt"):
                case (".css"):
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
                    binaryReader.Close();
                }
                else
                {
                    // read text file
                    streamReader = new StreamReader(path);
                    pageString = streamReader.ReadToEnd();

                    if (filetype == appFileType)
                    {
                        // treat as an application filetype, to be processed by the host application
                        // cf .php, .cf, .asmx, .lua etc
                        // should return a text for the response string

                        // Deal with the message                    
                        if (MessageHandler != null)
                        {
                            responseString = MessageHandler(webMethod, pageString);
                        }
                        else
                        {
                            responseString = appErrorResponse;
                        }
                    }
                    else
                    {
                        responseString = pageString;

                    }
                    buffer = Encoding.UTF8.GetBytes(responseString);
                    streamReader.Close();
                }
            }

            // file doesn't exist
                // check for json, xml requests
            else if (filetype == ".json" || filetype == ".xml" || filetype == ".txt")
            {
                // Deal with the message                    
                if (MessageHandler != null)
                {
                    responseString = MessageHandler(webMethod, request.RawUrl);
                }
                else
                {
                    responseString = appErrorResponse;
                }
                buffer = Encoding.UTF8.GetBytes(responseString);
            }

            // check for a command
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

        /// <summary>
        /// Need to take off any string after a question mark character
        /// </summary>
        /// <param name="filetype"></param>
        /// <returns></returns>
        private string StripParameters(string filetype)
        {
            string stripped = filetype;
            if (stripped.IndexOf("?") != -1)
            {
                int n = stripped.IndexOf("?");
                stripped = stripped.Substring(0, n);
            }
            return stripped;
        }
    }
}
