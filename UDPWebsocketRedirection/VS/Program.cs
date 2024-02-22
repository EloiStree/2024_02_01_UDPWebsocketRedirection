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

        public int m_portOfServerWebsocket = 7072;
        public int m_portOfServerUDP = 7073;
        public string m_redirectionIp="127.0.0.1";
        public int m_portOfRedirection=7074;

        public static AppConfig Configuration= new AppConfig();
        internal bool m_displayIpAddresses=true;
        public bool m_redirectBytesInsteadOfUTF8 = false;
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
        if(AppConfig.Configuration.m_redirectBytesInsteadOfUTF8)
            udpListener.LaunchThread(AppConfig.Configuration.m_portOfServerUDP, (string text) => { SendUDPMessage(text); }, (out bool contine) => { contine = true; /*Bad code*/});
        else udpListener.LaunchThread(AppConfig.Configuration.m_portOfServerUDP, (byte[] text) => { SendUDPMessage(text); }, (out bool contine) => { contine = true; /*Bad code*/});

        // Start the WebSocket server
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://*:{AppConfig.Configuration.m_portOfServerWebsocket}/");
        httpListener.Start();
        Console.WriteLine($"WebSocket server is running on http://{NetworkInfo.GetRouterPublicIpAddress()}:{AppConfig.Configuration.m_portOfServerWebsocket}/");
        Console.WriteLine($"UDP server is running on http://{NetworkInfo.GetRouterPublicIpAddress()}:{AppConfig.Configuration.m_portOfServerUDP}/");


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
        byte[] buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {message}");

                    // Send the received message via UDP
                    SendUDPMessage(message);
                }
                
                else if (result.MessageType == WebSocketMessageType.Binary)
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

    static void SendUDPMessage(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.Configuration.m_redirectionIp), AppConfig.Configuration.m_portOfRedirection);
            udpClientRedirection.Send(data, data.Length, endPoint);
            Console.WriteLine($"Sent message via UDP: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending UDP message: {ex.Message}");
        }
    }
    static void SendUDPMessage(byte[] byteMessage)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(AppConfig.Configuration.m_redirectionIp), AppConfig.Configuration.m_portOfRedirection);
            udpClientRedirection.Send(byteMessage, byteMessage.Length, endPoint);
            Console.WriteLine($"Sent message via bytes: {byteMessage.Length}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending UDP byte: {ex.Message}");
        }
    }
}
