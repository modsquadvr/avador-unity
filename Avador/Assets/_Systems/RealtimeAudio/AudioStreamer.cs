using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class AudioStreamer : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip audioClip;

    private ConcurrentQueue<(bool, float)> audioQueue = new ConcurrentQueue<(bool, float)>();

    private bool _isIncrementingAudioEndMs;

    private void Awake()
    {
        AudioStreamMediator.Instance.OnResponseCreated += StartResponse;
        AudioStreamMediator.Instance.OnAudioInterrupted += InterruptAudio;
        AudioProcessor.Instance.OnOutputAudioProcessed += AddAudioData;
    }

    private void OnDestroy()
    {
        AudioStreamMediator.Instance.OnResponseCreated -= StartResponse;
        AudioStreamMediator.Instance.OnAudioInterrupted -= InterruptAudio;
        AudioProcessor.Instance.OnOutputAudioProcessed -= AddAudioData;
    }

    private void Start()
    {
        audioClip = AudioClip.Create("RealtimeAudio", 24000 * 10, 1, 24000, true, OnAudioRead);
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void OnAudioRead(float[] data)
    {
        // Fill the audio buffer
        for (int i = 0; i < data.Length; i++)
            data[i] = GetNextSample();
    }

    private void AddAudioData((bool isResponseDone, float[] newAudioData) data)
    {
        for (int i = 0; i < data.newAudioData.Length; i++)
            audioQueue.Enqueue((data.isResponseDone, data.newAudioData[i]));
    }

    private void StartResponse()
    {
        if (!_isIncrementingAudioEndMs)
        {
            if (AudioStreamMediator.Instance.audio_end_ms != 0)
                Debug.LogError("Starting a new response but the audio_end_ms is not reset");

            _ = Task.Run(IncrementAudioEnd);
        }
    }

    private void InterruptAudio()
    {
        if (_isIncrementingAudioEndMs)
        {
            EndAudioSection();
            audioQueue.Clear();
        }
    }

    //HELPERS
    private float GetNextSample()
    {
        if (audioQueue.TryDequeue(out (bool isResponseDone, float chunk) sample))
        {
            AudioStreamMediator.Instance.isAudioPlaying = true;
            if (sample.isResponseDone)
                EndAudioSection();

            return sample.chunk;
        }

        return 0.0f;
    }

    /// <summary>
    /// Every 50ms, tell the audio stream mediator that another 50 ms of audio has been played.
    /// This is in case we want to interrupt the audio, we will need to tell OpenAI how much audio we already played.
    /// 50 ms increments are chosen arbitrarily so as not to update it very frequently, when millisecond accuracy is not needed.
    /// </summary>
    private async void IncrementAudioEnd()
    {
        if (_isIncrementingAudioEndMs) return;
        _isIncrementingAudioEndMs = true;

        try
        {
            while (_isIncrementingAudioEndMs)
            {
                AudioStreamMediator.Instance.audio_end_ms += 50;
                await Task.Delay(50);
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in IncrementAudioEnd: {ex.Message}");
        }

    }

    private void EndAudioSection()
    {
        _isIncrementingAudioEndMs = false;
        AudioStreamMediator.Instance.audio_end_ms = 0;
        AudioStreamMediator.Instance.isAudioPlaying = false;
    }

}