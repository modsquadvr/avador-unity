using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class RealtimeClient : MonoBehaviour
{
    private ClientWebSocket _webSocket;
    private Uri _uri = new Uri("wss://api.openai.com/v1/realtime");
    private string _apiKey => Secret.API_KEY;

    public void Awake()
    => AudioProcessor.Instance.OnInputAudioProcessed += HandleInputAudioProcessed;

    public void OnDestroy()
    => AudioProcessor.Instance.OnInputAudioProcessed -= HandleInputAudioProcessed;

    public async void Start() => await Task.Run(ConnectAsync);

    public async Task ConnectAsync()
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
        await _webSocket.ConnectAsync(_uri, CancellationToken.None);
        print("<color=#8FD694>Connected to OpenAI Realtime API</color>");
    }

    private async Task SendAudioDataAsync(byte[] audioData)
    {
        string encodedData = EncodeAudioData(audioData);
        byte[] buffer = Encoding.UTF8.GetBytes(encodedData);
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 4];
        while (_webSocket.State == WebSocketState.Open)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string receivedData = Encoding.UTF8.GetString(buffer, 0, result.Count);
                byte[] decodedData = DecodeAudioData(receivedData);
                AudioProcessor.Instance.ProcessAudioOut(decodedData);
            }
        }
    }

    // HELPERS
    private string EncodeAudioData(byte[] audioData)
    {
        return Convert.ToBase64String(audioData);
    }

    private byte[] DecodeAudioData(string receivedData)
    {
        byte[] audioData = Convert.FromBase64String(receivedData);
        return audioData;
    }

    private async void HandleInputAudioProcessed(byte[] audioData)
        => await SendAudioDataAsync(audioData);

}