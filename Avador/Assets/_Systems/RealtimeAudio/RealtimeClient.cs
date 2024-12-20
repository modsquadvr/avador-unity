using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public partial class RealtimeClient : MonoBehaviour
{
    private ClientWebSocket _webSocket;
    private Uri _uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01");
    private string _apiKey => Secret.API_KEY;

    private Dictionary<string, Action<string>> eventHandlers;

    private bool isConversationInitialized;

    public void Awake()
    => InitializeEventHandlers();

    public void OnDestroy()
    => AudioProcessor.Instance.OnInputAudioProcessed -= HandleInputAudioProcessed;

    public async void Start() => await Task.Run(ConnectAsync);

    public async Task ConnectAsync()
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
        _webSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

        //Authorization handshake
        await _webSocket.ConnectAsync(_uri, CancellationToken.None);

        //Start message receive loop
        _ = Task.Run(ReceiveMessagesAsync);

        //Configure session parameters
        await InitiateConversation();
    }

    private async Task InitiateConversation()
    {
        try
        {
            var configureEvent = new
            {
                type = "session.update",
                session = new
                {
                    modalities = new[] { "text", "audio" },
                    GPTConfig.instructions,
                    GPTConfig.voice,
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    input_audio_transcription = new
                    {
                        model = "whisper-1"
                    },
                    turn_detection = new
                    {
                        type = "server_vad",
                        threshold = 0.5,
                        prefix_padding_ms = 300,
                        silence_duration_ms = 500
                    },
                    tools = new string[] { },
                    tool_choice = "none",
                    temperature = 0.8,
                    max_response_output_tokens = "inf"
                }
            };

            string jsonString = JsonConvert.SerializeObject(configureEvent, Formatting.Indented);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending audio data: {e}");
        }
    }

    private async Task SendAudioDataAsync(byte[] audioData)
    {
        try
        {
            var appendEvent = new
            {
                type = "input_audio_buffer.append",
                audio = EncodeAudioData(audioData)
            };

            string jsonString = JsonConvert.SerializeObject(appendEvent);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending audio data: {e}");
        }

    }

    public async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 16];
        var messageBuilder = new StringBuilder(); //handles message defragmentation

        while (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        string completeMessage = messageBuilder.ToString();
                        messageBuilder.Clear();
                        HandleServerEvent(completeMessage);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.LogWarning("Websocket connection closed by server.");
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving websocket message: {e}");
            }
        }
    }

    // HELPERS
    private void HandleServerEvent(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject != null && eventObject.ContainsKey("type"))
            {
                string eventType = eventObject["type"].ToString();

                if (eventHandlers.TryGetValue(eventType, out Action<string> handler))
                    handler.Invoke(jsonEvent);
                // else Debug.LogWarning($"No Handler found for event type: {eventType}");
            }
            else Debug.LogWarning("Invalid event format. Missing field 'type'");

        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling server event {e}");
        }
    }

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