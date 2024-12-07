using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class AudioStreamer : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip audioClip;

    private ConcurrentQueue<float> audioQueue = new ConcurrentQueue<float>();

    private bool _isIncrementingAudioEndMs;

    void Awake()
    {
        AudioProcessor.Instance.OnOutputAudioProcessed += AddAudioData;
        AudioStreamMediator.OnAudioInterrupted += InterruptAudio;
        RealtimeClient.Instance.OnResponseCreated += StartResponse;
    }

    void OnDestroy()
    {
        AudioProcessor.Instance.OnOutputAudioProcessed -= AddAudioData;
        AudioStreamMediator.OnAudioInterrupted -= InterruptAudio;
        RealtimeClient.Instance.OnResponseCreated -= StartResponse;
    }

    void Start()
    {
        audioClip = AudioClip.Create("RealtimeAudio", 24000 * 10, 1, 24000, true, OnAudioRead);
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        // Fill the audio buffer
        for (int i = 0; i < data.Length; i++)
            data[i] = GetNextSample();
    }

    public void AddAudioData(float[] newAudioData)
    {
        foreach (var sample in newAudioData)
            audioQueue.Enqueue(sample);
    }

    public void StartResponse()
    {
        if (!_isIncrementingAudioEndMs)
        {
            AudioStreamMediator.audio_end_ms = 0; // Reset only if not already incrementing
            _ = Task.Run(IncrementAudioEnd);
        }
    }


    public void InterruptAudio()
    {
        if (_isIncrementingAudioEndMs)
        {
            _isIncrementingAudioEndMs = false;
            audioQueue.Clear();
            AudioStreamMediator.isAudioPlaying = false;
        }
    }

    //HELPERS
    private float GetNextSample()
    {
        if (audioQueue.TryDequeue(out float sample))
        {
            AudioStreamMediator.isAudioPlaying = true;
            return sample;
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
                AudioStreamMediator.audio_end_ms += 50;
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in IncrementAudioEnd: {ex.Message}");
        }

    }

}