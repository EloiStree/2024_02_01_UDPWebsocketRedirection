using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class Program
{
    public static string m_configFileRelativePath = "ConfigRedirection.json";
    public static UdpClient udpClientRedirection;

    public class AppConfig {

        public int m_portOfServerWebsocket = 7065;
        public int m_portOfServerByteUDP = 7066;
        public int m_portOfByteRedirection = 12346;
        public string m_redirectionIp="127.0.0.1";

        public static AppConfig Configuration= new AppConfig();
        internal bool m_displayIpAddresses=true;
    }

    static async Task Main(string[] args)
    {
        if (!File.Exists(m_configFileRelativePath))
            File.WriteAllText(m_configFileRelativePath, JsonConvert.SerializeObject(AppConfig.Configuration));

        string configUsed = File.ReadAllText(m_configFileRelativePath);
        Console.WriteLine(configUsed);
        AppConfig.Configuration = JsonConvert.DeserializeObject<AppConfig>(configUsed);


        if(AppConfig.Configuration.m_displayIpAddresses)
            NetworkInfo.DisplayConnectedLocalIPs();
        // Start the UDP client
        udpClientRedirection = new UdpClient();


        UdpListener udpListener = new UdpListener();
        udpListener.LaunchThread(AppConfig.Configuration.m_portOfServerByteUDP, (byte[] text) => { SendUDPMessage(text); }, (out bool contine) => { contine = true; /*Bad code*/});

        // Start the WebSocket server
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://*:{AppConfig.Configuration.m_portOfServerWebsocket}/");
        httpListener.Start();
        Console.WriteLine($"WebSocket server is running on http://{NetworkInfo.GetRouterPublicIpAddress()}:{AppConfig.Configuration.m_portOfServerWebsocket}/");
        Console.WriteLine($"UDP server is running on http://{NetworkInfo.GetRouterPublicIpAddress()}:{AppConfig.Configuration.m_portOfServerByteUDP}/");


        HideWindow.MinimizeConsoleWindow();
        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                ProcessWebSocketRequest(context);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
        WebSocket webSocket = webSocketContext.WebSocket;

        Console.WriteLine("WebSocket connection established.");

        await HandleWebSocketMessages(webSocket);
    }

    static async Task HandleWebSocketMessages(WebSocket webSocket)
    {
        // intIndex intCommand long byteToken
        byte[] buffer = new byte[32];

        while (webSocket.State == WebSocketState.Open)
        {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                 if (result.MessageType == WebSocketMessageType.Binary)
                {
                    Console.WriteLine($"Received messag bytes: {buffer.Length}");
                    SendUDPMessage(buffer);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("WebSocket connection closed.");
                    break;
                }
        }
    }

    static void SendUDPMessage(byte[] byteMessage)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.Configuration.m_redirectionIp), AppConfig.Configuration.m_portOfByteRedirection);
            udpClientRedirection.Send(byteMessage, byteMessage.Length, endPoint);

            Console.WriteLine($"Sent message via bytes: {byteMessage.Length} | ");

            Console.Write(string.Join(",", byteMessage));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending UDP byte: {ex.Message}");
        }
    }
}
