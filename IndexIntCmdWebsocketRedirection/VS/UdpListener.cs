using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpListener
{
    private  UdpClient m_udpClient;
    private  Thread m_listenerThread;


    public void LaunchThread(int port, Action<string> receivedRedirection, ShouldStayAlive checkAliveState)
    {

        m_listenerThread = new Thread(() => StartListener(port, receivedRedirection, checkAliveState));
        m_listenerThread.Start();
    }
    public delegate void ShouldStayAlive(out bool shouldStayAlive);
    void StartListener(int port, Action<string> receivedRedirection, ShouldStayAlive checkAliveState)
    {
        m_udpClient = new UdpClient(port);

        try
        {
            bool shouldStayAlive = true;
            while (shouldStayAlive)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = m_udpClient.Receive(ref remoteEP);
                string receivedMessage = Encoding.UTF8.GetString(data);
                Console.WriteLine($"Received from {remoteEP}: {receivedMessage}");
                receivedRedirection.Invoke(receivedMessage);
                checkAliveState(out shouldStayAlive);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
        finally
        {
            m_udpClient.Close();
        }
    }


    public void LaunchThread(int port, Action<byte[]> receivedRedirection, ShouldStayAlive checkAliveState)
    {

        m_listenerThread = new Thread(() => StartListener(port, receivedRedirection, checkAliveState));
        m_listenerThread.Start();
    }
    void StartListener(int port, Action<byte[]> receivedRedirection, ShouldStayAlive checkAliveState)
    {
        m_udpClient = new UdpClient(port);

        try
        {
            bool shouldStayAlive = true;
            while (shouldStayAlive)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = m_udpClient.Receive(ref remoteEP);
                Console.WriteLine($"Received from {remoteEP} bytes: {data.Length}");
                receivedRedirection.Invoke(data);
                checkAliveState(out shouldStayAlive);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
        finally
        {
            m_udpClient.Close();
        }
    }

    public void StopListener()
    {
        if (m_udpClient != null)
        {
            m_udpClient.Close();
        }
    }
}

