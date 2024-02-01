using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using System;
class Program
{
    public static string m_configFileRelativePath = "ConfigRedirection.json";
    public static UdpClient udpClient;

    public class AppConfig {

        public int m_portOfServer=7072;
        public string m_redirectionIp="127.0.0.1";
        public int m_portOfRedirection=7073;

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
        udpClient = new UdpClient();

        // Start the WebSocket server
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://localhost:{AppConfig.Configuration.m_portOfServer}/");
        httpListener.Start();
        Console.WriteLine($"WebSocket server is running on http://localhost:{AppConfig.Configuration.m_portOfServer}/");


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
            udpClient.Send(data, data.Length, endPoint);
            Console.WriteLine($"Sent message via UDP: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending UDP message: {ex.Message}");
        }
    }
}
