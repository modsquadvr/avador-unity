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
            { "response.content_part.added", HandleResponseContentPartAdded },
            { "input_audio_buffer.speech_started", HandleInputAudioBufferSpeechStarted},
            { "input_audio_buffer.speech_stopped", HandleInputAudioBufferSpeechStopped},
        };
    }

    // EVENT HANDLERS

    private void HandleErrorEvent(string jsonEvent)
        => Debug.LogError($"Event Error: {jsonEvent}");

    private void HandleSessionCreatedEvent(string jsonEvent)
        => print("<color=#8FD694>Realtime API Session Created</color>");

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
                    //Log the audio response transcription
                    if (partObject["text"] != null)
                        Debug.Log($"<color=#61E8E1>GPT: {partObject["text"]}</color>");


                    string base64Audio = partObject["audio"].ToString();

                    byte[] decodedData = DecodeAudioData(base64Audio);
                    AudioProcessor.Instance.ProcessAudioOut(decodedData);

                }
                else
                {
                    Debug.LogWarning("'part' object is missing 'audio' property in event.");
                    Debug.Log(jsonEvent.ToString());
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

    private void HandleInputAudioBufferSpeechStarted(string jsonEvent)
    {
        Debug.Log("<color=#5B5B5B>Server VAD: User speech started.</color>");
    }

    private void HandleInputAudioBufferSpeechStopped(string jsonEvent)
    {
        Debug.Log("<color=#5B5B5B>Server VAD: User speech stopped.</color>");
    }


}