using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.PlayerLoop;

public partial class RealtimeClient : MonoBehaviour
{
    [SerializeField] private GPTConfig config;
    [SerializeField] private ContentProvider _contentProvider;
    private ClientWebSocket _webSocket;
    private Uri _uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01");
    private string _apiKey => Secret.API_KEY;
    private string _initialConfigInstruction;

    private Dictionary<string, Action<string>> eventHandlers;

    private bool isConversationInitialized;

    private string activeResponseID;

    //singleton
    public static RealtimeClient Instance;

    public void Awake()
    {
        if (Instance is not null)
            Debug.LogError("There can only be one RealtimeClient");
        Instance = this;
        InitializeEventHandlers();
    }

    public async void OnDestroy()
    {
        config.instructions = _initialConfigInstruction;
        
        try
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while closing WebSocket: {e}");
        }
        finally
        {
            AudioProcessor.Instance.OnInputAudioProcessed -= HandleInputAudioProcessed;
            Instance = null;
        }
    }

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
        
        //Add included objects from the ContentProvider to the config instructions
        if (_contentProvider != null)
            AddMuseumObjectsToConfig();

        //Configure session parameters
        await InitiateConversation();

        //Send an inital "Hello", so it appears that the GPT is starting the conversation
        await SendConversationItem("Hello");
        await RequestResponse();
    }

    private void AddMuseumObjectsToConfig()
    {
        // Initialize a StringBuilder with the existing instructions.
        StringBuilder builder = new StringBuilder(config.instructions);
    
        // Append a header and the museum objects.
        builder.AppendLine();
        builder.AppendLine("Objects with format ID: Name: (\nDescription\n)");
    
        foreach (MuseumObjectSO museumObject in _contentProvider.MuseumObjectSOs)
        {
            builder.AppendLine($"{museumObject.Id}: {museumObject.ObjectName}:(\n{museumObject.Description}\n)");
        }
    
        // Update the config instructions with the built string.
        _initialConfigInstruction = config.instructions;
        config.instructions = builder.ToString();
    
        Debug.Log($"New config instructions: \n{config.instructions}");
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
                    config.instructions,
                    voice = config.voice.ToString(),
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
                    tools = new[] {
                        new
                        {
                            type = "function",
                            name = "identify_item",
                            description = "Identify the current item number being discussed.",
                            parameters = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>
                                {
                                    {"item_id", new { type = "integer" }}
                                },
                                required = new[] { "item_id" }
                            }
                        },
                        new 
                        {
                            type = "function",
                            name = "return_to_suggestion_bubbles",
                            description = "Identify when no item is currently being discussed and a new one must be found.",
                            parameters = new
                            {
                                type = "object",
                                properties = new Dictionary<string, object>(),
                                required = Array.Empty<string>()
                            }
                        }
                    },
                    tool_choice = "auto",
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

    private async Task SendConversationItem(string messageText)
    {
        try
        {
            var conversationItem = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                    new
                    {
                        type = "input_text",
                        text = messageText
                    }
                }
                }
            };

            string jsonString = JsonConvert.SerializeObject(conversationItem, Formatting.Indented);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending conversation.item.create message: {e}");
        }
    }

    private async Task RequestResponse()
    {
        try
        {
            var request = new { type = "response.create" };

            string jsonString = JsonConvert.SerializeObject(request, Formatting.Indented);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error requesting response: {e}");
        }

    }

    private async Task SendConversationItemTruncate()
    {
        try
        {
            var truncateItem = new
            {
                type = "conversation.item.truncate",
                item_id = activeResponseID,
                content_index = 0,
                AudioStreamMediator.Instance.audio_end_ms
            };

            string jsonString = JsonConvert.SerializeObject(truncateItem, Formatting.Indented);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            await _webSocket.SendAsync(new ArraySegment<byte>(jsonBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending conversation.item.truncate: {e}");
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
                    Debug.Log("Websocket connection closed.");
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving websocket message: {e}");
            }
        }
    }

    // HELPERS - - - -
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
    => Convert.ToBase64String(audioData);

    private byte[] DecodeAudioData(string receivedData)
    {
        byte[] audioData = Convert.FromBase64String(receivedData);
        return audioData;
    }

    private async void HandleInputAudioProcessed(byte[] audioData)
    {
        await SendAudioDataAsync(audioData);
    }
}