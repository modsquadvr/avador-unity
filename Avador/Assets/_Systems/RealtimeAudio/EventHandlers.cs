using System;
using System.Collections.Generic;
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
            { "response.content_part.added", HandleResponseContentPartAdded },
            { "input_audio_buffer.speech_started", HandleInputAudioBufferSpeechStarted},
            { "input_audio_buffer.speech_stopped", HandleInputAudioBufferSpeechStopped},
            { "response.created", HandleResponseCreated},
            { "response.done", HandleResponseDone},
            { "conversation.item.input_audio_transcription.completed", HandleInputTranscription},
            { "response.audio.delta", HandleResponseAudioDelta},
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
        _enableAudioSend = true;

        AudioProcessor.Instance.OnInputAudioProcessed += HandleInputAudioProcessed;
        Debug.Log($"Session updated: Now Sending microphone data. Session Details: \n {jsonEvent}");
    }

    /// <summary>
    /// when audio is sent back, process it.
    /// </summary>
    /// <param name="jsonEvent"></param>

    private void HandleResponseContentPartAdded(string jsonEvent)
    {

    }

    private void HandleResponseAudioDelta(string jsonEvent)
    {
        try
        {
            var eventObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonEvent);
            if (eventObject != null && eventObject.ContainsKey("delta"))
            {
                // Extract the base64-encoded audio delta
                string base64AudioDelta = eventObject["delta"]?.ToString();
                if (!string.IsNullOrEmpty(base64AudioDelta))
                {
                    // Decode the Base64 string into a byte array
                    byte[] decodedData = DecodeAudioData(base64AudioDelta);

                    // Process the decoded audio data
                    AudioProcessor.Instance.ProcessAudioOut(decodedData);
                    Debug.Log("Processed audio delta successfully.");
                }
                else
                {
                    Debug.LogWarning("Delta property is empty or null.");
                }
            }
            else
            {
                Debug.LogWarning($"Response is missing 'delta' property: {jsonEvent}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing audio delta: {e.Message}");
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

    private void HandleResponseCreated(string jsonEvent)
    {
        Debug.Log("<color=red>Response created.</color>");
        Debug.Log(jsonEvent.ToString());
        //IF WE RECEIVE AN AUDIO RESPONSE, STOP SENDING AUDIO - TEMP FIX
        _enableAudioSend = false;
    }
    private void HandleResponseDone(string jsonEvent)
    {
        Debug.Log("<color=red>Response done.</color>");
        Debug.Log(jsonEvent.ToString());
        //WHEN THE AUDIO RESPONSE IS DONE, START SENDING AUDIO AGAIN - TEMP FIX
        _enableAudioSend = true;

        //response -> output -> content -> text
        // try
        // {
        var jsonObject = JObject.Parse(jsonEvent);

        // Navigate to response -> output
        var outputArray = jsonObject["response"]?["output"] as JArray;
        if (outputArray == null)
            throw new Exception("Output array not found in response.");

        // Loop through the array and extract the "text" field from "content"
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
                        print($"<color=#44FFD2>Response: {transcript}</color>");
                        // OnResponseDone.Invoke(transcript);
                        MainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            OnResponseDone?.Invoke(transcript);
                        });
                    }
                }
            }
        }
        // }
        // catch (Exception e)
        // {
        //     Debug.LogError($"Error handling response done event: {e}");
        // }
    }
    private void HandleInputTranscription(string jsonEvent)
    {
        Debug.Log("<color=red>Input transcription.</color>");
        Debug.Log(jsonEvent.ToString());
    }
}