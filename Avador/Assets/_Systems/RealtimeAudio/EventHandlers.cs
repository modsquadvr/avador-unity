using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
            { "input_audio_buffer.speech_started", HandleInputAudioBufferSpeechStarted },
            { "input_audio_buffer.speech_stopped", HandleInputAudioBufferSpeechStopped },
            { "response.created", HandleResponseCreated },
            { "response.done", HandleResponseDone },
            { "conversation.item.input_audio_transcription.completed", HandleInputTranscription },
            { "response.audio.delta", HandleResponseAudioDelta },
            { "response.audio_transcript.delta", HandleResponseAudioTranscriptDelta }
        };
    }

    //events exposed outside of client
    public Action OnResponseCreated;
    public Action<string> OnResponseAudioTranscriptDelta;


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
        _enableAudioSend = true;

        AudioProcessor.Instance.OnInputAudioProcessed += HandleInputAudioProcessed;
    }


    private void HandleResponseCreated(string jsonEvent)
    {
        //IF WE RECEIVE AN AUDIO RESPONSE, STOP SENDING AUDIO - TEMP FIX
        _enableAudioSend = false;

        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            OnResponseCreated?.Invoke();
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
                string base64AudioDelta = eventObject["delta"]?.ToString();
                if (!string.IsNullOrEmpty(base64AudioDelta))
                {
                    byte[] decodedData = DecodeAudioData(base64AudioDelta);
                    AudioProcessor.Instance.ProcessAudioOut(decodedData);
                }
                else Debug.LogWarning("Delta property is empty or null.");
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
        //WHEN THE AUDIO RESPONSE IS DONE, START SENDING AUDIO AGAIN - TEMP FIX
        _enableAudioSend = true;
    }

    private void HandleInputAudioBufferSpeechStarted(string jsonEvent)
    {
        print("<color=#5B5B5B>Server VAD: User speech started.</color>");
    }

    private void HandleInputAudioBufferSpeechStopped(string jsonEvent)
    {
        print("<color=#5B5B5B>Server VAD: User speech stopped.</color>");
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

                else Debug.LogWarning("Transcript property is empty or null.");
            }
            else Debug.LogWarning($"Response is missing 'transcript' property: {jsonEvent}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing audio delta: {e.Message}");
        }

    }
}