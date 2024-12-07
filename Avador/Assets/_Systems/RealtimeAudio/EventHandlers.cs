using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UPP.Utils;

public partial class RealtimeClient
{
    private void InitializeEventHandlers()
    {
        eventHandlers = new Dictionary<string, Action<string>>
        {
            { "error", HandleErrorEvent },
            { "session.created", HandleSessionCreatedEvent },
            { "session.updated", HandleSessionUpdatedEvent },
            { "response.created", HandleResponseCreated },
            { "response.audio.delta", HandleResponseAudioDelta },
            { "response.audio_transcript.delta", HandleResponseAudioTranscriptDelta },
            { "response.done", HandleResponseDone },
            { "conversation.item.input_audio_transcription.completed", HandleInputTranscription },
            { "response.function_call_arguments.done", HandleFunctionCallArgumentsDone },
            { "input_audio_buffer.speech_started", HandleInputAudioBufferSpeechStarted },
            { "input_audio_buffer.speech_stopped", HandleInputAudioBufferSpeechStopped },

        };
    }

    //events exposed outside of client
    public Action OnResponseCreated;
    public Action<string> OnResponseAudioTranscriptDelta;
    public Action<int> OnItemSelected;
    public Action OnSpeechStarted;
    public Action OnReturnToBubbles;

    // EVENT HANDLERS
    private void HandleErrorEvent(string jsonEvent)
        => Debug.LogError($"Event Error: {jsonEvent}");

    private void HandleSessionCreatedEvent(string jsonEvent)
        => print("Realtime API Session Created.");

    /// <summary>
    /// only subscribe to sending audio after the conversation is configured.
    /// </summary>
    /// <param name="jsonEvent"></param>
    private void HandleSessionUpdatedEvent(string jsonEvent)
    {
        if (isConversationInitialized)
            return;
        isConversationInitialized = true;

        AudioProcessor.Instance.OnInputAudioProcessed += HandleInputAudioProcessed;
    }


    private void HandleResponseCreated(string jsonEvent)
    {
        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            OnResponseCreated?.Invoke();
            AudioStreamMediator.TriggerResponseCreated();
        });
    }

    /// <summary>
    /// when audio is sent back from the realtime API, process it.
    /// </summary>
    /// <param name="jsonEvent"></param>
    private void HandleResponseAudioDelta(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject != null && eventObject.ContainsKey("delta"))
            {
                //audio chunk
                string base64AudioDelta = eventObject["delta"]?.ToString();
                if (!string.IsNullOrEmpty(base64AudioDelta))
                {
                    byte[] decodedData = DecodeAudioData(base64AudioDelta);
                    AudioProcessor.Instance.ProcessAudioOut(decodedData);
                }
                else Debug.LogWarning("Delta property is empty or null.");

                //item ID of the response -- used in case of interruption
                string itemID = eventObject["item_id"]?.ToString();
                if (!string.IsNullOrEmpty(itemID))
                {
                    activeResponseID = itemID;
                }
                else Debug.LogWarning("item_id property is empty or null.");

            }
            else Debug.LogWarning($"Response is missing 'delta' property: {jsonEvent}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing audio delta: {e.Message}");
        }
    }

    private void HandleResponseAudioTranscriptDelta(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject != null && eventObject.ContainsKey("delta"))
            {
                string transcriptDelta = eventObject["delta"]?.ToString();
                if (!string.IsNullOrEmpty(transcriptDelta))
                {
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        OnResponseAudioTranscriptDelta?.Invoke(transcriptDelta);
                    });
                }
                else Debug.LogWarning("Delta property is empty or null.");
            }
            else Debug.LogWarning($"Response is missing 'delta' property: {jsonEvent}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing transcript delta: {e.Message}");
        }

    }

    private void HandleResponseDone(string jsonEvent)
    {
        //response -> output -> content -> text
        try
        {
            var jsonObject = JObject.Parse(jsonEvent);

            var outputArray = jsonObject["response"]?["output"] as JArray;
            if (outputArray == null)
                throw new Exception("Output array not found in response.");

            foreach (var outputItem in outputArray)
            {
                var contentArray = outputItem["content"] as JArray;
                if (contentArray != null)
                {
                    foreach (var contentItem in contentArray)
                    {
                        if (contentItem["type"]?.ToString() == "audio")
                        {
                            string transcript = contentItem["transcript"]?.ToString();
                            print($"<color=#44FFD2>GPT: {transcript}</color>");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling response done event: {e}");
        }
    }

    private void HandleInputTranscription(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject != null && eventObject.ContainsKey("transcript"))
            {
                string transcript = eventObject["transcript"]?.ToString();
                if (!string.IsNullOrEmpty(transcript))
                    print($"<color=#87F6FF>User: {transcript}</color>");

                else Debug.LogWarning("Input transcript property is empty or null.");
            }
            else Debug.LogWarning($"Response is missing 'transcript' property: {jsonEvent}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing audio delta: {e.Message}");
        }

    }

    private void HandleFunctionCallArgumentsDone(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject == null)
            {
                Debug.LogWarning("Event object is null or invalid.");
                return;
            }

            string functionName = eventObject["name"]?.ToString();
            string argumentsJson = eventObject["arguments"]?.ToString();

            if (string.IsNullOrEmpty(functionName))
            {
                Debug.LogWarning("Function name is missing in the event.");
                return;
            }

            switch (functionName)
            {
                case "identify_item":
                    HandleIdentifyItem(argumentsJson);
                    break;
                case "return_to_suggestion_bubbles":
                    HandleReturnToSuggestionBubbles();
                    break;

                default:
                    Debug.LogWarning($"Unhandled function name: {functionName}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling function call arguments done: {e}");
        }
    }


    //HELPERS
    private void HandleIdentifyItem(string argumentsJson)
    {
        try
        {
            var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(argumentsJson);
            if (arguments != null && arguments.ContainsKey("item_id"))
            {
                int itemId = Convert.ToInt32(arguments["item_id"]);
                Debug.Log($"<color=#D8D174>Identifying item with ID: {itemId}</color>");

                _ = Task.Run(RequestResponse);

                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnItemSelected?.Invoke(itemId);
                });
            }
            else Debug.LogWarning("Missing 'item_id' parameter in identify_item arguments.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling identify_item function: {e}");
        }
    }
    
    private void HandleReturnToSuggestionBubbles()
    {
        try
        {
            _ = Task.Run(RequestResponse);
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                OnReturnToBubbles?.Invoke();
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling return_to_suggestion_bubbles function: {e}");
        }
    }

    private void HandleInputAudioBufferSpeechStarted(string jsonEvent)
    {

        if (AudioStreamMediator.isAudioPlaying)
        {
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                AudioStreamMediator.TriggerAudioInterrupted();
            });

            _ = Task.Run(SendConversationItemTruncate);
        }

    }

    private void HandleInputAudioBufferSpeechStopped(string jsonEvent) { }

}