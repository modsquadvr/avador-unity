using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public partial class RealtimeClient
{
    private void InitializeEventHandlers()
    {
        eventHandlers = new Dictionary<string, Action<string>>
        {
            { "error", HandleErrorEvent },
            { "session.created", HandleSessionCreatedEvent },
            { "session.updated", HandleSessionUpdatedEvent },
            { "response.content_part.added", HandleResponseContentPartAdded }
        };
    }

    // EVENT HANDLERS

    private void HandleErrorEvent(string jsonEvent) { }

    private void HandleSessionCreatedEvent(string jsonEvent) { Debug.Log("Session created!"); }

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
        Debug.Log("Session updated. Sending microphone data.");
    }

    /// <summary>
    /// when audio is sent back, process it.
    /// </summary>
    /// <param name="jsonEvent"></param>

    private void HandleResponseContentPartAdded(string jsonEvent)
    {
        try
        {
            // Step 1: Parse the JSON object
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);

            if (eventObject != null && eventObject.ContainsKey("part"))
            {
                var partObject = eventObject["part"] as JObject;

                if (partObject != null && partObject["audio"] != null)
                {
                    string base64Audio = partObject["audio"].ToString();

                    byte[] decodedData = DecodeAudioData(base64Audio);
                    AudioProcessor.Instance.ProcessAudioOut(decodedData);

                }
                else
                {
                    Debug.LogWarning("'part' object is missing 'audio' property in event.");
                }
            }
            else
            {
                Debug.LogWarning("'response.content_part.added' event is missing 'part' property.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling 'response.content_part.added' event: {ex.Message}");
        }
    }

}