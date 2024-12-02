using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RealtimeVoiceClient : MonoBehaviour
{

    private string openAIWebSocketUri = "wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01";
    AudioStreamer audioStreamer;
    async void Start()
    {
        audioStreamer = new();
        await ConnectAsync(openAIWebSocketUri);

        audioStreamer.AudioDataAvailable += async (audioData) =>
        {
            await SendAudioData(audioData);
        };

        audioStreamer.StartCapture();
    }

    async void OnDestroy()
    {
        audioStreamer.StopCapture();
        await DisconnectAsync();
    }

    private ClientWebSocket webSocket;

    public async Task ConnectAsync(string uri)
    {
        webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {Secret.API_KEY}");
        webSocket.Options.SetRequestHeader("OpenAI-Beta", $"realtime=v1");

        await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);

        print("Connected to OpenAI WebSocket.");

        _ = ReceiveResponses();
    }

    public async Task SendAudioData(byte[] buffer)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    private async Task ReceiveResponses()
    {
        var buffer = new byte[1024];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            else
            {
                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Received response: " + response);
            }
        }
    }

    public async Task DisconnectAsync()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
    }

}
