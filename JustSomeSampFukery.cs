using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SampChatListener
{
    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;
    private CancellationTokenSource cancelTokenSource;

    public event EventHandler<ChatMessageEventArgs> ChatMessageReceived;

    public SampChatListener(string serverIp, int serverPort)
    {
        try
        {
            client = new TcpClient();
            cancelTokenSource = new CancellationTokenSource();

            ConnectAsync(serverIp, serverPort).Wait(); // Synchronous call, for simplicity

            // Subscribe to chat events
            Send("SUBSCRIBE_CHAT");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public async Task ConnectAsync(string serverIp, int serverPort)
    {
        await client.ConnectAsync(serverIp, serverPort);
        stream = client.GetStream();
        reader = new StreamReader(stream, Encoding.UTF8);
        writer = new StreamWriter(stream, Encoding.UTF8);
    }

    public async Task ListenForChatMessagesAsync()
    {
        try
        {
            while (!cancelTokenSource.Token.IsCancellationRequested)
            {
                string chatMessage = await reader.ReadLineAsync();

                if (!string.IsNullOrEmpty(chatMessage))
                {
                    OnChatMessageReceived(chatMessage);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The cancellation token was triggered; exit gracefully
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listening for chat messages: {ex.Message}");
        }
    }

    public void Send(string message)
    {
        try
        {
            writer.WriteLine(message);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        try
        {
            cancelTokenSource.Cancel(); // Stop listening for chat messages
            reader.Close();
            writer.Close();
            stream.Close();
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disconnecting: {ex.Message}");
        }
    }

    protected virtual void OnChatMessageReceived(string message)
    {
        ChatMessageReceived?.Invoke(this, new ChatMessageEventArgs(message));
    }
}

public class ChatMessageEventArgs : EventArgs
{
    public string Message { get; }

    public ChatMessageEventArgs(string message)
    {
        Message = message;
    }
}