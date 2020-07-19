using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Web;
using ZLibNet;
using ME3PlayerDataEditor.PlayerProfile;
namespace ME3Server_WV
{
    public static class ME3Server
    {
        private static readonly object _sync = new object();
        private static string loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";
        private static bool exitnow = false;
        public static bool isMITM = false;
        public static int NAT_Type;
        public static long TimeOutLimit;
        public static int RWTimeout;
        public static bool bRecordPlayerSettings = false;
        public static List<string> importKeys = null;
        public static List<string> importValues = null;
        public static bool silentStart;
        public static bool silentExit;
        public static bool ignoreTLKLangCode = false;
        public static void Start()
        {
            if (File.Exists(Logger.mainlogpath))
                File.Delete(Logger.mainlogpath);
            Logger.Log("ME3 Private Server Emulator by Warranty Voider", Color.DarkBlue);
            Logger.Log("Program: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), Color.DarkBlue);
            Logger.Log("OSVersion: " + Environment.OSVersion.ToString(), Color.DarkBlue);
            Logger.Log("Starting...", Color.Black);
            Config.Load();
            Logger.DeleteLogs();
            LoadInitialConfig();
            PerformSSL3Checks();
            tTick = new Thread(threadTickListener);
            tTick.Start();
            Application.DoEvents();
            tTelemetry = new Thread(threadTelemetryListener);
            tTelemetry.Start();
            Application.DoEvents();
            tHttp = new Thread(threadHttpListener);
            tHttp.Start();
            Application.DoEvents();
            tRedirector = new Thread(threadRedirectorListener);
            tRedirector.Start();
            Application.DoEvents();
            tMainServer = new Thread(threadMainServerListener);
            tMainServer.Start();
            if (!Config.GetBoolean("AlwaysSkipHostsCheck") && !silentStart && Frontend.IsRedirectionActive())
            {
                string msg = "PSE has detected valid redirection entries in the system's hosts file.\n\n";
                msg += "Would you like to deactivate redirection now?";
                if (MessageBox.Show(msg, "Startup check", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    Frontend.DeactivateRedirection();
            }
            if (isMITM)
            {
                Logger.Log("Man-in-the-middle (MITM) mode is enabled (command line argument).", Color.Black);
                Frontend.UpdateMITMMenuState();
            }
        }
        public static void LoadInitialConfig()
        {
            Logger.Log("Loading Config...", Color.Black);
            Logger.LogLevel = Convert.ToInt32(Config.FindEntry("LogLevel"));
            Logger.Log(" Log Level = " + Logger.LogLevel, Color.Black);
            Frontend.UpdateLogLevelMenu();
            NAT_Type = Convert.ToInt32(Config.FindEntry("NATType"));
            Logger.Log(" NAT Type = " + NAT_Type, Color.Black);
            TimeOutLimit = Convert.ToInt32(Config.FindEntry("TimeOutMs"));
            Logger.Log(" Time Out Limit = " + TimeOutLimit + "ms", Color.Black);
            RWTimeout = Convert.ToInt32(Config.FindEntry("RWTimeout"));
            Logger.Log(" Read/Write Time Out Limit = " + RWTimeout + "ms", Color.Black);
            Logger.Log(" Bind IP = " + Config.FindEntry("IP"), Color.Black);
            Logger.Log(" Redirect IP = " + Config.FindEntry("RedirectIP"), Color.Black);
            Logger.Log(" MITM Target IP = " + Config.FindEntry("TargetIP"), Color.Black);
            Logger.Log(" Live BINI = " + GetLiveBINI().Substring(loc.Length), Color.Black);
            ignoreTLKLangCode = Config.GetBoolean("IgnoreTLKLanguageCode");
            if(ignoreTLKLangCode)
                Logger.Log(" Live TLK = " + GetLiveTLK().Substring(loc.Length) + " (ignoring language code)", Color.Black);
            else
                Logger.Log(" Default TLK = " + GetLiveTLK().Substring(loc.Length), Color.Black);
            Logger.Log("Configuration loaded", Color.Black);
        }

        public static void PerformSSL3Checks()
        {
            if (!SSL3SupportCheck.CheckCipherSuites())
            {
                if (!silentStart)
                {
                    MessageBox.Show("Cipher suites used internally by Mass Effect 3 to communicate with a remote server currently aren't enabled for use by Schannel provider.\n" +
                        "\nPSE will be unable to accept connections from the game if Schannel is not allowed to use those cipher suites.\n" +
                        "\nPSE will attempt to enable them right now.", "Cipher suites", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (SSL3SupportCheck.EnableCipherSuites())
                    Logger.Log("Cipher suites: successfully enabled by PSE.", Color.DarkGreen);
                else
                    Logger.Log("Cipher suites: attempt to enable by PSE has failed.", Color.DarkRed);
            }
            else
            {
                Logger.Log("Cipher suites: verification OK.", Color.Black);
            }

            var winver = Environment.OSVersion.Version;
            if (winver.Build < 19041)
                return;

            int st = SSL3SupportCheck.GetSSL3ServerStatus();
            if (st == 1)
            {
                Logger.Log("SSL3 Server: enabled - registry verification OK.", Color.Black);
                return;
            }
            if (!silentStart)
            {
                MessageBox.Show("Current instance of Windows has been detected as being Windows 10 v2004 (build 19041) or later.\n" +
                  "\nSSL3 server support under Schannel is required by PSE but is currently disabled or missing from Windows registry.\n" +
                  "\nPSE will attempt to add/change the corresponding registry entry.", "SSL3 server check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            if (SSL3SupportCheck.EnableSSL3Server())
                Logger.Log("SSL3 Server: successfully enabled by PSE.", Color.DarkGreen);
            else
                Logger.Log("SSL3 Server: attempt to enable by PSE has failed.", Color.DarkRed);
        }

        public static void Stop()
        {
            exitnow = true;
            if (tRedirector != null && tRedirector.IsAlive)
            {
                RedirectorListener.Stop();
                tRedirector.Abort();
            }
            if (tMainServer != null && tMainServer.IsAlive)
            {
                MainServerListener.Stop();
                tMainServer.Abort();
            }
            if (tTelemetry != null && tTelemetry.IsAlive)
            {
                TelemetryListener.Stop();
                tTelemetry.Abort();
            }
            if (tHttp != null && tHttp.IsAlive)
            {
                HttpListener.Stop();
                tHttp.Abort();
            }
            if (tTick != null && tTick.IsAlive)
            {
                TickListener.Stop();
                tTick.Abort();
            }
            if (!Config.GetBoolean("AlwaysSkipHostsCheck") && !silentExit && Frontend.IsRedirectionActive())
            {
                string msg = "You're leaving PSE, but there are valid redirection entries in the system's hosts file.\n\n";
                msg += "Would you like to deactivate redirection before exiting?";
                if (MessageBox.Show(msg, "Exit check", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    Frontend.DeactivateRedirection();
            }
        }

#region TelemetryHandler
        public static TcpListener TelemetryListener;
        public static Thread tTelemetry;
        public struct TelemetryHandlerStruct
        {
            public Stream stream;
            public int ID;
            public TcpClient tcpClient;
        }
        public static void threadTelemetryListener(object objs)
        {
            Logger.Log("[Telemetry Listener] Starting...", Color.Black);
            try
            {
                string IP = Config.FindEntry("IP");
                if (IP == "")
                    TelemetryListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 9988);
                else
                    TelemetryListener = new TcpListener(IPAddress.Parse(IP), 9988);
                TelemetryListener.Start();
                Logger.Log("[Telemetry Listener] Started listening on " + EndpointToString(TelemetryListener.LocalEndpoint), Color.Black);
                int counter = 0;
                while (true)
                {
                    TcpClient tcpClient = TelemetryListener.AcceptTcpClient();
                    Logger.Log("[Telemetry Listener] New client connected", Color.DarkGreen);
                    Stream clientStream = tcpClient.GetStream();
                    Thread tHandler = new Thread(threadTelemetryHandler);
                    TelemetryHandlerStruct h = new TelemetryHandlerStruct();
                    h.stream = clientStream;
                    h.ID = counter++;
                    h.tcpClient = tcpClient;
                    tHandler.Start(h);
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Telemetry Listener] Crashed:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void threadTelemetryHandler(object objs)
        {
            TelemetryHandlerStruct h = (TelemetryHandlerStruct)objs;
            Logger.Log("[Telemetry Handler " + h.ID + "] Client handler started", Color.Black);
            NetworkStream clientStream = (NetworkStream)h.stream;
            try
            {
                int counter = 0;
                while (true && !exitnow)
                {
                    byte[] buff = ReadContent(clientStream);
                    if (buff.Length != 0)
                    {                        
                        File.WriteAllBytes(loc + "logs\\" + (counter++) + "Telemetry.bin", buff);
                        string content = "";
                        for (int i = 0; i < buff.Length - 4; i++)
                            if (buff[i] == 0x54 &&
                                buff[i + 1] == 0x4C &&
                                buff[i + 2] == 0x4D &&
                                buff[i + 3] == 0x33)
                                for (int j = i + 4; j < buff.Length - 4; j++)
                                    if (buff[j] == 0x41 &&
                                        buff[j + 1] == 0x55 &&
                                        buff[j + 2] == 0x54 &&
                                        buff[j + 3] == 0x48)
                                    {
                                        byte[] buff2 = new byte[j - (i + 5)];
                                        for (int k = i + 5; k < j; k++)
                                            buff2[k - (i + 5)] = buff[k];
                                        byte[] buff3 = DecodeTLM3Line(buff2);
                                        foreach (byte b in buff3)
                                            content += (char)b;
                                    }
                        Logger.Log("[Telemetry Handler " + h.ID + "] Received data, len = " + buff.Length, Color.Blue);
                        Logger.Log("[Telemetry Handler " + h.ID + "] Content :\n" + buff.Length + content.Replace("/", "\n"), Color.Gray, 5);
                    }
                    if (!SocketConnected(h.tcpClient.Client))
                    {
                        Logger.Log("[Telemetry Handler " + h.ID + "] Client handler stopped: disconnection", Color.Black);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Telemetry Handler " + h.ID + "] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 & part2)
                return false;
            else
                return true;
        }
#endregion

#region HTTPHandler
        public static TcpListener HttpListener;
        public static Thread tHttp;
        public struct HttpHandlerStruct
        {
            public NetworkStream stream;
            public int ID;
        }
        public static void threadHttpListener(object objs)
        {
            Logger.Log("[Http Listener] Starting...", Color.Black);
            try
            {
                string IP = Config.FindEntry("IP");
                if (IP == "")
                    HttpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 80);
                else
                    HttpListener = new TcpListener(IPAddress.Parse(IP), 80);
                HttpListener.Start();
                Logger.Log("[Http Listener] Started listening on " + EndpointToString(HttpListener.LocalEndpoint), Color.Black);
                int counter = 0;
                while (true && !exitnow)
                {
                    TcpClient tcpClient = HttpListener.AcceptTcpClient();
                    Thread tHttp = new Thread(threadHttpHandler);
                    HttpHandlerStruct h = new HttpHandlerStruct();
                    h.stream = (NetworkStream)tcpClient.GetStream();;
                    h.ID = counter++;
                    tHttp.Start(h);
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Http Listener] Crashed:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void threadHttpHandler(object objs)
        {            
            HttpHandlerStruct h = (HttpHandlerStruct)objs;
            Logger.Log("[Http Client Handler " + h.ID + "] got request " + EndpointToString(HttpListener.LocalEndpoint), Color.Green, 5);
            NetworkStream clientStream = h.stream;
            try
            {
                while (true && !exitnow)
                {
                    byte[] buff = ReadContentHttp(clientStream);
                    if (buff.Length != 0)
                    {
                        string res = "";
                        string content = "";
                        foreach (byte b in buff)
                            content += (char)b;
                        string[] parts = content.Split(' ');
                        string get = "";
                        if (parts.Length >= 2 && parts[0] == "GET")
                        {
                            get = parts[1];
                            if (get.StartsWith("/wal/masseffect-gaw-pc/"))
                            {
                                HandleGaW(get.Substring(23), clientStream);
                                clientStream.Close();
                                return;
                            }
                            res = "GET " + get;
                            string filename = Path.GetFileName(get);
                            string fullfilename = Path.Combine(loc, "http\\" + filename);
                            if (!File.Exists(fullfilename))
                            {
                                Logger.Log("[Http Handler] Request failed: " + res + "\nFile not found: " + filename, Color.Red);
                                HandleGaW_SendResponseToClient(clientStream, CreateHttpHeader(0, 404));
                                clientStream.Close();
                                return;
                            }
                            byte[] resbuff = File.ReadAllBytes(fullfilename);
                            string httpheader = "HTTP/1.1 200 OK\r\nServer: Apache-Coyote/1.1\r\nAccept-Ranges: bytes\r\nETag: W/\"524416-1333666807000\"\r\nLast-Modified: Thu, 05 Apr 2012 23:00:07 GMT\r\nContent-Length: ";
                            httpheader += resbuff.Length + "\r\nDate: Tue, 05 Aug 2014 10:14:13 GMT\r\nConnection: close\r\n\r\n";
                            List<byte> resbuff2 = new List<byte>();
                            foreach (char c in httpheader)
                                resbuff2.Add((byte)c);
                            resbuff2.AddRange(resbuff);
                            clientStream.Write(resbuff2.ToArray(), 0, resbuff2.Count);
                            clientStream.Flush();
                            Logger.Log("[Http Handler] Request OK: " + res, Color.DarkBlue);
                            clientStream.Close();
                            return;
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Http Handler] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void HandleGaW(string request, NetworkStream clientStream)
        {
            Logger.Log("[Http Handler][GaW] Request: " + request, Color.DarkGoldenrod, 3);
            string[] s = request.Split('/');
            if (s.Length < 2)
            {
                Logger.Log("[Http Handler][GaW] Bad request: " + request, Color.DarkBlue);
                string badreqResponse = CreateHttpHeader(0, 400);
                HandleGaW_SendResponseToClient(clientStream, badreqResponse);
                return;
            }
            string playername = "";
            bool handled = false;
            switch (s[0])
            {
                case "authentication":
                    if (s[1].StartsWith("sharedTokenLogin"))
                    {
                        string authResponse = GetResponseGaWAuthentication(request, out playername);
                        HandleGaW_SendResponseToClient(clientStream, authResponse);
                        s[1] = "sharedTokenLogin";
                        handled = true;
                    }
                    break;
                case "galaxyatwar":
                    if (s[1] == "getRatings")
                    {
                        string ratingsResponse = GetResponseGaWRatings(request, out playername);
                        HandleGaW_SendResponseToClient(clientStream, ratingsResponse);
                        handled = true;
                    }
                    else if (s[1] == "increaseRatings")
                    {
                        GaWIncreaseRatings(request, out playername);
                        string ratingsResponse = GetResponseGaWRatings(request, out playername);
                        HandleGaW_SendResponseToClient(clientStream, ratingsResponse);
                        handled = true;
                    }
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                Logger.Log("[Http Handler][GaW] " + s[0] + "/" + s[1] + " - " + playername, Color.DarkBlue);
            else
            {
                Logger.Log("[Http Handler][GaW] Unsupported request: " + s[0] + "/" + s[1], Color.DarkBlue);
                string unsupResponse = CreateHttpHeader(0, 501);
                HandleGaW_SendResponseToClient(clientStream, unsupResponse); 
            }
        }
        public static void HandleGaW_SendResponseToClient(NetworkStream clientStream, string response)
        {
            List<byte> resbuff = new List<byte>();
            foreach (char c in response)
                resbuff.Add((byte)c);
            clientStream.Write(resbuff.ToArray(), 0, resbuff.Count);
            clientStream.Flush();
        }
#endregion

#region TickHandler
        public static TcpListener TickListener;
        public static Thread tTick;
        public struct TickHandlerStruct
        {
            public Stream stream;
            public int ID;
        }
        public static void threadTickListener(object objs)
        {
            Logger.Log("[Tick Listener] Starting...", Color.Black);
            try
            {
                string IP = Config.FindEntry("IP");
                if (IP == "")
                    TickListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 8999);
                else
                    TickListener = new TcpListener(IPAddress.Parse(IP), 8999);
                TickListener.Start();
                Logger.Log("[Tick Listener] Started listening on " + EndpointToString(TickListener.LocalEndpoint), Color.Black);
                int counter = 0;
                while (true)
                {
                    TcpClient tcpClient = TickListener.AcceptTcpClient();
                    Logger.Log("[Tick Listener] New client connected", Color.DarkGreen);
                    Stream clientStream = tcpClient.GetStream();
                    Thread tHandler = new Thread(threadTickHandler);
                    TickHandlerStruct h = new TickHandlerStruct();
                    h.stream = clientStream;
                    h.ID = counter++;
                    tHandler.Start(h);
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Tick Listener] Crashed:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void threadTickHandler(object objs)
        {
            TickHandlerStruct h = (TickHandlerStruct)objs;
            Logger.Log("[Tick Handler " + h.ID + "] Client handler started", Color.Black);
            Stream clientStream = h.stream;
            try
            {
                int counter = 0;
                while (true && !exitnow)
                {
                    int byteread = 0;
                    MemoryStream m = new MemoryStream();
                    while ((byteread = clientStream.ReadByte()) != -1)
                        m.WriteByte((byte)byteread);
                    if (m.Length != 0)
                    {
                        Logger.Log("[Tick Handler " + h.ID + "] Received request data, len = " + m.Length + "\n" + Blaze.HexDump(m.ToArray()), Color.Blue);
                        File.WriteAllBytes(loc + "logs\\" + (counter++) + "Tick.bin", m.ToArray());
                        clientStream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Tick Handler " + h.ID + "] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
#endregion

#region Redirector
        public static X509Certificate2 RedirectorCert = new X509Certificate2(Path.GetDirectoryName(Application.ExecutablePath) + "\\cert\\redirector.pfx", "123456");
        public static TcpListener RedirectorListener;
        public static Thread tRedirector;
        public struct RedirectorHandlerStruct
        {
            public SslStream stream;
            public int ID;
            public TcpClient tcpClient;
        }
        public static void threadRedirectorListener(object objs)
        {
            Logger.Log("[Redirector] Starting...", Color.Black);            
            try
            {
                string IP = Config.FindEntry("IP");
                if (IP == "")
                    RedirectorListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 42127);
                else
                    RedirectorListener = new TcpListener(IPAddress.Parse(IP), 42127);
                RedirectorListener.Start();
                Logger.Log("[Redirector] Started listening on " + EndpointToString(RedirectorListener.LocalEndpoint), Color.Black);
                int counter = 0;
                while (true)
                {
                    TcpClient tcpClient = RedirectorListener.AcceptTcpClient();
                    Logger.Log("[Redirector] New client connected", Color.DarkGreen);
                    SslStream clientStream = new SslStream(tcpClient.GetStream(), true);
                    clientStream.AuthenticateAsServer(RedirectorCert, false, SslProtocols.Ssl3, false);
                    Thread tHandler = new Thread(threadRedirectorClientHandler);
                    RedirectorHandlerStruct h = new RedirectorHandlerStruct();
                    h.stream = clientStream;
                    h.ID = counter++;
                    h.tcpClient = tcpClient;
                    tHandler.Start(h);                    
                }

            }
            catch (Exception e)
            {
                Logger.Log("[Redirector] Crashed:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void threadRedirectorClientHandler(object objs)
        {
            RedirectorHandlerStruct h = (RedirectorHandlerStruct)objs;
            Logger.Log("[Redirector Handler " + h.ID + "] Client handler started", Color.Black);
            SslStream clientStream = h.stream;
            try
            {
                byte[] clientRequest;
                while (clientStream.IsAuthenticated && !exitnow)
                {
                    clientRequest = ReadContentSSL(clientStream);
                    if (clientRequest.Length != 0)
                    {
                        clientStream.Flush();
                        Blaze.Packet p = Blaze.FetchAllBlazePackets(new MemoryStream(clientRequest))[0];
                        if (p.Component == 0x5 && p.Command == 0x1)
                        {
                            Logger.Log("[Redirector Handler " + h.ID + "] Send redirection to client => " + ((IPEndPoint)h.tcpClient.Client.RemoteEndPoint).ToString(), Color.Blue);
                            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
                            List<Blaze.Tdf> VALU = new List<Blaze.Tdf>();
                            VALU.Add(Blaze.TdfString.Create("HOST", Config.FindEntry("REDIHOST")));
                            VALU.Add(Blaze.TdfInteger.Create("IP\0\0", GetIPfromString(Config.FindEntry("RedirectIP"))));
                            VALU.Add(Blaze.TdfInteger.Create("PORT", ConvertHex(Config.FindEntry("REDIPORT"))));
                            Blaze.TdfUnion ADDR = Blaze.TdfUnion.Create("ADDR", (byte)ConvertHex(Config.FindEntry("REDIADDR")), Blaze.TdfStruct.Create("VALU", VALU));
                            Result.Add(ADDR);
                            Result.Add(Blaze.TdfInteger.Create("SECU", ConvertHex(Config.FindEntry("REDISECU"))));
                            Result.Add(Blaze.TdfInteger.Create("XDNS", ConvertHex(Config.FindEntry("REDIXDNS"))));
                            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result);
                            clientStream.Write(buff);
                            clientStream.Flush();
                            clientStream.Close();
                            byte[] filebuff = new byte[buff.Length + clientRequest.Length];
                            Array.Copy(clientRequest, 0, filebuff, 0, clientRequest.Length);
                            Array.Copy(buff, 0, filebuff, clientRequest.Length, buff.Length);
                            File.WriteAllBytes(loc + "logs\\Redirector_" + String.Format(@"{0:yyyy-MM-dd_HHmmss}", DateTime.Now) + "_" + h.ID.ToString("00") + ".bin", filebuff);
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Redirector Handler " + h.ID + "] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
#endregion

#region Main Server
        public static TcpListener MainServerListener;
        public static Thread tMainServer;
        public static void threadMainServerListener(object objs)
        {
            Logger.Log("[Main Server] Starting...", Color.Black);
            try
            {
                string IP = Config.FindEntry("IP");
                if (IP == "")
                    MainServerListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 14219);
                else
                    MainServerListener = new TcpListener(IPAddress.Parse(IP), 14219);
                MainServerListener.Start();
                Logger.Log("[Main Server] Started listening on " + EndpointToString(MainServerListener.LocalEndpoint), Color.Black);
                int counter = 0;
                while (true)
                {
                    TcpClient tcpClient = MainServerListener.AcceptTcpClient();
                    Logger.Log("[Main Server] New client connected", Color.DarkGreen);
                    Thread tHandler = new Thread(threadMainServerClientHandler);
                    Player.PlayerInfo player = new Player.PlayerInfo(counter++, tcpClient, tcpClient.GetStream());
                    Player.AllPlayers.Add(player);
                    tHandler.Start(player);
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server] Crashed:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void threadMainServerClientHandler(object obj)
        {
            Player.PlayerInfo player = (Player.PlayerInfo)obj;
            NetworkStream clientStream = player.ClientStream;
            Logger.Log("[Main Server Handler " + player.ID + "] Client handler started", Color.Black);
            TcpClient target = null;
            SslStream targetstream = null;
            if (isMITM)
            {
                target = new TcpClient(Config.FindEntry("TargetIP"), 14219);
                Logger.Log("[Main Server Handler " + player.ID + "] Connected to target", Color.Blue);
                targetstream = new SslStream(target.GetStream(), true, new RemoteCertificateValidationCallback(ValidateAlways), null);
                Logger.Log("[Main Server Handler " + player.ID + "] Established SSL", Color.Blue);
                targetstream.AuthenticateAsClient("383933-gosprapp396.ea.com");
                Logger.Log("[Main Server Handler " + player.ID + "] Authenticated as client", Color.Blue);
            }
            try
            {
                byte[] clientRequest;
                byte[] targetResponse;
                while (!exitnow && player.PingTimer.ElapsedMilliseconds < TimeOutLimit)
                {
                    Thread.Sleep(10);
                    clientRequest = new byte[0];
                    targetResponse = new byte[0];
                    clientRequest = ReadContent(clientStream);
                    if (clientRequest.Length >= 0xC)
                    {
                        List<Blaze.Packet> packets = Blaze.FetchAllBlazePackets(new MemoryStream(clientRequest));
                        Logger.Log("[Main Server Handler " + player.ID + "] Received request data, len = " + clientRequest.Length, Color.Blue);
                        foreach (Blaze.Packet p in packets)
                        {
                            Logger.Log("[<-][INFO] " + Blaze.PacketToDescriber(p), Color.DarkGray, 3);
                            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                            foreach (Blaze.Tdf tdf in content)
                            {
                                string value = "";
                                switch (tdf.Type)
                                {
                                    case 0:
                                        value = " 0x" + ((Blaze.TdfInteger)tdf).Value.ToString("X");
                                        break;
                                    case 1:
                                        value = " \"" + ((Blaze.TdfString)tdf).Value + "\"";
                                        break;
                                }
                                Logger.Log("[<-][INFO]  " + tdf.Label + " : " + tdf.Type + value, Color.Gray, 5);
                            }
                        }
                        Logger.DumpPacket(clientRequest, player);
                        if (!isMITM)
                            MainServerHandler(player, clientRequest);
                        else
                        {
                            targetstream.Write(clientRequest);
                            targetstream.Flush();
                        }
                        player.PingTimer.Restart();
                    }

                    if (isMITM && importValues != null)
                    {
                        List<Blaze.Tdf> listTDF;
                        int i;
                        for (i = 0; i < importKeys.Count; i++)
                        {
                            listTDF = new List<Blaze.Tdf>();
                            listTDF.Add(Blaze.TdfString.Create("DATA", importValues[i]));
                            listTDF.Add(Blaze.TdfString.Create("KEY\0", importKeys[i]));
                            listTDF.Add(Blaze.TdfInteger.Create("UID\0", 0));
                            targetstream.Write(Blaze.CreatePacket(9, 0xB, 0, 0, 0x0, listTDF));
                            targetstream.Flush();
                        }
                        Logger.Log("[Import player settings] Concluded. Packets sent: " + i, Color.Purple);
                        importKeys = null;
                        importValues = null;
                    }

                    if (isMITM)
                    {
                        targetResponse = ReadContentSSL(targetstream);
                        if (targetResponse.Length > 5 && targetResponse[0] == 0x17)
                        {
                            MemoryStream m = new MemoryStream();
                            m.Write(targetResponse, 5, targetResponse.Length - 5);
                            targetResponse = m.ToArray();
                        }
                        if (targetResponse.Length >= 0xC)
                        {
                            Logger.Log("[Main Server Handler " + player.ID + "] Received response data, len = " + targetResponse.Length, Color.Blue);
                            List<Blaze.Packet> packets = Blaze.FetchAllBlazePackets(new MemoryStream(targetResponse));
                            foreach (Blaze.Packet p in packets)
                            {
                                if (bRecordPlayerSettings && Blaze.PacketToDescriber(p).Contains("userSettingsLoadAll"))
                                    ExportUserSettings(p);
                                Logger.Log("[->][INFO] " + Blaze.PacketToDescriber(p), Color.DarkGray, 3);
                                List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                                foreach (Blaze.Tdf tdf in content)
                                    Logger.Log("[->][INFO]  " + tdf.Label + " : " + tdf.Type, Color.Gray, 5);
                            }
                            Logger.DumpPacket(targetResponse, player);
                            clientStream.Write(targetResponse, 0, targetResponse.Length);
                            clientStream.Flush();
                        }
                    }
                }
                Logger.Log("[Main Server Handler " + player.ID + "] Player Timed Out", Color.Red);
                player.SetActiveState(false);
                clientStream.Close();
                return;
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        private static void ExportUserSettings(Blaze.Packet p)
        {
            // code converted from ME3 Player Data Importer by Erik JS
            try
            {
                List<Blaze.Tdf> Fields = Blaze.ReadPacketContent(p);
                // check if SMAP is present
                Blaze.TdfDoubleList SMAP_field = null;
                foreach (Blaze.Tdf f in Fields)
                {
                    if (f.Label == "SMAP")
                    {
                        SMAP_field = (Blaze.TdfDoubleList)f;
                        break; // Exit For
                    }
                }
                // if SMAP not found, bail out
                if (SMAP_field == null)
                {
                    Logger.Log("[Record player settings] Field SMAP is missing.", Color.Red);
                    return; // Exit Try
                }
                List<string> listKeys = (List<string>)SMAP_field.List1;
                List<string> listValues = (List<string>)SMAP_field.List2;
                List<string> listLines = new List<string>();
                listLines.Add("PID=");
                listLines.Add("UID=");
                listLines.Add("AUTH=");
                listLines.Add("AUTH2=");
                listLines.Add("DSNM=Recorded online profile");
                // cycle through both lists of SMAP_field
                for (int i = 0; i < listKeys.Count; i++)
                {
                    listLines.Add(listKeys[i] + "=" + listValues[i]);
                }
                System.IO.File.WriteAllLines(loc + "RecordedPlayerSettings.txt", listLines);
                Logger.Log("[Record player settings] Dummy player file created: RecordedPlayerSettings.txt", Color.Purple);
            }
            catch (Exception ex)
            {
                Logger.Log("[Record player settings] " + GetExceptionMessage(ex), Color.Red);
            }
        }
        public static void MainServerHandler(Player.PlayerInfo player, byte[] buff)
        {
                List<Blaze.Packet> packets = Blaze.FetchAllBlazePackets(new MemoryStream(buff));
                foreach (Blaze.Packet p in packets)
                {
                    switch (p.Component)
                    {
                        case 0x1:
                            HandleComponent_1(player, p);
                            break;
                        case 0x4:
                            HandleComponent_4(player, p);
                            break;
                        case 0x7:
                            HandleComponent_7(player, p);
                            break;
                        case 0x9:
                            HandleComponent_9(player, p);
                            break;
                        case 0xF:
                            HandleComponent_F(player, p);
                            break;
                        case 0x19:
                            HandleComponent_19(player, p);
                            break;
                        case 0x1C:
                            HandleComponent_1C(player, p);
                            break;
                        case 0x7802:
                            HandleComponent_7802(player, p);
                            break;
                    }
                }
        }
        public static void HandleComponent_1(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0x1D: // listUserEntitlements2
                        List<Blaze.Tdf> cont = Blaze.ReadPacketContent(p);
                        if (cont.Count == 13)
                        {
                            Blaze.TdfString etag = (Blaze.TdfString)cont[3];
                            if (etag.Value == "")
                            {
                                MemoryStream m = new MemoryStream(ME3Server_WV.Properties.Resources._01_1D_res);
                                m.Seek(0, 0);
                                Blaze.Packet p2 = Blaze.ReadBlazePacket(m);
                                List<Blaze.Tdf> content = Blaze.ReadPacketContent(p2);
                                SendPacket(player, Blaze.CreatePacket(1, 0x1D, 0, 0x1000, p.ID, content));
                                if (!player.SendOffers)
                                {
                                    player.SendOffers = true;
                                    m = new MemoryStream(ME3Server_WV.Properties.Resources._7802_01_res);
                                    m.Seek(0, 0);
                                    p2 = Blaze.ReadBlazePacket(m);
                                    content = Blaze.ReadPacketContent(p2);
                                    Blaze.TdfStruct DATA = (Blaze.TdfStruct)content[0];
                                    DATA.Values[0] = GetTdfUnionIP(player, "ADDR");
                                    Blaze.TdfStruct QDAT = (Blaze.TdfStruct)DATA.Values[7];
                                    Blaze.TdfInteger NATT = (Blaze.TdfInteger)QDAT.Values[0];
                                    NATT.Value = NAT_Type;
                                    Blaze.TdfInteger USID = (Blaze.TdfInteger)content[1];
                                    USID.Value = player.UserID;
                                    SendPacket(player, Blaze.CreatePacket(0x7802, 0x1, 0, 0x2000, 0, content));
                                    SendPacket(player, Blaze.CreatePacket(0x7802, 0x1, 0, 0x2000, 0, content));
                                }
                            }
                            else
                                SendEmpty(player, p, 0x1000);
                        }
                        break;
                    case 0x24: // getAuthToken
                        List<Blaze.Tdf> res = new List<Blaze.Tdf>();
                        res.Add(Blaze.TdfString.Create("AUTH", player.PlayerID.ToString("X")));
                        SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, res));
                        break;
                    case 0x28: // login
                        HandleComponent_1_Command_28(player, p);
                        //SendLoginErrorPacket(player, p, TestErrorCode);
                        break;
                    case 0x32: // silentLogin
                        HandleComponent_1_Command_32(player, p);
                        break;
                    case 0x46: // logout
                        SendEmpty(player, p, 0x1000); // this is actually the proper response to client's logout
                        break;
                    case 0x6E: // loginPersona
                        HandleComponent_1_Command_6E(player, p);
                        break;
                    case 0x98: // originLogin
                        HandleComponent_1_Command_98(player, p);
                        break;
                    case 0x2F: // getPrivacyPolicyContent
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xF2: // getLegaldocsInfo
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xF6: // getTermsOfServiceContent
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xF1: // acceptLegalDocs
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x33: // checkAgeReq
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xA: // createAccount
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x50: // createPersona
                        SendEmpty(player, p, 0x1000);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }
        public static void HandleComponent_1_Command_32(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            if (content.Count != 3)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:32] Error: HandleComponent_1_Command_32: Count != 3 ", Color.Red);
                return;
            }
            else
            {
                Blaze.TdfString tdfs = (Blaze.TdfString)content[0];
                if (tdfs.Label != "AUTH")
                {
                    Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:32] Error: HandleComponent_1_Command_32: AUTH not found ", Color.Red);
                    return;
                }
                else
                {
                    player.AuthString = tdfs.Value;
                    player.Update = true;
                }
                Blaze.TdfInteger tdfi = (Blaze.TdfInteger)content[1];
                if (tdfi.Label != "PID ")
                {
                    Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:32] Error: HandleComponent_1_Command_32: PID not found ", Color.Red);
                    return;
                }
                else
                {
                    player.PlayerID = tdfi.Value;
                    string[] files = GetListOfPlayerFiles().ToArray();
                    bool found = false;
                    foreach (string file in files)
                    {
                        string[] lines = File.ReadAllLines(file);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] parts = lines[i].Split('=');
                            if (parts.Length == 2 && parts[0].Trim() == "PID")
                            {
                                long l = ConvertHex(parts[1]);
                                if (l == player.PlayerID)
                                    found = true;
                            }
                            if (parts.Length == 2 && parts[0].Trim() == "AUTH2" && found)
                                player.Auth2String = parts[1].Trim();
                            if (parts.Length == 2 && parts[0].Trim() == "UID" && found)
                                player.UserID = ConvertHex(parts[1]);
                            if (parts.Length == 2 && parts[0].Trim() == "DSNM" && found)
                            {
                                player.Name = parts[1].Trim();
                                player.pathtoprofile = file;
                                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:32] (silentLogin) Name=" + player.Name + ", PID=0x" + player.PlayerID.ToString("X"), Color.DarkOrange);
                                player.Settings = new List<Player.PlayerInfo.SettingEntry>();
                                for (int j = i + 1; j < lines.Length; j++)
                                {
                                    string[] set = lines[j].Split('=');
                                    if (set.Length == 2)
                                        player.UpdateSettings(set[0].Trim(), set[1].Trim());
                                }
                                CreateAuthPacket01(player, p);
                                CreateAuthPacket02(player, p);
                                break;
                            }
                        }
                        if (found)
                            break;
                    }
                    player.Update = true;
                }
            }
        }
        public static void HandleComponent_1_Command_98(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            if (content.Count != 2)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:98] Error: HandleComponent_1_Command_98: Count != 2 ", Color.Red);
                return;
            }
            else
            {
                Blaze.TdfString tdfs = (Blaze.TdfString)content[0];
                if (tdfs.Label != "AUTH")
                {
                    Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:98] Error: HandleComponent_1_Command_98: AUTH not found ", Color.Red);
                    return;
                }
                else
                {
                    player.AuthString = tdfs.Value;
                    player.Auth2String = "Ciyvab0tregdVsBtboIpeChe4G6uzC1v5_-SIxmvSLKgZgp-f4WWjnLCUtT4rmTwWsr12wYQYPnpiBW8XHX24beeTARMTteIPnx7TkKeF5HUTTTWqz-2HZfAJw4xecQArZjwI3t0GEzOL_kXDCqWMg";
                    player.Name = Config.FindEntry("OriginName") + "_" + player.ID;
                    player.PlayerID = ConvertHex(Config.FindEntry("OriginPID")) + player.ID;
                    player.UserID = ConvertHex(Config.FindEntry("OriginUID")) + player.ID;
                    player.Update = true;
                    Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:98] (originLogin) Name=" + player.Name + ", PID=0x" + player.PlayerID.ToString("X"), Color.DarkOrange);
                    CreateAuthPacket01(player, p);
                    CreateAuthPacket02(player, p);
                }
            }
        }
        public static void HandleComponent_1_Command_28(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            if (content.Count != 5)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:28] Error: HandleComponent_1_Command_28: Count != 5 ", Color.Red);
                return;
            }
            string playername = ((Blaze.TdfString)content[1]).Value.Trim();
            string password = ((Blaze.TdfString)content[2]).Value.Trim();
            // playername and password cannot be blank
            if (playername == "" || password == "")
            {
                SendLoginErrorPacket(player, p, LoginErrorCode.INVALIDINFORMATION);
                return;
            }
            // check playername and password against existing player files
            string file; // player profile text file
            bool valid; // name/password flag
            ME3PlayerHeader header;
            CheckNamePassword(playername, password, out file, out valid, out header);
            // if file is null or empty, no player files have 'DSNM=playername'
            if (String.IsNullOrEmpty(file))
            {
                SendLoginErrorPacket(player, p, LoginErrorCode.INVALIDEMAIL);
                return;
            }
            // file has been found; if valid is false, either password doesn't match AUTH2, or AUTH2 itself is in an invalid format for passwords
            // the latter means old profiles can only be used through the old way (local_profile.sav)
            if (!valid)
            {
                SendLoginErrorPacket(player, p, LoginErrorCode.WRONGPASSWORD);
                return;
            }
            // if 'file' is not null or empty, and 'valid' is true, then header has valid data
            // To define a player: name, playerid, authstring, auth2string, userid, pathtoprofile, settings, update->true
            player.Name = header.GetDisplayName();
            player.PlayerID = header.GetPID();
            player.AuthString = header.GetAuth();
            player.Auth2String = header.GetAuth2();
            player.UserID = header.GetUID();
            player.pathtoprofile = file;
            player.Settings = new List<Player.PlayerInfo.SettingEntry>();
            ME3MP_Profile profile = ME3MP_Profile.InitializeFromFile(file);
             /* One 'Base' line, six 'class' lines and one or more 'char' lines must be present in the file,
              * or else InitializeFromFile will return null */
            if (profile != null)
            {
                string[] settings = profile.ToLines(false).ToArray();
                foreach (string line in settings)
                {
                    string[] set = line.Split('=');
                    if (set.Length == 2)
                        player.UpdateSettings(set[0].Trim(), set[1].Trim());
                }
            }
            player.Update = true;
            Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:28] (login) Name=" + player.Name + ", PID=0x" + player.PlayerID.ToString("X"), Color.DarkOrange);
            CreateAuthPacket01_28(player, p); // Direct response
        }
        public static void CheckNamePassword(string PlayerName, string Password, out string PlayerFile, out bool ValidLogin, out ME3PlayerHeader Header)
        {
            PlayerFile = "";
            ValidLogin = false;
            Header = null;
            string[] playerfiles = GetListOfPlayerFiles().ToArray();
            foreach (string pf in playerfiles)
            {
                Header = new ME3PlayerHeader(File.ReadAllLines(pf));
                if (Header.GetDisplayName().ToLower() == PlayerName.ToLower())
                {
                    PlayerFile = pf;
                    break;
                }
            }
            if (PlayerFile == "")
                return;
            if (!GUI_ProfileCreator.IsValidPassword(Header.GetAuth2()))
                return;
            if (Header.GetAuth2() == Password)
                ValidLogin = true;
        }
        public static void HandleComponent_1_Command_6E(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            if (content.Count != 1)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:6E] Error: HandleComponent_1_Command_6E: Count != 1 ", Color.Red);
                return;
            }
            Blaze.TdfString pnam = (Blaze.TdfString)content[0];
            if (pnam.Label != "PNAM")
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:6E] Error: HandleComponent_1_Command_6E: PNAM not found ", Color.Red);
                return;
            }
            if (pnam.Value != player.Name)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1:6E] Error: HandleComponent_1_Command_6E: PNAM is invalid ", Color.Red);
                return;
            }
            uint t = Blaze.GetUnixTimeStamp();
            List<Blaze.Tdf> SESS = new List<Blaze.Tdf>();
            SESS.Add(Blaze.TdfInteger.Create("BUID", player.PlayerID));
            SESS.Add(Blaze.TdfInteger.Create("FRST", 0));
            SESS.Add(Blaze.TdfString.Create("KEY\0", "11229301_9b171d92cc562b293e602ee8325612e7"));
            SESS.Add(Blaze.TdfInteger.Create("LLOG", t));
            SESS.Add(Blaze.TdfString.Create("MAIL", ""));
            List<Blaze.Tdf> PDTL = new List<Blaze.Tdf>();
            PDTL.Add(Blaze.TdfString.Create("DSNM", player.Name));
            PDTL.Add(Blaze.TdfInteger.Create("LAST", t));
            PDTL.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
            PDTL.Add(Blaze.TdfInteger.Create("STAS", 0));
            PDTL.Add(Blaze.TdfInteger.Create("XREF", 0));
            PDTL.Add(Blaze.TdfInteger.Create("XTYP", 0));
            SESS.Add(Blaze.TdfStruct.Create("PDTL", PDTL));
            SESS.Add(Blaze.TdfInteger.Create("UID\0", player.UserID));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, SESS));
            CreateAuthPacket02(player, p); // Async
        }
        public static void HandleComponent_4(Player.PlayerInfo player, Blaze.Packet p)
        {
                List<Blaze.Tdf> res;
                switch (p.Command)
                {
                    case 0x1:
                        CreateGameStartPacket(player, p);
                        break;
                    case 0x3:
                        res = Blaze.ReadPacketContent(p);
                        if (res.Count == 2)
                        {
                            Blaze.TdfInteger GID = (Blaze.TdfInteger)res[0];
                            Blaze.TdfInteger GSTA = (Blaze.TdfInteger)res[1];
                            GameManager.GameInfo game = GameManager.FindByGID(GID.Value);
                            if (game != null)
                                game.UpdateGameState((int)GSTA.Value);
                        }
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x4:
                        HandleComponent_4_Command_4(player, p);
                        break;
                    case 0x7:
                        HandleComponent_4_Command_7(player, p);                        
                        break;
                    case 0xB:
                        res = Blaze.ReadPacketContent(p);
                        if (res.Count == 5)
                        {
                            Blaze.TdfInteger PID = (Blaze.TdfInteger)res[3];
                            Blaze.TdfInteger GID = (Blaze.TdfInteger)res[2];
                            List<Blaze.Tdf> ejectRes = new List<Blaze.Tdf>();
                            for (int i = 0; i < 4; i++)
                                ejectRes.Add(res[i + 1]);
                            GameManager.GameInfo game = GameManager.FindByGID(GID.Value);
                            if (game != null)
                            {
                                foreach (Player.PlayerInfo pl in game.AllPlayers)
                                {
                                    SendPacket(pl, Blaze.CreatePacket(0x4, 0x28, 0, 0x2000, 0, ejectRes));
                                }
                                for(int i=0;i<game.OtherPlayers.Count;i++)
                                    if (game.OtherPlayers[i].PlayerID == PID.Value)
                                    {
                                        game.OtherPlayers.RemoveAt(i);
                                        game.Update = true;
                                    }
                                if (game.Creator.PlayerID == PID.Value)
                                {
                                    game.isActive = false;
                                    game.Update = true;
                                }
                                foreach (Player.PlayerInfo pl in game.AllPlayers)
                                {
                                    SendPacket(pl, Create7802_03_packet(player));
                                    SendPacket(player, Create7802_03_packet(pl));
                                }
                            }
                            //SendPacket(player, Blaze.CreatePacket(0x4, 0x28, 0, 0x2000, 0, ejectRes));
                        }
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xD:
                        CreateJoinGamePacket(player, p);
                        break;
                    case 0xE:
                        player.SetJoinWaitState(false);
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x1D:
                        HandleComponent_4_Command_1D(player, p);                    
                        break;
                    default:
                        SendEmpty(player, p, 0x1000);
                        break;
                }

        }
        public static byte[] Create7802_03_packet(Player.PlayerInfo player)
        {
            List<Blaze.Tdf> listTDF = new List<Blaze.Tdf>();
            listTDF.Add(Blaze.TdfInteger.Create("BUID", player.PlayerID));
            return Blaze.CreatePacket(0x7802, 0x3, 0, 0x2000, 0, listTDF);
        }
        public static void HandleComponent_4_Command_1D(Player.PlayerInfo player, Blaze.Packet p)
        {
            byte[] buff;
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            Blaze.TdfInteger GID = (Blaze.TdfInteger)input[0];
            GameManager.GameInfo game = GameManager.FindByGID((uint)GID.Value);
            if (game != null && player.WaitsForJoining)
            {
                MemoryStream res = new MemoryStream();            
                List<Blaze.Tdf> form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
                form.Add(Blaze.TdfInteger.Create("STAT", 4));
                buff = Blaze.CreatePacket(0x4, 0x74, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
                buff = Blaze.CreatePacket(0x4, 0x1E, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("ALST", player.PlayerID));
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("OPER", 0));
                form.Add(Blaze.TdfInteger.Create("UID\0", game.Creator.PlayerID));
                buff = Blaze.CreatePacket(0x4, 0xCA, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                player.SetJoinWaitState(false);
                MemoryStream host = new MemoryStream();
                form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
                form.Add(Blaze.TdfInteger.Create("STAT", 4));
                buff = Blaze.CreatePacket(0x4, 0x74, 0, 0x2000, 0, form);
                host.Write(buff, 0, buff.Length);
                form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
                buff = Blaze.CreatePacket(0x4, 0x1E, 0, 0x2000, 0, form);
                host.Write(buff, 0, buff.Length);
                form = new List<Blaze.Tdf>();
                form.Add(Blaze.TdfInteger.Create("ALST", player.PlayerID));
                form.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
                form.Add(Blaze.TdfInteger.Create("OPER", 0));
                form.Add(Blaze.TdfInteger.Create("UID\0", game.Creator.PlayerID));
                buff = Blaze.CreatePacket(0x4, 0xCA, 0, 0x2000, 0, form);
                host.Write(buff, 0, buff.Length);
                SendPacket(player, res.ToArray());
                SendPacket(game.Creator, host.ToArray());
                return;
            }
            SendEmpty(player, p, 0x1000);
        }
        public static void HandleComponent_4_Command_7(Player.PlayerInfo player, Blaze.Packet p)
        {
            byte[] buff = File.ReadAllBytes(loc + "replay\\04_50_res.bin");
            Blaze.Packet pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
            List<Blaze.Tdf> form = Blaze.ReadPacketContent(pform);
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            Blaze.TdfDoubleList ATTR = (Blaze.TdfDoubleList)input[0];
            Blaze.TdfInteger GID = (Blaze.TdfInteger)input[1];
            Blaze.TdfDoubleList ATTR2 = (Blaze.TdfDoubleList)form[0];
            Blaze.TdfInteger GID2 = (Blaze.TdfInteger)form[1];
            ATTR2.List1 = ATTR.List1;
            ATTR2.List2 = ATTR.List2;
            ATTR2.Count = ATTR.Count;
            GID2.Value = GID.Value;
            GameManager.GameInfo game = GameManager.FindByGID((uint)GID.Value);
            if (game != null)
            {
                game.UpdateAttributes(((List<string>)(ATTR.List1)).ToArray(), ((List<string>)(ATTR.List2)).ToArray());
                foreach (Player.PlayerInfo pl in game.AllPlayers)
                    SendPacket(pl, Blaze.CreatePacket(0x4, 0x50, 0, 0x2000, 0, input));
            }
            SendEmpty(player, p, 0x1000);
        }
        public static void HandleComponent_4_Command_4(Player.PlayerInfo player, Blaze.Packet p)
        {
            byte[] buff = File.ReadAllBytes(loc + "replay\\04_6E_res.bin");
            Blaze.Packet pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
            List<Blaze.Tdf> form = Blaze.ReadPacketContent(pform);
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            Blaze.TdfInteger GID = (Blaze.TdfInteger)input[0];
            Blaze.TdfInteger GSET = (Blaze.TdfInteger)input[1];
            Blaze.TdfInteger ATTR = (Blaze.TdfInteger)form[0];
            Blaze.TdfInteger GID2 = (Blaze.TdfInteger)form[1];
            GID2.Value = GID.Value;
            ATTR.Value = GSET.Value;
            GameManager.GameInfo game = GameManager.FindByGID((uint)GID.Value);
            if (game != null)
                game.UpdateGameSetting((int)GSET.Value);
            MemoryStream res = new MemoryStream();
            List<Blaze.Tdf> empty = new List<Blaze.Tdf>();
            buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, empty);
            res.Write(buff, 0, buff.Length);
            buff = Blaze.CreatePacket(0x4, 0x6e, 0, 0x2000, 0, form);
            res.Write(buff, 0, buff.Length);
            SendPacket(player, res.ToArray());
        }
        public static void HandleComponent_7(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0xA: // getLeaderboardGroup
                        HandleComponent_7_Command_A(player, p);
                        break;
                    case 0xE: // getFilteredLeaderboard
                        HandleComponent_7_Command_E(player, p);
                        break;
                    case 0x12: // getLeaderboardEntityCount
                        HandleComponent_7_Command_12(player, p);
                        break;
                    case 0xD: // getCenteredLeaderboard
                        HandleComponent_7_Command_D(player, p);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void HandleComponent_7_Command_A(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {

                List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                if (content.Count == 2)
                {
                    Blaze.TdfString name = (Blaze.TdfString)content[1];
                    List<Blaze.Tdf> res = new List<Blaze.Tdf>();
                    switch (name.Value)
                    {
                        case "N7RatingGlobal":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0A_01_res)));
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xA, 0x0, 0x1000, p.ID, res));
                            break;
                        case "N7RatingDE":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0A_02_res)));
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xA, 0x0, 0x1000, p.ID, res));
                            break;
                        case "ChallengePointsGlobal":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0A_03_res)));
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xA, 0x0, 0x1000, p.ID, res));
                            break;
                        case "ChallengePointsDE":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0A_04_res)));
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xA, 0x0, 0x1000, p.ID, res));
                            break;
                        default:
                            SendEmpty(player, p, 0x1000);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7:A] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void HandleComponent_7_Command_E(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                if (content.Count == 8)
                {
                    Blaze.TdfString name = (Blaze.TdfString)content[4];
                    List<Blaze.Tdf> res = new List<Blaze.Tdf>();
                    Blaze.TdfList LDLS;
                    Blaze.TdfStruct entry;
                    Blaze.TdfString ENAM;
                    Blaze.TdfInteger ENID;
                    Blaze.TdfUnion RWST;
                    ME3MP_Profile profile;
                    Blaze.TdfString RSTA;
                    switch (name.Value)
                    {
                        case "N7RatingGlobal":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0E_01_res)));
                            LDLS = (Blaze.TdfList)res[0];
                            entry = ((List<Blaze.TdfStruct>)LDLS.List)[0];
                            ENAM = (Blaze.TdfString)entry.Values[0];
                            ENAM.Value = player.Name;
                            ENID = (Blaze.TdfInteger)entry.Values[1];
                            ENID.Value = player.PlayerID;
                            RWST = (Blaze.TdfUnion)entry.Values[5];
                            RWST.UnionType = 0x7F;
                            profile = ME3MP_Profile.InitializeFromFile(player.pathtoprofile);
                            if (profile != null)
                            {
                                RSTA = (Blaze.TdfString)entry.Values[3];
                                RSTA.Value = profile.GetN7Rating().ToString();
                                List<string> statlist = new List<string>(new string[]{RSTA.Value});
                                entry.Values[6] = Blaze.TdfList.Create("STAT", 1, 1, statlist);
                            }
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xE, 0x0, 0x1000, p.ID, res));
                            break;
                        case "ChallengePointsGlobal":
                            res = Blaze.ReadPacketContent(Blaze.ReadBlazePacket(new MemoryStream(ME3Server_WV.Properties.Resources._07_0E_02_res)));
                            LDLS = (Blaze.TdfList)res[0];
                            entry = ((List<Blaze.TdfStruct>)LDLS.List)[0];
                            ENAM = (Blaze.TdfString)entry.Values[0];
                            ENAM.Value = player.Name;
                            ENID = (Blaze.TdfInteger)entry.Values[1];
                            ENID.Value = player.PlayerID;
                            RWST = (Blaze.TdfUnion)entry.Values[5];
                            RWST.UnionType = 0x7F;
                            profile = ME3MP_Profile.InitializeFromFile(player.pathtoprofile);
                            if (profile != null)
                            {
                                RSTA = (Blaze.TdfString)entry.Values[3];
                                RSTA.Value = profile.GetChallengePoints().ToString();
                                List<string> statlist = new List<string>(new string[]{RSTA.Value});
                                entry.Values[6] = Blaze.TdfList.Create("STAT", 1, 1, statlist);
                            }
                            SendPacket(player, Blaze.CreatePacket(0x7, 0xE, 0x0, 0x1000, p.ID, res));
                            break;
                        default:
                            SendEmpty(player, p, 0x1000);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7:E] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void HandleComponent_7_Command_12(Player.PlayerInfo player, Blaze.Packet p)
        {
            // getLeaderboardEntityCount - game wants to know how many entries are in a certain list.
            try
            {
                //List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                //Blaze.TdfString NAME = (Blaze.TdfString)content[2];
                // NAME -> N7RatingGlobal, N7RatingXX, ChallengePointsGlobal, ChallengePointsXX (where XX is the country code)
                // currently, no special rules are checked. All players are in the same pool.
                List<Blaze.Tdf> response = new List<Blaze.Tdf>();
                Blaze.TdfInteger CNT = Blaze.TdfInteger.Create("CNT\0", GetListOfActiveProfiles().Count);
                response.Add(CNT);
                SendPacket(player, Blaze.CreatePacket(0x7, 0x12, 0x0, 0x1000, p.ID, response));
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7:12] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static void HandleComponent_7_Command_D(Player.PlayerInfo player, Blaze.Packet p)
        {
            // getCenteredLeaderboard - game wants a list of n entries, where a specific ID is the center.
            // Example: 60 entries, center ID is 12345. Player 12345 will be #31 (index 30).
            // even number -> x = n / 2; -> get x entries before player, get player, get (x - 1) entries after player
            // odd number -> x = n / 2; -> get x entries before player, get player, get x entries after player
            try
            {
                List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
                Blaze.TdfInteger CENT = (Blaze.TdfInteger)content[1]; // central element - player ID
                Blaze.TdfInteger COUN = (Blaze.TdfInteger)content[2]; // number of elements to return
                Blaze.TdfString NAME = (Blaze.TdfString)content[5]; // leaderboard
                List<Tuple<string, long, int, int>> leaderboard = null;
                int type = 0;
                if (NAME.Value.Contains("N7Rating"))
                {
                    leaderboard = GetLeaderboard(0);
                }
                else if (NAME.Value.Contains("ChallengePoints"))
                {
                    leaderboard = GetLeaderboard(1);
                    type = 1;
                }
                if (leaderboard == null || leaderboard.Count == 0)
                    return;
                // now the real fun starts... 
                // 1- COUN.Value cannot be higher than leaderboard.Count!
                if (COUN.Value > leaderboard.Count)
                    COUN.Value = leaderboard.Count;
                // 2- What the hell is center player's position?
                int pos = 0; // default value - if player is not in the leaderboard, return the top scores
                for (int i = 0; i < leaderboard.Count; i++)
                {
                    if (leaderboard[i].Item2 == CENT.Value)
                    {
                        pos = i;
                        break;
                    }
                }
                // 3- Determine where reading will start
                int half = (int)(COUN.Value / 2);
                int start = pos - half;
                // 3a- If player is close to the top, 'start' may be a negative number
                if (start < 0)
                    start = 0;
                // 3b- If player is close to the bottom, (start + COUN.Value) may be an index equal or higher than leaderboard.Count
                // (in other words, when number of entries from pos to last index is lower than 'half')
                if ((start + COUN.Value) >= leaderboard.Count)
                    start = (int)(leaderboard.Count - COUN.Value);
                // 4- Read starting at 'start', until n entries have been processed (where 'n' is COUN.Value)

                List<Blaze.TdfStruct> playerentries = new List<Blaze.TdfStruct>();

                for (int i = 0; i < COUN.Value; i++)
                {

                    List<Blaze.Tdf> playerentry = new List<Blaze.Tdf>();

                    Blaze.TdfString ENAM = Blaze.TdfString.Create("ENAM", leaderboard[start + i].Item1); // player name
                    Blaze.TdfInteger ENID = Blaze.TdfInteger.Create("ENID", leaderboard[start + i].Item2); // player ID
                    Blaze.TdfInteger RANK = Blaze.TdfInteger.Create("RANK", start + i + 1); // position in leaderboard (starts at 1)
                    Blaze.TdfString RSTA; // score as string
                    if (type == 0)
                        RSTA = Blaze.TdfString.Create("RSTA", leaderboard[start + i].Item3.ToString()); // N7 Rating
                    else
                        RSTA = Blaze.TdfString.Create("RSTA", leaderboard[start + i].Item4.ToString()); // Challenge Points
                    Blaze.TdfInteger RWFG = Blaze.TdfInteger.Create("RWFG", 0); // unknown
                    Blaze.TdfUnion RWST = Blaze.TdfUnion.Create("RWST"); // unknown
                    List<string> statlist = new List<string>();
                    statlist.Add(RSTA.Value);
                    Blaze.TdfList STAT = Blaze.TdfList.Create("STAT", 1, 1, statlist); // list of single item - score as string - dunno why
                    Blaze.TdfInteger UATT = Blaze.TdfInteger.Create("UATT", 0); // unknown
                    playerentry.Add(ENAM);
                    playerentry.Add(ENID);
                    playerentry.Add(RANK);
                    playerentry.Add(RSTA);
                    playerentry.Add(RWFG);
                    playerentry.Add(RWST);
                    playerentry.Add(STAT);
                    playerentry.Add(UATT);
                    playerentries.Add(Blaze.TdfStruct.Create(i.ToString(), playerentry));
                }


                Blaze.TdfList LDLS = Blaze.TdfList.Create("LDLS", 3, playerentries.Count(), playerentries);
                List<Blaze.Tdf> response = new List<Blaze.Tdf>();
                response.Add(LDLS);

                //byte[] buff = File.ReadAllBytes(loc + "replay\\pl.bin");
                //Blaze.Packet pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
                //List<Blaze.Tdf> response = Blaze.ReadPacketContent(pform);

                SendPacket(player, Blaze.CreatePacket(0x7, 0xD, 0x0, 0x1000, p.ID, response));
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7:D] Error:\n" + GetExceptionMessage(e), Color.Red);
            }
        }
        public static List<Tuple<string, long, int, int>> GetLeaderboard(int type)
        {
            // name, id, n7, challenge
            List<Tuple<string, long, int, int>> leaderboard = new List<Tuple<string, long, int, int>>();
            List<ME3MP_Profile> profiles = GetListOfActiveProfiles();
            foreach (ME3MP_Profile profile in profiles)
            {
                leaderboard.Add(new Tuple<string, long, int, int>(profile.GetPlayerName(), profile.GetPlayerID(), profile.GetN7Rating(), profile.GetChallengePoints()));
            }
            if (type == 0) // N7 Rating
            {
                leaderboard.Sort((a, b) => b.Item3.CompareTo(a.Item3));
            }
            else if (type == 1) // Challenge Points
            {
                leaderboard.Sort((a, b) => b.Item4.CompareTo(a.Item4));
            }
            return leaderboard;
        }
        public static void HandleComponent_9(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                List<Blaze.Tdf> content;
                switch (p.Command)
                {
                    case 0x1:
                        HandleComponent_9_Command_1(player, p);
                        break;
                    case 0x2:
                        long ms = player.PingTimer.ElapsedMilliseconds;
                        Logger.Log("[Main Server Handler " + player.ID + "][Handler_9:*] Last Ping Time was : " + (ms / 1000f).ToString() + " seconds", Color.Orange, 3);
                        player.PingTimer.Restart();
                        CreateServerTimePacket(player, p);
                        break;
                    case 0x7:
                        CreateBootPacket01(player, p);
                        break;
                    case 0x8:
                        CreateBootPacket02(player, p);
                        break;
                    case 0xB:
                        content = Blaze.ReadPacketContent(p);
                        if (content.Count == 3)
                        {
                            Blaze.TdfString DATA = (Blaze.TdfString)content[0];
                            Blaze.TdfString KEY = (Blaze.TdfString)content[1];
                            player.UpdateSettings(KEY.Value, DATA.Value);
                        }
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0xC:
                        HandleComponent_9_Command_C(player, p);
                        break;
                    case 0x16:
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x1B:
                        content = Blaze.ReadPacketContent(p);
                        if (content.Count == 1)
                        {
                            Blaze.TdfInteger tval = (Blaze.TdfInteger)content[0];
                            switch (tval.Value)
                            {
                                case 0x1312D00:
                                    SendPacket(player, Blaze.CreatePacket(0x9, 0x1B, 0x12D, 0x3000, p.ID, new List<Blaze.Tdf>()));
                                    break;
                                case 0x55D4A80:
                                    SendPacket(player, Blaze.CreatePacket(0x9, 0x1B, 0x12E, 0x3000, p.ID, new List<Blaze.Tdf>()));
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_9:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }        
        }
        public static void HandleComponent_9_Command_1(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            if (content.Count != 1)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_9:1] Error: HandleComponent_9_Command_1: Count != 1 ", Color.Red);
                return;
            }
            else
            {
                Blaze.TdfString tdfs = (Blaze.TdfString)content[0];
                if (tdfs.Label != "CFID")
                {
                    Logger.Log("[Main Server Handler " + player.ID + "][Handler_9:1] Error: HandleComponent_9_Command_1: CFID not found ", Color.Red);
                    return;
                }
                else
                {
                    string command = tdfs.Value;
                    string[] lines;
                    List<string> List1, List2;
                    List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
                    if (command.StartsWith("ME3_LIVE_TLK_PC_"))
                    {
                        HandleLiveTLK(player, p, command.Substring(16));
                        return;
                    }
                    switch (command)
                    {
                        case "ME3_DATA":
                            lines = File.ReadAllLines(loc + "conf\\ME3DATA.txt");
                            List1 = new List<string>();
                            List2 = new List<string>();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(';');
                                List1.Add(parts[0].Trim());
                                List2.Add(parts[1].Trim());
                            }
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, lines.Length));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        case "ME3_MSG":
                            lines = File.ReadAllLines(loc + "conf\\ME3MSG.txt");
                            List1 = new List<string>();
                            List2 = new List<string>();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(';');
                                List1.Add(parts[0].Trim());
                                List2.Add(parts[1].Trim());
                            }
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, lines.Length));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        case "ME3_ENT":
                            lines = File.ReadAllLines(loc + "conf\\ME3ENT.txt");
                            List1 = new List<string>();
                            List2 = new List<string>();
                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts;
                                if (!lines[i].Trim().StartsWith("ENT_ENC"))
                                    parts = lines[i].Split(';');
                                else
                                    parts = lines[i].Split(':');
                                List1.Add(parts[0].Trim());
                                List2.Add(parts[1].Trim());
                            }
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, lines.Length));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        case "ME3_DIME":
                            List1 = new List<string>();
                            List2 = new List<string>();
                            List1.Add("Config");
                            List2.Add(File.ReadAllText(loc + "conf\\ME3DIME.txt"));
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, 1));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        case "ME3_BINI_VERSION":
                            List1 = new List<string>();
                            List2 = new List<string>();
                            List1.Add("SECTION");
                            List2.Add("BINI_PC_COMPRESSED");
                            List1.Add("VERSION");
                            List2.Add("40128");
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, 2));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        case "ME3_BINI_PC_COMPRESSED":
                            List1 = new List<string>();
                            List2 = new List<string>();
                            CreateBase64StringsFromCompressedCoalesced(GetLiveBINI(), out List1, out List2);
                            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, List1.Count));
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                        //case "ME3_LIVE_TLK_PC_en":
                        //    List1 = new List<string>();
                        //    List2 = new List<string>();
                        //    CreateBase64StringsFromTLK(GetLiveTLK(), out List1, out List2);
                        //    Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, List1.Count));
                        //    SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                        //    break;
                        default:
                            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
                            break;
                    }
                }
            }
        }
        public static void HandleLiveTLK(Player.PlayerInfo player, Blaze.Packet p, string lang)
        {
            //Logger.Log("TLK file requested | language: " + lang, Color.White);
            List<string> List1 = new List<string>();
            List<string> List2 = new List<string>();
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            string tlkfile = ignoreTLKLangCode ? GetLiveTLK() : GetLiveTLK(lang);
            CreateBase64StringsFromTLK(tlkfile, out List1, out List2);
            Result.Add(Blaze.TdfDoubleList.Create("CONF", 1, 1, List1, List2, List1.Count));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void HandleComponent_9_Command_C(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            List<string> Keys = new List<string>();
            List<string> Data = new List<string>();
            foreach (Player.PlayerInfo.SettingEntry set in player.Settings)
            {
                Keys.Add(set.Key);
                Data.Add(set.Data);
            }
            result.Add(Blaze.TdfDoubleList.Create("SMAP", 1, 1, Keys, Data, Keys.Count));
            SendPacket(player, Blaze.CreatePacket(0x9, 0xC, 0, 0x1000, p.ID, result));
        }
        public static void HandleComponent_F(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0x2:
                        MemoryStream m = new MemoryStream();
                        List<Blaze.Tdf> content = new List<Blaze.Tdf>();
                        content.Add(Blaze.TdfInteger.Create("MCNT", 0x1));
                        byte[] buff = Blaze.CreatePacket(0xF, 0x02, 0, 0x1000, p.ID, content);
                        m.Write(buff, 0, buff.Length);
                        buff = MainMenuMessagePacket(player);
                        m.Write(buff, 0, buff.Length);
                        SendPacket(player, m.ToArray());
                        player.GameState = "mainmenu";
                        player.Update = true;
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_F:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }
        private static byte[] MainMenuMessagePacket(Player.PlayerInfo player)
        {
            string v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int c = 0;
            foreach (Player.PlayerInfo pl in Player.AllPlayers)
                if (pl.isActive)
                    c++;
            string d = DateTime.Now.ToString("yyyy.MM.dd");
            string t = DateTime.Now.ToString("HH:mm:ss");
            List<Blaze.Tdf> message = new List<Blaze.Tdf>();
            message.Add(Blaze.TdfInteger.Create("FLAG", 0x01));
            message.Add(Blaze.TdfInteger.Create("MGID", 0x01));
            string strMsg = Config.MainMenuMessage();
            strMsg = strMsg.Replace("{v}", v);
            strMsg = strMsg.Replace("{n}", player.Name);
            strMsg = strMsg.Replace("{ip}", player.IP);
            strMsg = strMsg.Replace("{c}", c.ToString());
            strMsg = strMsg.Replace("{d}", d);
            strMsg = strMsg.Replace("{t}", t);
            strMsg += (char)0xA;
            message.Add(Blaze.TdfString.Create("NAME", strMsg));
            List<Blaze.Tdf> listPYLD = new List<Blaze.Tdf>();
            listPYLD.Add(Blaze.TdfDoubleList.Create("ATTR", 1, 1, new List<string> { "B0000" }, new List<string> { "160" }, 1));
            listPYLD.Add(Blaze.TdfInteger.Create("FLAG", 0x01));
            listPYLD.Add(Blaze.TdfInteger.Create("STAT", 0x00));
            listPYLD.Add(Blaze.TdfInteger.Create("TAG\0", 0x00));
            listPYLD.Add(Blaze.TdfTrippleVal.Create("TARG", new Blaze.TrippleVal(0x7802, 0x01, player.PlayerID)));
            listPYLD.Add(Blaze.TdfInteger.Create("TYPE", 0x00));
            message.Add(Blaze.TdfStruct.Create("PYLD", listPYLD));
            message.Add(Blaze.TdfTrippleVal.Create("SRCE", new Blaze.TrippleVal(0x7802, 0x01, player.PlayerID)));
            message.Add(Blaze.TdfInteger.Create("TIME", Blaze.GetUnixTimeStamp()));
            return Blaze.CreatePacket(0xF, 0x01, 0, 0x2000, 0, message);
        }
        public static void HandleComponent_19(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0x4:
                        SendEmpty(player, p, 0x1000);
                        break;
                    case 0x6:
                        CreateBootPacket03(player, p);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_19:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }
        public static void HandleComponent_1C(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0x2:
                        MemoryStream res = new MemoryStream();
                        List<Blaze.Tdf> result = new List<Blaze.Tdf>();
                        byte[] buff = Blaze.CreatePacket(0x1C, 0x2, 0, 0x1000, p.ID, result);
                        res.Write(buff, 0, buff.Length);
                        result.Add(Blaze.TdfIntegerList.Create("DATA", 0, new List<long>()));
                        result.Add(Blaze.TdfInteger.Create("EROR", 0));
                        result.Add(Blaze.TdfInteger.Create("FNL\0", 0));
                        result.Add(Blaze.TdfInteger.Create("GHID", 0));
                        result.Add(Blaze.TdfInteger.Create("GRID", 0));
                        buff = Blaze.CreatePacket(0x1C, 0x72, 0, 0x2000, 0, result);
                        res.Write(buff, 0, buff.Length);
                        SendPacket(player, res.ToArray());
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_1C:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }
        public static void HandleComponent_7802(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                switch (p.Command)
                {
                    case 0x8:
                    case 0x14:
                        HandleComponent_7802_14(player, p);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7802:*] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }        
        public static void HandleComponent_7802_14(Player.PlayerInfo player, Blaze.Packet p)
        {
            try
            {
                List<Blaze.Tdf> req = Blaze.ReadPacketContent(p);
                for (int i = 0; i < req.Count; i++)
                {
                    Blaze.Tdf tdf = req[i];
                    switch (tdf.Label)
                    {
                        case "ADDR":
                            Blaze.TdfUnion ADDR = (Blaze.TdfUnion)tdf;
                            Blaze.TdfStruct VALU = (Blaze.TdfStruct)ADDR.UnionContent;
                            Blaze.TdfStruct INIP = (Blaze.TdfStruct)VALU.Values[1];
                            //Blaze.TdfInteger IP = (Blaze.TdfInteger)INIP.Values[0];
                            Blaze.TdfInteger PORT = (Blaze.TdfInteger)INIP.Values[1];
                            player.INIP = new Player.NETDATA();
                            player.INIP.IP = player.GetIPvalue();
                            player.INIP.PORT = (uint)PORT.Value;
                            player.EXIP = new Player.NETDATA();
                            player.EXIP.IP = player.GetIPvalue();
                            player.EXIP.PORT = player.INIP.PORT;
                            break;

                    }
                }
                SendEmpty(player, p, 0x1000);
            }
            catch (Exception e)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][Handler_7802:14] Error:\n" + GetExceptionMessage(e), Color.Red);
            }

        }      
        public static void CreateBootPacket01(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            Result.Add(Blaze.TdfInteger.Create("ANON", ConvertHex(Config.FindEntry("ANON"))));
            Result.Add(Blaze.TdfString.Create("ASRC", Config.FindEntry("ASRC")));
            List<string> t = ConvertStringList(Config.FindEntry("CIDS"));
            List<long> t2 = new List<long>();
            foreach (string v in t)
                t2.Add(Convert.ToInt64(v));
            Result.Add(Blaze.TdfList.Create("CIDS", 0, t2.Count, t2));
            Result.Add(Blaze.TdfString.Create("CNGN", ""));
            t = new List<string>();
            List<string> t3 = new List<string>();
            ConvertDoubleStringList(Config.FindEntry("BOOTCONF"),out t,out t3);
            Blaze.TdfDoubleList conf2 = Blaze.TdfDoubleList.Create("CONF", 1, 1, t, t3, t.Count);
            List<Blaze.Tdf> t4 = new List<Blaze.Tdf>();
            t4.Add(conf2);
            Result.Add(Blaze.TdfStruct.Create("CONF", t4));
            Result.Add(Blaze.TdfString.Create("INST", Config.FindEntry("INST")));
            Result.Add(Blaze.TdfInteger.Create("MINR", ConvertHex(Config.FindEntry("MINR"))));
            Result.Add(Blaze.TdfString.Create("NASP", Config.FindEntry("NASP")));
            Result.Add(Blaze.TdfString.Create("PILD", ""));
            Result.Add(Blaze.TdfString.Create("PLAT", Config.FindEntry("PLAT")));
            Result.Add(Blaze.TdfString.Create("PTAG", ""));
            List<Blaze.Tdf> QOSS = new List<Blaze.Tdf>();
            List<Blaze.Tdf> BWPS = new List<Blaze.Tdf>();
            BWPS.Add(Blaze.TdfString.Create("PSA\0", Config.FindEntry("BOOTPSA0")));
            BWPS.Add(Blaze.TdfInteger.Create("PSP\0", ConvertHex(Config.FindEntry("BOOTPSP0"))));
            BWPS.Add(Blaze.TdfString.Create("SNA\0", Config.FindEntry("BOOTSNA0")));
            QOSS.Add(Blaze.TdfStruct.Create("BWPS", BWPS));
            QOSS.Add(Blaze.TdfInteger.Create("LNP\0", ConvertHex(Config.FindEntry("LNP"))));
            List<Blaze.Tdf> LTPS1 = new List<Blaze.Tdf>();            
            LTPS1.Add(Blaze.TdfString.Create("PSA\0", Config.FindEntry("BOOTPSA1")));
            LTPS1.Add(Blaze.TdfInteger.Create("PSP\0", ConvertHex(Config.FindEntry("BOOTPSP1"))));
            LTPS1.Add(Blaze.TdfString.Create("SNA\0", Config.FindEntry("BOOTSNA1")));
            List<Blaze.Tdf> LTPS2 = new List<Blaze.Tdf>();
            LTPS2.Add(Blaze.TdfString.Create("PSA\0", Config.FindEntry("BOOTPSA2")));
            LTPS2.Add(Blaze.TdfInteger.Create("PSP\0", ConvertHex(Config.FindEntry("BOOTPSP2"))));
            LTPS2.Add(Blaze.TdfString.Create("SNA\0", Config.FindEntry("BOOTSNA2")));
            List<Blaze.Tdf> LTPS3 = new List<Blaze.Tdf>();
            LTPS3.Add(Blaze.TdfString.Create("PSA\0", Config.FindEntry("BOOTPSA3")));
            LTPS3.Add(Blaze.TdfInteger.Create("PSP\0", ConvertHex(Config.FindEntry("BOOTPSP3"))));
            LTPS3.Add(Blaze.TdfString.Create("SNA\0", Config.FindEntry("BOOTSNA3")));
            List<Blaze.TdfStruct> LTPS = new List<Blaze.TdfStruct>();
            LTPS.Add(Blaze.CreateStructStub(LTPS1));
            LTPS.Add(Blaze.CreateStructStub(LTPS2));
            LTPS.Add(Blaze.CreateStructStub(LTPS3));
            t = ConvertStringList(Config.FindEntry("LTPS"));
            QOSS.Add(Blaze.TdfDoubleList.Create("LTPS", 1, 3, t, LTPS, 3));
            QOSS.Add(Blaze.TdfInteger.Create("SVID", ConvertHex(Config.FindEntry("SVID"))));
            Result.Add(Blaze.TdfStruct.Create("QOSS", QOSS));
            Result.Add(Blaze.TdfString.Create("RSRC", Config.FindEntry("RSRC")));
            Result.Add(Blaze.TdfString.Create("SVER", Config.FindEntry("SVER")));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateBootPacket02(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            List<Blaze.Tdf> PSSList = new List<Blaze.Tdf>();
            PSSList.Add(Blaze.TdfString.Create("ADRS", Config.FindEntry("PSSADRS")));
            //Blaze.TdfInteger csig = Blaze.TdfInteger.Create("CSIG", ConvertHex(Config.FindEntry("PSSCSIG")));
            //csig.Type = 2;
            PSSList.Add(Blaze.TdfBlob.Create("CSIG"));
            PSSList.Add(Blaze.TdfString.Create("PJID", Config.FindEntry("PSSPJID")));
            PSSList.Add(Blaze.TdfInteger.Create("PORT", ConvertHex(Config.FindEntry("PSSPORT"))));
            PSSList.Add(Blaze.TdfInteger.Create("RPRT", ConvertHex(Config.FindEntry("PSSRPRT"))));
            PSSList.Add(Blaze.TdfInteger.Create("TIID", ConvertHex(Config.FindEntry("PSSTIID"))));
            Result.Add(Blaze.TdfStruct.Create("PSS\0", PSSList));

            List<Blaze.Tdf> TELEList = new List<Blaze.Tdf>();
            TELEList.Add(Blaze.TdfString.Create("ADRS", Config.FindEntry("IP")));
            TELEList.Add(Blaze.TdfInteger.Create("ANON", ConvertHex(Config.FindEntry("TELEANON"))));
            TELEList.Add(Blaze.TdfString.Create("DISA", Config.FindEntry("TELEDISA")));
            TELEList.Add(Blaze.TdfString.Create("FILT", Config.FindEntry("TELEFILT")));
            TELEList.Add(Blaze.TdfInteger.Create("LOC\0", ConvertHex(Config.FindEntry("TELELOC"))));
            TELEList.Add(Blaze.TdfString.Create("NOOK", Config.FindEntry("TELENOOK")));
            TELEList.Add(Blaze.TdfInteger.Create("PORT", ConvertHex(Config.FindEntry("TELEPORT"))));
            TELEList.Add(Blaze.TdfInteger.Create("SDLY", ConvertHex(Config.FindEntry("TELESDLY"))));
            TELEList.Add(Blaze.TdfString.Create("SESS", "JMhnT9dXSED"));
            byte[] skey = { 0x5E, 0x8A, 0xCB, 0xDD, 0xF8, 0xEC, 0xC1, 0x95, 0x98, 0x99, 0xF9, 0x94, 0xC0, 0xAD, 0xEE, 0xFC, 0xCE, 0xA4, 0x87, 0xDE, 0x8A, 0xA6, 0xCE, 0xDC, 0xB0, 0xEE, 0xE8, 0xE5, 0xB3, 0xF5, 0xAD, 0x9A, 0xB2, 0xE5, 0xE4, 0xB1, 0x99, 0x86, 0xC7, 0x8E, 0x9B, 0xB0, 0xF4, 0xC0, 0x81, 0xA3, 0xA7, 0x8D, 0x9C, 0xBA, 0xC2, 0x89, 0xD3, 0xC3, 0xAC, 0x98, 0x96, 0xA4, 0xE0, 0xC0, 0x81, 0x83, 0x86, 0x8C, 0x98, 0xB0, 0xE0, 0xCC, 0x89, 0x93, 0xC6, 0xCC, 0x9A, 0xE4, 0xC8, 0x99, 0xE3, 0x82, 0xEE, 0xD8, 0x97, 0xED, 0xC2, 0xCD, 0x9B, 0xD7, 0xCC, 0x99, 0xB3, 0xE5, 0xC6, 0xD1, 0xEB, 0xB2, 0xA6, 0x8B, 0xB8, 0xE3, 0xD8, 0xC4, 0xA1, 0x83, 0xC6, 0x8C, 0x9C, 0xB6, 0xF0, 0xD0, 0xC1, 0x93, 0x87, 0xCB, 0xB2, 0xEE, 0x88, 0x95, 0xD2, 0x80, 0x80 };
            string skeys = "";
            foreach (byte b in skey)
                skeys += (char)b;
            TELEList.Add(Blaze.TdfString.Create("SKEY", skeys));
            TELEList.Add(Blaze.TdfInteger.Create("SPCT", ConvertHex(Config.FindEntry("TELESPCT"))));
            TELEList.Add(Blaze.TdfString.Create("STIM", ""));
            Result.Add(Blaze.TdfStruct.Create("TELE", TELEList));

            List<Blaze.Tdf> TICKList = new List<Blaze.Tdf>();
            TICKList.Add(Blaze.TdfString.Create("ADRS", Config.FindEntry("IP")));
            TICKList.Add(Blaze.TdfInteger.Create("PORT", ConvertHex(Config.FindEntry("TICKPORT"))));
            TICKList.Add(Blaze.TdfString.Create("SKEY", Config.FindEntry("TICKSKEY")));
            Result.Add(Blaze.TdfStruct.Create("TICK", TICKList));

            List<Blaze.Tdf> UROPList = new List<Blaze.Tdf>();
            UROPList.Add(Blaze.TdfInteger.Create("TMOP", ConvertHex(Config.FindEntry("UROPTMOP"))));
            UROPList.Add(Blaze.TdfInteger.Create("UID\0", player.UserID));
            Result.Add(Blaze.TdfStruct.Create("UROP", UROPList));

            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));            
            
        }
        public static void CreateBootPacket03(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            List<Blaze.Tdf> INFO = new List<Blaze.Tdf>();
            INFO.Add(Blaze.TdfTrippleVal.Create("BOID", new Blaze.TrippleVal(0x19, 0x1, 0x28557f3)));
            INFO.Add(Blaze.TdfInteger.Create("FLGS", 4));
            List<Blaze.Tdf> LID = new List<Blaze.Tdf>();
            LID.Add(Blaze.TdfString.Create("LNM\0", "friendList"));
            LID.Add(Blaze.TdfInteger.Create("TYPE", 1));
            INFO.Add(Blaze.TdfStruct.Create("LID\0", LID));
            INFO.Add(Blaze.TdfInteger.Create("LMS\0", 0xC8));
            INFO.Add(Blaze.TdfInteger.Create("PRID", 0));
            List<Blaze.Tdf> tmp = new List<Blaze.Tdf>();
            tmp.Add(Blaze.TdfStruct.Create("INFO", INFO));
            tmp.Add(Blaze.TdfInteger.Create("OFRC", 0));
            tmp.Add(Blaze.TdfInteger.Create("TOCT", 0));
            List<Blaze.TdfStruct> tmp2 = new List<Blaze.TdfStruct>();
            tmp2.Add(Blaze.CreateStructStub(tmp));
            Result.Add(Blaze.TdfList.Create("LMAP", 3, 1, tmp2));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateBootPacket04(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateServerTimePacket(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            Result.Add(Blaze.TdfInteger.Create("STIM", Blaze.GetUnixTimeStamp()));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateAuthPacket01(Player.PlayerInfo player, Blaze.Packet p)
        {
            uint t = Blaze.GetUnixTimeStamp();
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            Result.Add(Blaze.TdfInteger.Create("AGUP", 0));
            Result.Add(Blaze.TdfString.Create("LDHT", ""));
            Result.Add(Blaze.TdfInteger.Create("NTOS", 0));
            Result.Add(Blaze.TdfString.Create("PCTK", player.AuthString));
            Result.Add(Blaze.TdfString.Create("PRIV", ""));
            List<Blaze.Tdf> SESS = new List<Blaze.Tdf>();
            SESS.Add(Blaze.TdfInteger.Create("BUID", player.PlayerID));
            SESS.Add(Blaze.TdfInteger.Create("FRST", 0));
            SESS.Add(Blaze.TdfString.Create("KEY\0", "11229301_9b171d92cc562b293e602ee8325612e7"));
            SESS.Add(Blaze.TdfInteger.Create("LLOG", t));
            SESS.Add(Blaze.TdfString.Create("MAIL", ""));
            List<Blaze.Tdf> PDTL = new List<Blaze.Tdf>();
            PDTL.Add(Blaze.TdfString.Create("DSNM", player.Name));
            PDTL.Add(Blaze.TdfInteger.Create("LAST", t));
            PDTL.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
            PDTL.Add(Blaze.TdfInteger.Create("STAS", 0));
            PDTL.Add(Blaze.TdfInteger.Create("XREF", 0));
            PDTL.Add(Blaze.TdfInteger.Create("XTYP", 0));
            SESS.Add(Blaze.TdfStruct.Create("PDTL", PDTL));
            SESS.Add(Blaze.TdfInteger.Create("UID\0", player.UserID));
            Result.Add(Blaze.TdfStruct.Create("SESS", SESS));
            Result.Add(Blaze.TdfInteger.Create("SPAM", 0));
            Result.Add(Blaze.TdfString.Create("THST", ""));
            Result.Add(Blaze.TdfString.Create("TSUI", ""));
            Result.Add(Blaze.TdfString.Create("TURI", ""));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateAuthPacket02(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            List<Blaze.Tdf> DATA = new List<Blaze.Tdf>();
            DATA.Add(Blaze.TdfUnion.Create("ADDR"));
            DATA.Add(Blaze.TdfString.Create("BPS\0", ""));
            DATA.Add(Blaze.TdfString.Create("CTY\0", ""));
            DATA.Add(Blaze.TdfIntegerList.Create("CVAR", 0, null));
            List<long> l1 = new List<long>(); l1.Add(0x70001);
            List<long> l2 = new List<long>(); l2.Add(0x22);
            DATA.Add(Blaze.TdfDoubleList.Create("DMAP", 0, 0, l1, l2, 1));
            DATA.Add(Blaze.TdfInteger.Create("HWFG", 0));
            List<Blaze.Tdf> QDAT = new List<Blaze.Tdf>();
            QDAT.Add(Blaze.TdfInteger.Create("DBPS", 0));
            QDAT.Add(Blaze.TdfInteger.Create("NATT", NAT_Type));
            QDAT.Add(Blaze.TdfInteger.Create("UBPS", 0));
            DATA.Add(Blaze.TdfStruct.Create("QDAT", QDAT));
            DATA.Add(Blaze.TdfInteger.Create("UATT", 0));
            Result.Add(Blaze.TdfStruct.Create("DATA", DATA));
            List<Blaze.Tdf> USER = new List<Blaze.Tdf>();
            USER.Add(Blaze.TdfInteger.Create("AID\0", player.UserID));
            USER.Add(Blaze.TdfInteger.Create("ALOC", 0x64654445));
            USER.Add(Blaze.TdfBlob.Create("EXBB", new byte[0]));
            USER.Add(Blaze.TdfInteger.Create("EXID", 0));
            USER.Add(Blaze.TdfInteger.Create("ID\0\0", player.PlayerID));
            USER.Add(Blaze.TdfString.Create("NAME", player.Name));
            Result.Add(Blaze.TdfStruct.Create("USER", USER));

            SendPacket(player, Blaze.CreatePacket(0x7802, 2, 0, 0x2000, 0, Result));

            Result = new List<Blaze.Tdf>();
            Result.Add(Blaze.TdfInteger.Create("FLGS", 3));
            Result.Add(Blaze.TdfInteger.Create("ID\0\0", player.PlayerID));

            SendPacket(player, Blaze.CreatePacket(0x7802, 5, 0, 0x2000, 0, Result));
        }
        public enum LoginErrorCode : ushort
        {
            SERVERUNAVAILABLE = 0,
            EMAILNOTFOUND = 0xB,
            WRONGPASSWORD = 0x0C,
            EMAILALREADYINUSE = 0x0F,
            AGERESTRICTION = 0x10,
            INVALIDACCOUNT = 0x11, // 0x17, 0x28, 0x29, 0x2B, 0x2C
            BANNEDACCOUNT = 0x13, // 0x20
            INVALIDINFORMATION = 0x15,
            INVALIDEMAIL = 0x16,
            LEGALGUARDIANREQUIRED = 0x2A,
            CODEREQUIRED = 0x32, // game attempts to 'consumeCode', using a dummy key which currently cannot be changed (since Origin is supposed to handle the key)
            KEYCODEALREADYINUSE = 0x33, // probably in response to 'consumeCode' packet
            INVALIDCERBERUSKEY = 0x34, // leftover from Mass Effect 2 (probably 0x32 and 0x33 as well)
            SERVERUNAVAILABLEFINAL = 0x4001,
            FAILEDNOLOGINACTION = 0x4004,
            SERVERUNAVAILABLENOTHING = 0x4005,
            CONNECTIONLOST = 0x4007 /* Technically impossible, because the client must have a connection
                                     * with the server in order to receive this "connection lost" error code.
                                     * Could be used as "server stealthly unavailable" */
        }
        public static void SendLoginErrorPacket(Player.PlayerInfo player, Blaze.Packet p, LoginErrorCode errorcode)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            Result.Add(Blaze.TdfString.Create("PNAM", ""));
            Result.Add(Blaze.TdfInteger.Create("UID\0", 0));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, (ushort)errorcode, 0x3000, p.ID, Result));
            Logger.Log("[Main Server Handler " + player.ID + "][SendLoginErrorPacket] " + player.IP + " => "  + errorcode + " (" + errorcode.ToString("X") + ")", Color.DarkGoldenrod);
        }
        public static void CreateAuthPacket01_28(Player.PlayerInfo player, Blaze.Packet p)
        {
            uint t = Blaze.GetUnixTimeStamp();

            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            //Result.Add(Blaze.TdfInteger.Create("AGUP", 0));
            Result.Add(Blaze.TdfString.Create("LDHT", ""));
            Result.Add(Blaze.TdfInteger.Create("NTOS", 0));
            Result.Add(Blaze.TdfString.Create("PCTK", player.AuthString));

            List<Blaze.TdfStruct> playerentries = new List<Blaze.TdfStruct>();

            List<Blaze.Tdf> PlayerEntry = new List<Blaze.Tdf>();
            PlayerEntry.Add(Blaze.TdfString.Create("DSNM", player.Name));
            PlayerEntry.Add(Blaze.TdfInteger.Create("LAST", t));
            PlayerEntry.Add(Blaze.TdfInteger.Create("PID\0", player.PlayerID));
            PlayerEntry.Add(Blaze.TdfInteger.Create("STAS", 0));
            PlayerEntry.Add(Blaze.TdfInteger.Create("XREF", 0));
            PlayerEntry.Add(Blaze.TdfInteger.Create("XTYP", 0));

            playerentries.Add(Blaze.TdfStruct.Create("0", PlayerEntry));

            Result.Add(Blaze.TdfList.Create("PLST", 3, 1, playerentries));

            Result.Add(Blaze.TdfString.Create("PRIV", ""));
            Result.Add(Blaze.TdfString.Create("SKEY", "11229301_9b171d92cc562b293e602ee8325612e7"));
            Result.Add(Blaze.TdfInteger.Create("SPAM", 0));
            Result.Add(Blaze.TdfString.Create("THST", ""));
            Result.Add(Blaze.TdfString.Create("TSUI", ""));
            Result.Add(Blaze.TdfString.Create("TURI", ""));
            Result.Add(Blaze.TdfInteger.Create("UID\0", player.UserID));
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, Result));
        }
        public static void CreateGameStartPacket(Player.PlayerInfo player, Blaze.Packet p)
        {
            List<Blaze.Tdf> res;
            GameManager.GameInfo game = GameManager.GameInfo.CreateGame(player);
            List<Blaze.Tdf> content = Blaze.ReadPacketContent(p);
            game.ATTR = (Blaze.TdfDoubleList)content[0];
            game.Attributes = new List<GameManager.GameInfo.Attribut>();
            //Fetch and edit creation infos
            List<string> attribname = (List<string>)game.ATTR.List1;
            List<string> attribval = (List<string>)game.ATTR.List2;
            for (int i = 0; i < attribname.Count; i++)
            {
                GameManager.GameInfo.Attribut a = new GameManager.GameInfo.Attribut();
                a.Name = attribname[i];
                a.Value = attribval[i];
                game.Attributes.Add(a);
            }
            game.MakeATTR();
            //Create Creation response Packet
            res = new List<Blaze.Tdf>();
            res.Add(Blaze.TdfInteger.Create("GID\0", game.ID));
            MemoryStream m = new MemoryStream();
            byte[] buff = Blaze.CreatePacket(0x4, 0x1, 0x0, 0x1000, p.ID, res);
            //Add packet to response
            m.Write(buff, 0, buff.Length);
            //Create returnDedicatedServerToPool Packet
            buff = File.ReadAllBytes(loc + "replay\\04_14_01_res.bin");
            Blaze.Packet resp = Blaze.ReadBlazePacket(new MemoryStream(buff));
            List<Blaze.Tdf> form = Blaze.ReadPacketContent(resp);
            for (int i = 0; i < form.Count; i++)
            {
                Blaze.Tdf tdf = form[i];
                switch (tdf.Label)
                {
#region GAME
                    case "GAME":
                        Blaze.TdfStruct GAME = (Blaze.TdfStruct)tdf;
                        for (int j = 0; j < GAME.Values.Count; j++)
                        {
                            Blaze.Tdf tdf2 = GAME.Values[j];
                            switch(tdf2.Label)
                            {
                                case"ATTR":
                                    Blaze.TdfDoubleList ATTR = (Blaze.TdfDoubleList)tdf2;
                                    ATTR.List1 = game.ATTR.List1;
                                    ATTR.List2 = game.ATTR.List2;
                                    break;
                                case "ADMN":
                                    Blaze.TdfList ADMN = (Blaze.TdfList)tdf2;
                                    List<long> tmp = new List<long>();
                                    tmp.Add(player.PlayerID);
                                    ADMN.List = tmp;
                                    break;
                                case "GID ":
                                    Blaze.TdfInteger GID = (Blaze.TdfInteger)tdf2;
                                    GID.Value = game.ID;
                                    break;
                                case "GNAM":
                                    Blaze.TdfString GNAM = (Blaze.TdfString)tdf2;
                                    GNAM.Value = player.Name;
                                    break;
                                case "PHST":
                                    Blaze.TdfStruct PHST = (Blaze.TdfStruct)tdf2;
                                    Blaze.TdfInteger HPID = (Blaze.TdfInteger)PHST.Values[0];
                                    HPID.Value = player.PlayerID;
                                    break;
                                case "THST":
                                    Blaze.TdfStruct THST = (Blaze.TdfStruct)tdf2;
                                    Blaze.TdfInteger HPID2 = (Blaze.TdfInteger)THST.Values[0];
                                    HPID2.Value = player.PlayerID;
                                    break;
                                case"NQOS":
                                    Blaze.TdfStruct NQOS = (Blaze.TdfStruct)tdf2;
                                    Blaze.TdfInteger NATT = (Blaze.TdfInteger)NQOS.Values[1];
                                    NATT.Value = NAT_Type;
                                    break;
                                case "HNET":
                                    Blaze.TdfList HNET = (Blaze.TdfList)tdf2;
                                    List<Blaze.Tdf> entry = ((List<Blaze.TdfStruct>)HNET.List)[0].Values;
                                    Blaze.TdfStruct EXIP = (Blaze.TdfStruct)entry[0];
                                    Blaze.TdfInteger IP = (Blaze.TdfInteger)EXIP.Values[0];
                                    Blaze.TdfInteger PORT = (Blaze.TdfInteger)EXIP.Values[1];
                                    IP.Value = player.EXIP.IP;
                                    PORT.Value = player.EXIP.PORT;
                                    Blaze.TdfStruct INIP = (Blaze.TdfStruct)entry[1];
                                    IP = (Blaze.TdfInteger)INIP.Values[0];
                                    PORT = (Blaze.TdfInteger)INIP.Values[1];
                                    IP.Value = player.INIP.IP;
                                    PORT.Value = player.INIP.PORT;
                                    break;
                            }
                        }
                        break;
#endregion
#region PROS
                    case "PROS":
                        Blaze.TdfList PROS = (Blaze.TdfList)tdf;
                        List<Blaze.Tdf> result = ((List<Blaze.TdfStruct>)PROS.List)[0].Values;
                        for (int j = 0; j < result.Count; j++)
                        {
                            Blaze.Tdf tdf2 = result[j];
                            switch (tdf2.Label)
                            {
                                case "GID ":
                                    Blaze.TdfInteger GID = (Blaze.TdfInteger)tdf2;
                                    GID.Value = game.ID;
                                    break;
                                case "NAME":
                                    Blaze.TdfString NAME = (Blaze.TdfString)tdf2;
                                    NAME.Value = player.Name;
                                    break;
                                case "PID ":
                                    Blaze.TdfInteger PID = (Blaze.TdfInteger)tdf2;
                                    PID.Value = player.PlayerID;
                                    break;
                                case "UID ":
                                    Blaze.TdfInteger UID = (Blaze.TdfInteger)tdf2;
                                    UID.Value = player.UserID;
                                    break;
                                case "PNET":
                                    tdf2 = GetTdfUnionIP(player, "PNET");
                                    break;
                            }
                        }
                        break;
#endregion
                }
            }
            buff = Blaze.CreatePacket(0x4, 0x14, 0, 0x2000, 0, form);
            //Add packet to response
            m.Write(buff, 0, buff.Length);
            //Create Player Info Packet
            buff = File.ReadAllBytes(loc + "replay\\04_14_02_res.bin");
            resp = Blaze.ReadBlazePacket(new MemoryStream(buff));
            form = Blaze.ReadPacketContent(resp);
            Blaze.TdfStruct DATA = (Blaze.TdfStruct)form[0];
            DATA.Values[0] = GetTdfUnionIP(player, "ADDR");
            Blaze.TdfStruct QDAT = (Blaze.TdfStruct)DATA.Values[7];
            Blaze.TdfInteger NATT2 = (Blaze.TdfInteger)QDAT.Values[1];
            NATT2.Value = NAT_Type;
            Blaze.TdfInteger USID = (Blaze.TdfInteger)form[1];
            USID.Value = player.UserID;
            buff = Blaze.CreatePacket(0x7802, 0x1, 0, 0x2000, 0, form);
            //Add packet to response
            m.Write(buff, 0, buff.Length);  
            //Send to player
            SendPacket(player, m.ToArray());
        }
        public static void CreateJoinGamePacket(Player.PlayerInfo player, Blaze.Packet p)
        {
            //Player is waiting for host infos to join
            player.SetJoinWaitState(true);
            //Find Game
            GameManager.GameInfo game = GameManager.FindFirstActive();
            if (game == null)
            {
                SendEmpty(player, p, 0x1000);
                return;
            }
            //Add joiner to Game
            game.OtherPlayers.Add(player);
            game.Update = true;
            //Tell Host
            CreatePlayerJoinInfoForHost(game, player);
            //Create Packet for Joining Player
            MemoryStream m = new MemoryStream();
            //MatchMaking response
            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            result.Add(Blaze.TdfInteger.Create("MSID", game.MID));
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            m.Write(buff, 0, buff.Length);
            SendPacket(player, m.ToArray()); 
            //Create Asynchron Player Infos
            m = new MemoryStream();
            //First Creator
            buff = CreateMPPlayerInfo(game, game.Creator);
            m.Write(buff, 0, buff.Length);
            //Then all others, except joining Player
            foreach (Player.PlayerInfo pl in game.OtherPlayers)
                if (pl.ID != player.ID)
                {
                    buff = CreateMPPlayerInfo(game, pl);
                    m.Write(buff, 0, buff.Length);
                }
            //Create ReturnDedicatedServerToPool Packet
            buff = CreateJoiningDedicateServerInfo(game, player);
            m.Write(buff, 0, buff.Length);
            //Send Joiner
            SendPacket(player, m.ToArray());
        }
        public static void CreatePlayerJoinInfoForHost(GameManager.GameInfo game, Player.PlayerInfo player)
        {
            try
            {
                MemoryStream res = new MemoryStream();
                Blaze.Packet resp;
                List<Blaze.Tdf> form;
                //Create Player Info Packet
                byte[] buff = CreateMPPlayerInfo(game, player);
                res.Write(buff, 0, buff.Length);
                //Create joinGameByGroup Packet
                buff = File.ReadAllBytes(loc + "replay\\04_15_res.bin");
                resp = Blaze.ReadBlazePacket(new MemoryStream(buff));
                form = Blaze.ReadPacketContent(resp);
                Blaze.TdfInteger GID = (Blaze.TdfInteger)form[0];
                GID.Value = game.ID;
                Blaze.TdfStruct PDAT = (Blaze.TdfStruct)form[1];
                GID = (Blaze.TdfInteger)PDAT.Values[2];
                GID.Value = game.ID;
                Blaze.TdfString NAME = (Blaze.TdfString)PDAT.Values[4];
                NAME.Value = player.Name;
                Blaze.TdfInteger PID = (Blaze.TdfInteger)PDAT.Values[5];
                PID.Value = player.PlayerID;
                PDAT.Values[6] = GetTdfUnionIP(player, "PNET");
                Blaze.TdfInteger SID = (Blaze.TdfInteger)PDAT.Values[7];
                SID.Value = game.OtherPlayers.Count;
                Blaze.TdfInteger UID = (Blaze.TdfInteger)PDAT.Values[13];
                UID.Value = player.UserID;
                buff = Blaze.CreatePacket(0x4, 0x15, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                // Create Player Info
                buff = File.ReadAllBytes(loc + "replay\\04_14_02_res.bin");
                resp = Blaze.ReadBlazePacket(new MemoryStream(buff));
                form = Blaze.ReadPacketContent(resp);
                Blaze.TdfStruct DATA = (Blaze.TdfStruct)form[0];
                DATA.Values[0] = GetTdfUnionIP(player, "ADDR");
                Blaze.TdfStruct QDAT = (Blaze.TdfStruct)DATA.Values[7];
                Blaze.TdfInteger NATT = (Blaze.TdfInteger)QDAT.Values[1];
                NATT.Value = NAT_Type;
                Blaze.TdfInteger USID = (Blaze.TdfInteger)form[1];
                USID.Value = player.UserID;
                buff = Blaze.CreatePacket(0x7802, 0x1, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                SendPacket(game.Creator, res.ToArray());
            }
            catch(Exception ex)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][CreatePlayerJoinInfoForHost] Error:\n" + GetExceptionMessage(ex), Color.Red);
            }
        }
        public static byte[] CreateJoiningDedicateServerInfo(GameManager.GameInfo game, Player.PlayerInfo player)
        {
            try
            {
                MemoryStream res = new MemoryStream();
                byte[] buff = File.ReadAllBytes(loc + "replay\\04_14_03_res.bin");
                Blaze.Packet pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
                List<Blaze.Tdf> form = Blaze.ReadPacketContent(pform);
                //Create returnDedicatedServerToPool Packet
#region GAME
                Blaze.TdfStruct GAME = (Blaze.TdfStruct)form[0];
                Blaze.TdfDoubleList ATTR = (Blaze.TdfDoubleList)GAME.Values[1];
                ATTR.List1 = game.ATTR.List1;
                ATTR.List2 = game.ATTR.List2;
                List<string> attrbname = (List<string>)ATTR.List1;
                List<string> attrbvalue = (List<string>)ATTR.List2;
                attrbname.Insert(7, "ME3gameState");
                //attrbvalue.Insert(7, "IN_LOBBY");
                attrbvalue.Insert(7, game.GetAttrValue("ME3gameState"));
                ATTR.List1 = attrbname;
                ATTR.List2 = attrbvalue;
                Blaze.TdfList ADMN = (Blaze.TdfList)GAME.Values[0];
                List<long> tmp = new List<long>();
                tmp.Add(game.Creator.PlayerID);
                foreach (Player.PlayerInfo p in game.OtherPlayers)
                    tmp.Add(p.PlayerID);
                ADMN.List = tmp;
                ADMN.Count = tmp.Count;
                Blaze.TdfInteger GID = (Blaze.TdfInteger)GAME.Values[3];
                GID.Value = game.ID;
                Blaze.TdfString GNAM = (Blaze.TdfString)GAME.Values[4];
                GNAM.Value = game.Creator.Name;
                Blaze.TdfInteger GSET = (Blaze.TdfInteger)GAME.Values[6];
                GSET.Value = game.GAMESETTING;
                Blaze.TdfInteger GSID = (Blaze.TdfInteger)GAME.Values[7];
                GSID.Value = 0x4000000618E41C;
                Blaze.TdfInteger GSTA = (Blaze.TdfInteger)GAME.Values[8];
                GSTA.Value = game.GAMESTATE;
                Blaze.TdfList HNET = (Blaze.TdfList)GAME.Values[10];
                List<Blaze.Tdf> entry = ((List<Blaze.TdfStruct>)HNET.List)[0].Values;
                Blaze.TdfStruct EXIP = (Blaze.TdfStruct)entry[0];
                Blaze.TdfInteger IP = (Blaze.TdfInteger)EXIP.Values[0];
                Blaze.TdfInteger PORT = (Blaze.TdfInteger)EXIP.Values[1];
                IP.Value = (uint)game.Creator.EXIP.IP;
                PORT.Value = game.Creator.EXIP.PORT;
                Blaze.TdfStruct INIP = (Blaze.TdfStruct)entry[1];
                IP = (Blaze.TdfInteger)INIP.Values[0];
                PORT = (Blaze.TdfInteger)INIP.Values[1];
                IP.Value = (uint)game.Creator.INIP.IP;
                PORT.Value = game.Creator.INIP.PORT;
                Blaze.TdfInteger HSES = (Blaze.TdfInteger)GAME.Values[11];
                HSES.Value = 0x112888C1;
                Blaze.TdfStruct NQOS = (Blaze.TdfStruct)GAME.Values[14];
                Blaze.TdfInteger NATT = (Blaze.TdfInteger)NQOS.Values[1];
                NATT.Value = NAT_Type;
                Blaze.TdfStruct PHST = (Blaze.TdfStruct)GAME.Values[19];
                Blaze.TdfInteger HPID = (Blaze.TdfInteger)PHST.Values[0];
                HPID.Value = game.Creator.PlayerID;
                Blaze.TdfInteger PRES = (Blaze.TdfInteger)GAME.Values[20];
                PRES.Value = 0x1;
                Blaze.TdfInteger SEED = (Blaze.TdfInteger)GAME.Values[23];
                SEED.Value = 0x2CF2048F;
                Blaze.TdfStruct THST = (Blaze.TdfStruct)GAME.Values[25];
                HPID = (Blaze.TdfInteger)THST.Values[0];
                HPID.Value = game.Creator.PlayerID;
                Blaze.TdfString UUID = (Blaze.TdfString)GAME.Values[26];
                UUID.Value = "f5193367-c991-4429-aee4-8d5f3adab938";
                #endregion
#region PROS
                Blaze.TdfList PROS = (Blaze.TdfList)form[1];
                for (int i = 0; i < game.OtherPlayers.Count + 1; i++)
                {
                    entry = ((List<Blaze.TdfStruct>)PROS.List)[i].Values;
                    GID = (Blaze.TdfInteger)entry[2];
                    GID.Value = game.ID;
                    Player.PlayerInfo tmppl;
                    switch (i)
                    {
                        case 0:
                            tmppl = game.Creator;
                            break;
                        default:
                            if ((i - 1) < game.OtherPlayers.Count)
                                tmppl = game.OtherPlayers[i - 1];
                            else
                                tmppl = player;
                            break;
                    }
                    Blaze.TdfString NAME = (Blaze.TdfString)entry[4];
                    NAME.Value = tmppl.Name;
                    Blaze.TdfInteger PID = (Blaze.TdfInteger)entry[5];
                    PID.Value = tmppl.PlayerID;
                    Blaze.TdfInteger SID = (Blaze.TdfInteger)entry[8];
                    SID.Value = i;
                    entry[6] = GetTdfUnionIP(tmppl, "PNET");
                    Blaze.TdfInteger STAT = (Blaze.TdfInteger)entry[9];
                    if (tmppl.ID == player.ID)
                        STAT.Value = 2;
                    Blaze.TdfInteger UID = (Blaze.TdfInteger)entry[13];
                    UID.Value = tmppl.PlayerID;
                }
                List<Blaze.TdfStruct> list = (List<Blaze.TdfStruct>)PROS.List;
                if (game.OtherPlayers.Count == 2)
                {
                    list.RemoveAt(3);
                    PROS.Count = 3;
                }
                if (game.OtherPlayers.Count == 1)
                {
                    list.RemoveAt(3);
                    list.RemoveAt(2);
                    PROS.Count = 2;
                }
                if (game.OtherPlayers.Count == 0)
                {
                    list.RemoveAt(3);
                    list.RemoveAt(2);
                    list.RemoveAt(1);
                    PROS.Count = 1;
                }
                PROS.List = list;
                #endregion
#region REAS
                Blaze.TdfUnion REAS = (Blaze.TdfUnion)form[2];
                Blaze.TdfStruct VALU = (Blaze.TdfStruct)REAS.UnionContent;
                Blaze.TdfInteger MSID = (Blaze.TdfInteger)VALU.Values[2];
                MSID.Value = game.MID;
                Blaze.TdfInteger USID = (Blaze.TdfInteger)VALU.Values[4];
                USID.Value = player.PlayerID;
                #endregion
                buff = Blaze.CreatePacket(0x04, 0x14, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                //Create PlayerInfo Packet
                buff = File.ReadAllBytes(loc + "replay\\04_14_02_res.bin");
                pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
                form = Blaze.ReadPacketContent(pform);
                Blaze.TdfStruct DATA = (Blaze.TdfStruct)form[0];
                DATA.Values[0] = GetTdfUnionIP(player, "ADDR");
                Blaze.TdfStruct QDAT = (Blaze.TdfStruct)DATA.Values[7];
                NATT = (Blaze.TdfInteger)QDAT.Values[1];
                NATT.Value = NAT_Type;
                USID = (Blaze.TdfInteger)form[1];
                USID.Value = player.PlayerID;
                buff = Blaze.CreatePacket(0x7802, 0x1, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                return res.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][CreateJoiningDedicateServerInfo] Error:\n" + GetExceptionMessage(ex), Color.Red);
                return new byte[0];
            }
        }        
        public static byte[] CreateMPPlayerInfo(GameManager.GameInfo game, Player.PlayerInfo player)
        {
            try
            {
                MemoryStream res = new MemoryStream();
                byte[] buff = File.ReadAllBytes(loc + "replay\\7802_02_01_res.bin");
                Blaze.Packet pform = Blaze.ReadBlazePacket(new MemoryStream(buff));
                List<Blaze.Tdf> form = Blaze.ReadPacketContent(pform);
                Blaze.TdfStruct DATA = (Blaze.TdfStruct)form[0];
                DATA.Values[0] = GetTdfUnionIP(player, "ADDR");
                Blaze.TdfStruct QDAT = (Blaze.TdfStruct)DATA.Values[7];
                Blaze.TdfInteger NATT = (Blaze.TdfInteger)QDAT.Values[1];
                NATT.Value = NAT_Type;
                Blaze.TdfList ULST = (Blaze.TdfList)DATA.Values[9];
                List<Blaze.TrippleVal> list = (List<Blaze.TrippleVal>)ULST.List;
                Blaze.TrippleVal tval = list[0];
                tval.v3 = game.ID;
                list[0] = tval;
                ULST.List = list;
                Blaze.TdfStruct USER = (Blaze.TdfStruct)form[1];
                Blaze.TdfInteger AID = (Blaze.TdfInteger)USER.Values[0];
                Blaze.TdfInteger ID = (Blaze.TdfInteger)USER.Values[4];
                Blaze.TdfString NAME = (Blaze.TdfString)USER.Values[5];
                AID.Value = player.PlayerID;
                ID.Value = player.PlayerID;
                NAME.Value = player.Name;
                buff = Blaze.CreatePacket(0x7802, 0x2, 0, 0x2000, 0, form);
                res.Write(buff, 0, buff.Length);
                List<Blaze.Tdf> result = new List<Blaze.Tdf>();
                result.Add(Blaze.TdfInteger.Create("FLGS", 3));
                result.Add(Blaze.TdfInteger.Create("ID", player.PlayerID));
                buff = Blaze.CreatePacket(0x7802, 0x5, 0, 0x2000, 0, result);
                res.Write(buff, 0, buff.Length);
                return res.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log("[Main Server Handler " + player.ID + "][CreateMPPlayerInfo] Error:\n" + GetExceptionMessage(ex), Color.Red);
                return new byte[0];
            }
        }
#endregion

#region Helpers
        public static long GetIPfromString(string s)
        {
            long res = 0;
            string[] parts = s.Split('.');
            if (parts.Length != 4)
                return 0;
            for (int i = 0; i < 4; i++)
            {
                uint v = Convert.ToUInt32(parts[i]);
                res |= (v << (3 - i) * 8);
            }
            return res;
        }
        public static string GetStringFromIP(long ip)
        {
            return (ip >> 24) + "." + ((ip >> 16) & 0xFF) + "." + ((ip >> 8) & 0xFF) + "." + (ip & 0xFF);
        }
        public static void SendPacket(Player.PlayerInfo player, byte[] buff)
        {
            player.ClientStream.Write(buff, 0, buff.Length);
            player.ClientStream.Flush();
            Logger.Log("[Main Server Handler " + player.ID + "] Send Response, len = " + buff.Length, Color.Blue);
            List<Blaze.Packet> packets = Blaze.FetchAllBlazePackets(new MemoryStream(buff));
            foreach (Blaze.Packet p in packets)
                Logger.Log("[->][INFO] " + Blaze.PacketToDescriber(p), Color.DarkGray, 3);
            Logger.DumpPacket(buff, player);
        }
        public static void SendEmpty(Player.PlayerInfo player, Blaze.Packet p, ushort Qtype)
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            SendPacket(player, Blaze.CreatePacket(p.Component, p.Command, 0, Qtype, p.ID, Result));
        }
        public static byte[] ReadContentSSL(SslStream sslStream)
        {
            MemoryStream res = new MemoryStream();
            try
            {
                byte[] buff = new byte[0x10000];                
                int bytesRead;
                sslStream.ReadTimeout = RWTimeout;
                while ((bytesRead = sslStream.Read(buff, 0, 0x10000)) > 0)
                {
                    res.Write(buff, 0, bytesRead);
                    if(CheckIfStreamComplete(res))
                        break;
                }
                sslStream.Flush();
                return res.ToArray();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("ReadContentSSL | " + GetExceptionMessage(e));
                return res.ToArray();
            }
        }
        public static byte[] ReadContent(NetworkStream stream)
        {
            try
            {
                byte[] buff = new byte[0x10000];
                MemoryStream res = new MemoryStream();
                int bytesRead;
                stream.ReadTimeout = RWTimeout;
                while ((bytesRead = stream.Read(buff, 0, 0x10000)) > 0)
                {
                    res.Write(buff, 0, bytesRead);
                    stream.Flush();
                    if (CheckIfStreamComplete(res))
                        break;
                }
                return res.ToArray();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("ReadContent | " + GetExceptionMessage(e));
                return new byte[0];
            }
        }
        public static byte[] ReadContentHttp(NetworkStream stream)
        {
            try
            {
                byte[] buff = new byte[0x10000];
                MemoryStream res = new MemoryStream();
                int bytesRead;
                stream.ReadTimeout = RWTimeout;
                while ((bytesRead = stream.Read(buff, 0, 0x10000)) > 0)
                {
                    res.Write(buff, 0, bytesRead);
                    stream.Flush();
                    if (bytesRead != 0x10000)
                        break;
                }
                return res.ToArray();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Print("ReadContentHTTP | " + GetExceptionMessage(e));
                return new byte[0];
            }
        }
        public static bool CheckIfStreamComplete(MemoryStream m)
        {
            m.Seek(0, 0);
            byte t = (byte)m.ReadByte();
            if (t == 0x17)
                m.Seek(5, 0);
            else
                m.Seek(0, 0);
            long len = 0;
            while (m.Position + len < m.Length)
            {
                m.Seek(m.Position + len, 0);
                Blaze.Packet p = Blaze.ReadBlazePacketHeader(m);
                len = p.Length + (p.extLength << 16);
                if (m.Position + len == m.Length)
                    return true;
            }
            m.Seek(m.Length, 0);
            return false;
        }
        public static long ConvertHex(string hex)
        {
            hex = hex.Trim();
            if (hex.StartsWith("0x"))
                hex = hex.Substring(2, hex.Length - 2);
            return Convert.ToInt64(hex, 16);
        }
        public static List<string> ConvertStringList(string data)
        {
            List<string> res = new List<string>();
            string t = data.Replace("{", "");
            string[] t2 = t.Split('}');
            foreach (string line in t2)
                if (line.Trim() != "")
                    res.Add(line.Trim());
            return res;
        }
        public static void ConvertDoubleStringList(string data, out List<string> list1, out List<string> list2)
        {
            List<string> res1 = new List<string>();
            List<string> res2 = new List<string>();
            string t = data.Replace("{", "");
            string[] t2 = t.Split('}');
            foreach (string line in t2)
                if (line.Trim() != "")
                {
                    string[] t3 = line.Trim().Split(';');
                    res1.Add(t3[0].Trim());
                    res2.Add(t3[1].Trim());
                }
            list1 = res1;
            list2 = res2;
        }
        public static void CreateBase64StringsFromTLK(string path, out List<string> list1, out List<string> list2)
        {
            List<string> res1 = new List<string>();
            List<string> res2 = new List<string>();
            byte[] tlkdata = File.ReadAllBytes(path);
            string tlkbase64 = Convert.ToBase64String(tlkdata);
            int pos = 0;
            int chunkn = 0;
            while (pos < tlkbase64.Length)
            {
                res1.Add("CHUNK_" + chunkn++);
                string chunkdata = "";
                if (tlkbase64.Length - pos > 255)
                {
                    chunkdata = tlkbase64.Substring(pos, 255);
                }
                else
                {
                    int len = tlkbase64.Length - pos;
                    chunkdata = tlkbase64.Substring(pos, len);
                }
                res2.Add(chunkdata);
                pos += chunkdata.Length;
            }
            bool run = true;
            while (run)
            {
                run = false;
                string tmp;
                for (int i = 0; i < res1.Count - 1; i++)
                    if (res1[i].CompareTo(res1[i + 1]) > 0)
                    {
                        tmp = res1[i];
                        res1[i] = res1[i + 1];
                        res1[i + 1] = tmp;
                        tmp = res2[i];
                        res2[i] = res2[i + 1];
                        res2[i + 1] = tmp;
                        run = true;
                    }
            }
            res1.Add("CHUNK_SIZE");
            res2.Add("255");
            res1.Add("DATA_SIZE");
            res2.Add(tlkbase64.Length.ToString());
            list1 = res1;
            list2 = res2;
        }
        public static void CreateBase64StringsFromCompressedCoalesced(string path, out List<string> list1, out List<string> list2)
        {
            List<string> res1 = new List<string>();
            List<string> res2 = new List<string>();
            byte[] indata = File.ReadAllBytes(path);
            MemoryStream zipout = new MemoryStream();
            MemoryStream res = new MemoryStream();
            ZLibStream outstream = new ZLibStream(zipout, CompressionMode.Compress, CompressionLevel.Level6);
            outstream.Write(indata, 0, indata.Length);
            outstream.Flush();
            outstream.Close();
            byte[] buffc = zipout.ToArray();
            res.WriteByte((byte)'N');
            res.WriteByte((byte)'I');
            res.WriteByte((byte)'B');
            res.WriteByte((byte)'C');
            WriteInt(res, 1);
            WriteInt(res, buffc.Length);
            WriteInt(res, indata.Length);
            res.Write(buffc, 0, buffc.Length);
            File.WriteAllBytes(loc + "logs\\test.bin", res.ToArray());
            string tlkbase64 = Convert.ToBase64String(res.ToArray());
            int pos = 0;
            int chunkn = 0;
            while (pos < tlkbase64.Length)
            {
                res1.Add("CHUNK_" + chunkn++);
                string chunkdata = "";
                if (tlkbase64.Length - pos > 255)
                {
                    chunkdata = tlkbase64.Substring(pos, 255);
                }
                else
                {
                    int len = tlkbase64.Length - pos;
                    chunkdata = tlkbase64.Substring(pos, len);
                }
                res2.Add(chunkdata);
                pos += chunkdata.Length;
            }
            bool run = true;
            while (run)
            {
                run = false;
                string tmp;
                for (int i = 0; i < res1.Count - 1; i++)
                    if (res1[i].CompareTo(res1[i + 1]) > 0)
                    {
                        tmp = res1[i];
                        res1[i] = res1[i + 1];
                        res1[i + 1] = tmp;
                        tmp = res2[i];
                        res2[i] = res2[i + 1];
                        res2[i + 1] = tmp;
                        run = true;
                    }
            }
            res1.Add("CHUNK_SIZE");
            res2.Add("255");
            res1.Add("DATA_SIZE");
            res2.Add(tlkbase64.Length.ToString());
            list1 = res1;
            list2 = res2;
        }
        public static void WriteInt(Stream s, int i)
        {
            s.Write(BitConverter.GetBytes(i), 0, 4);
        }
        public static bool ValidateAlways(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        public static string EndpointToString(EndPoint p)
        {
            return ((IPEndPoint)p).Address.ToString() + ":" + ((IPEndPoint)p).Port.ToString();
        }
        public static byte[] TLM3key = { 0x54, 0x68, 0x65, 0x20, 0x74, 0x72, 0x75, 0x74, 0x68, 0x20, 0x69, 0x73, 0x20, 0x62, 0x61, 0x63, 0x6B, 0x20, 0x69, 0x6E, 0x20, 0x73, 0x74, 0x79, 0x6C, 0x65, 0x2E };
        public static byte[] DecodeTLM3Line(byte[] buff)
        {
            int start = -1;
            for (int i = 0; i < buff.Length; i++)
                if (buff[i] == 0x2D)
                {
                    start = i + 1;
                    break;
                }
            if (start != -1)
                for (int i = start; i < buff.Length; i++)
                {
                    byte b = buff[i];
                    byte k = TLM3key[(i - start) % 0x1B];
                    if ((b ^ k) <= 0x80)
                        buff[i] = (byte)(b ^ k);
                    else
                        buff[i] = (byte)(k ^ (b - 0x80));
                }
            return buff;
        }
        public static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static string GetLiveBINI()
        {
            string live_bini = Config.FindEntry("LIVE_BINI");
            if (File.Exists(loc + live_bini))
            {
                return loc + live_bini;
            }
            else if (File.Exists(loc + "conf\\" + live_bini))
            {
                return loc + "conf\\" + live_bini;
            }
            return loc + "conf\\ME3BINI.bin";
        }
        public static string GetLiveTLK()
        {
            string live_tlk = Config.FindEntry("LIVE_TLK");
            if (File.Exists(loc + live_tlk))
            {
                return loc + live_tlk;
            }
            else if (File.Exists(loc + "conf\\" + live_tlk))
            {
                return loc + "conf\\" + live_tlk;
            }
            return loc + "conf\\ME3TLK.tlk";
        }
        public static string GetLiveTLK(string language)
        {
            string fileTLK = loc + "conf\\ME3TLK_" + language + ".tlk";
            if (File.Exists(fileTLK))
            {
                return fileTLK;
            }
            else
            {
                return GetLiveTLK();
            }
        }
        public static List<string> GetListOfPlayerFiles()
        {
            try
            {
                List<string> files = new List<string>(Directory.GetFiles(loc + "player\\", "*.txt"));
                // item removal, loop must be backwards...
                for (int i = (files.Count - 1); i >= 0; i--)
                {
                    string[] lines = File.ReadAllLines(files[i]);
                    if (lines.Length < 5)
                    {
                        files.RemoveAt(i);
                        continue;
                    }
                    if (!lines[0].StartsWith("PID="))
                        files.RemoveAt(i);
                }
                return files;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("GetListOfPlayerFiles | " + GetExceptionMessage(ex));
                return new List<string>();
            }
        }
        public static List<ME3MP_Profile> GetListOfActiveProfiles()
        {
            List<ME3MP_Profile> profiles = new List<ME3MP_Profile>();
            List<string> files = GetListOfPlayerFiles();
            foreach (string file in files)
            {
                ME3MP_Profile profile = ME3MP_Profile.InitializeFromFile(file);
                if (profile != null)
                    profiles.Add(profile);
            }
            return profiles;
        }
        public static Blaze.TdfUnion GetTdfUnionIP(Player.PlayerInfo player, string label)
        {
            List<Blaze.Tdf> list = new List<Blaze.Tdf>();
            list.Add(Blaze.TdfInteger.Create("IP\0\0", player.EXIP.IP));
            list.Add(Blaze.TdfInteger.Create("PORT", player.EXIP.PORT));
            Blaze.TdfStruct EXIP = Blaze.TdfStruct.Create("EXIP", list);
            list = new List<Blaze.Tdf>();
            list.Add(Blaze.TdfInteger.Create("IP\0\0", player.INIP.IP));
            list.Add(Blaze.TdfInteger.Create("PORT", player.INIP.PORT));
            Blaze.TdfStruct INIP = Blaze.TdfStruct.Create("INIP", list);
            list = new List<Blaze.Tdf>();
            list.Add(EXIP);
            list.Add(INIP);
            Blaze.TdfStruct VALU = Blaze.TdfStruct.Create("VALU", list);
            Blaze.TdfUnion union = Blaze.TdfUnion.Create(label, 0x02, VALU);
            //Logger.Log(GetStringFromIP(player.INIP.IP) + " " + GetStringFromIP(player.EXIP.IP), Color.Purple);
            return union;
        }
        public static string GetExceptionMessage(Exception exception, int innerLevel = 0)
        {
            string message;
            message = new string('>', innerLevel) + exception.GetType().FullName + ": " + exception.Message;
            if (exception is SocketException)
            {
                SocketException se = (SocketException)exception;
                message += "\n" + String.Format("ErrorCode: {0}, NativeErrorCode: {1}, SocketErrorCode: {2}", se.ErrorCode, se.NativeErrorCode, se.SocketErrorCode);
            }
            if (exception.InnerException != null)
            {
                message += "\n[InnerException] " + GetExceptionMessage(exception.InnerException, innerLevel + 1);
            }
            return message;
        }
#endregion

#region GaW Functions
        public static string GetResponseGaWAuthentication(string request, out string playername)
        {
            playername = "«undefined»";
            string auth1 = "playernotfound";
            string strSession = "default";
            Uri authUri = new Uri("gaw://" + request);
            long pID = ConvertHex(HttpUtility.ParseQueryString(authUri.Query).Get("auth"));
            Player.PlayerInfo targetPlayer = null;
            foreach (Player.PlayerInfo pl in Player.AllPlayers)
            {
                if (pID == pl.PlayerID)
                {
                    targetPlayer = pl;
                    break;
                }
            }
            if (targetPlayer != null)
            {
                playername = targetPlayer.Name;
                auth1 = targetPlayer.AuthString;
                strSession = pID.ToString("X");
            }
            else
            {
                pID = 0;
            }
            string content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            content += "<fulllogin>\r\n";
            content += " <canageup>0</canageup>\r\n";
            content += " <legaldochost/>\r\n";
            content += " <needslegaldoc>0</needslegaldoc>\r\n";
            content += " <pclogintoken>\r\n";
            content += "  " + auth1 + "\r\n"; // AUTH1
            content += " </pclogintoken>\r\n";
            content += " <privacypolicyuri/>\r\n";
            content += " <sessioninfo>\r\n";
            content += "  <blazeuserid>" + pID.ToString() + "</blazeuserid>\r\n"; // id1
            content += "  <isfirstlogin>0</isfirstlogin>\r\n";
            content += "  <sessionkey>" + strSession + "</sessionkey>\r\n"; // session
            content += "  <lastlogindatetime>1422639771</lastlogindatetime>\r\n";
            content += "  <email>abcdefghij@0123456789.com</email>\r\n";
            content += "  <personadetails>\r\n";
            content += "   <displayname>" + playername + "</displayname>\r\n"; // player name
            content += "   <lastauthenticated>1422639540</lastauthenticated>\r\n";
            content += "   <personaid>" + pID.ToString() + "</personaid>\r\n"; // id1
            content += "   <status>UNKNOWN</status>\r\n";
            content += "   <extid>0</extid>\r\n";
            content += "   <exttype>BLAZE_EXTERNAL_REF_TYPE_UNKNOWN</exttype>\r\n";
            content += "  </personadetails>\r\n";
            content += "  <userid>" + pID.ToString() + "</userid>\r\n"; // id2
            content += " </sessioninfo>\r\n";
            content += " <isoflegalcontactage>0</isoflegalcontactage>\r\n";
            content += " <toshost/>\r\n";
            content += " <termsofserviceuri/>\r\n";
            content += " <tosuri/>\r\n";
            content += "</fulllogin>\r\n";
            string header = CreateHttpHeader(content.Length);
            return header + content;
        }
        public static string GetResponseGaWRatings(string request, out string playername)
        {
            playername = "«undefined»";
            Uri getRatingsUri = new Uri("gaw://" + request);
            string session = Path.GetFileName(getRatingsUri.LocalPath);
            int[] ratings = GaWGetRatings(session + ".txt");
            Player.PlayerInfo targetPlayer = null;
            if (session != "default")
            {
                foreach (Player.PlayerInfo pl in Player.AllPlayers)
                {
                    if (ConvertHex(session) == pl.PlayerID)
                    {
                        targetPlayer = pl;
                        break;
                    }
                }
            }
            int promotions = 0;
            if (targetPlayer != null)
            {
                playername = targetPlayer.Name;
                if (Config.GetBoolean("GaW_EnablePromotions", true))
                {
                    ME3MP_Profile profile = ME3MP_Profile.InitializeFromFile(targetPlayer.pathtoprofile);
                    if (profile != null)
                        promotions = profile.GetTotalPromotions();
                }
            }
            string content = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";
            content += "<galaxyatwargetratings>\r\n";
            content += " <ratings>\r\n";
            content += "  <ratings>" + ratings[0] + "</ratings>\r\n";
            content += "  <ratings>" + ratings[1] + "</ratings>\r\n";
            content += "  <ratings>" + ratings[2] + "</ratings>\r\n";
            content += "  <ratings>" + ratings[3] + "</ratings>\r\n";
            content += "  <ratings>" + ratings[4] + "</ratings>\r\n";
            content += " </ratings>\r\n";
            content += " <level>" + ((ratings[0] + ratings[1] + ratings[2] + ratings[3] + ratings[4]) / 5) + "</level>\r\n";
            content += " <assets>\r\n";
            content += "  <assets>" + promotions + "</assets>\r\n"; // MP promotions
            content += "  <assets>0</assets>\r\n"; // ME Infiltrator, Cerberus Escapees
            content += "  <assets>0</assets>\r\n"; // Allied Reinforcements, "General Sherman" (scrapped Facebook integration)
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += "  <assets>0</assets>\r\n";
            content += " </assets>\r\n";
            content += "</galaxyatwargetratings>\r\n";
            string header = CreateHttpHeader(content.Length);
            return header + content;
        }
        private static int[] GaWGetRatings(string filename)
        {
            int[] ratings = { 5000, 5000, 5000, 5000, 5000 };
            filename = loc + "player\\gaw\\" + filename;
            try
            {
                if (!File.Exists(filename)) // if file doesn't exist, create file and return default ratings
                {
                    File.WriteAllLines(filename, new string[] { Blaze.GetUnixTimeStamp().ToString() });
                    File.AppendAllLines(filename, new string[] { ratings[0].ToString(), ratings[1].ToString(), ratings[2].ToString(), ratings[3].ToString(), ratings[4].ToString() });
                    return ratings;
                }
                string[] lines = File.ReadAllLines(filename);
                uint ts = Convert.ToUInt32(lines[0]);
                ratings[0] = Convert.ToInt32(lines[1]);
                ratings[1] = Convert.ToInt32(lines[2]);
                ratings[2] = Convert.ToInt32(lines[3]);
                ratings[3] = Convert.ToInt32(lines[4]);
                ratings[4] = Convert.ToInt32(lines[5]);
                uint timediff = Blaze.GetUnixTimeStamp() - ts;
                float days = (float)timediff / 86400;
                string strDecayRate = Config.FindEntry("GaW_ReadinessDecayRatePerDay");
                strDecayRate = strDecayRate.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                strDecayRate = strDecayRate.Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                float decayRate;
                float.TryParse(strDecayRate, out decayRate);
                float finalDecayValue = decayRate * days * 100;
                for (int i = 0; i < 5; i++)
                {
                    ratings[i] -= (int)finalDecayValue;
                    if (ratings[i] < 5000)
                        ratings[i] = 5000;
                }
                return ratings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("GaWGetRatings | " + GetExceptionMessage(ex));
                return ratings;
            }
        }
        public static void GaWIncreaseRatings(string request, out string playername)
        {
            playername = "«undefined»";
            Uri incRatingsUri = new Uri("gaw://" + request);
            string session = Path.GetFileName(incRatingsUri.LocalPath);
            int[] ratings = GaWGetRatings(session + ".txt");
            if (!File.Exists(loc + "player\\gaw\\" + session + ".txt"))
                return;
            Player.PlayerInfo targetPlayer = null;
            if (session != "default")
            {
                foreach (Player.PlayerInfo pl in Player.AllPlayers)
                {
                    if (ConvertHex(session) == pl.PlayerID)
                    {
                        targetPlayer = pl;
                        break;
                    }
                }
            }
            if (targetPlayer != null)
                playername = targetPlayer.Name;
            string gawfile = loc + "player\\gaw\\" + session + ".txt";
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    string rinc = "rinc|" + i;
                    int increaseValue = Convert.ToInt32(HttpUtility.ParseQueryString(incRatingsUri.Query).Get(rinc));
                    ratings[i] += increaseValue;
                    if (ratings[i] > 10099)
                        ratings[i] = 10099;
                }
                File.WriteAllLines(gawfile, new string[] { Blaze.GetUnixTimeStamp().ToString() });
                File.AppendAllLines(gawfile, new string[] { ratings[0].ToString(), ratings[1].ToString(), ratings[2].ToString(), ratings[3].ToString(), ratings[4].ToString() });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("GaWIncreaseRatings | " + GetExceptionMessage(ex));
            }
        }
        private static string CreateHttpHeader(int contentLenght, int type = 200)
        {
            string header;
            switch (type)
            {
                case 400:
                    header = "HTTP/1.1 400 Bad Request\r\n";
                    break;
                case 404:
                    header = "HTTP/1.1 404 Not Found\r\n";
                    break;
                case 501:
                    header = "HTTP/1.1 501 Not Implemented\r\n";
                    break;
                case 200:
                default:
                    header = "HTTP/1.1 200 OK\r\n";
                    break;
            }
            header += "Content-Length: " + contentLenght + "\r\n";
            header += "Content-Type: text/xml;charset=UTF-8\r\n";
            header += "Date: " + DateTime.UtcNow.ToString("r") + "\r\n";
            header += "Server: Apache-Coyote/1.1\r\n";
            header += "Connection: close\r\n";
            header += "\r\n"; // blank line separates header from content
            return header;
        }
#endregion
    }
}
