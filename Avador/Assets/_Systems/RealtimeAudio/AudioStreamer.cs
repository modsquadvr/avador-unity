using System.Collections.Concurrent;
using UnityEngine;

public class AudioStreamer : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip audioClip;

    private ConcurrentQueue<float> audioQueue = new ConcurrentQueue<float>();


    void Awake()
    => AudioProcessor.Instance.OnOutputAudioProcessed += AddAudioData;

    void OnDestroy()
    => AudioProcessor.Instance.OnOutputAudioProcessed -= AddAudioData;

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


    //HELPERS
    private float GetNextSample()
    {
        if (audioQueue.TryDequeue(out float sample))
            return sample;

        return 0.0f;
    }
}