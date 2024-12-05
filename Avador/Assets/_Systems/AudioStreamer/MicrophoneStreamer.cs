using UnityEngine;
using NAudio.Dsp;
using System;

public class MicrophoneStreamer : MonoBehaviour
{
    private AudioSource audioSource;
    private string microphoneName;
    private int sampleRate = 44100; // Adjust based on your requirements

    public const float energyThreshold = 0.6f;
    private int bufferSize = 1024;  // Size of the audio buffer

    private float[] sharedBuffer;
    private readonly object lockObject = new object();

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        // Select the first microphone available
        if (Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneName);

            StartMicrophone();
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    private void StartMicrophone()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
        }

        // Start capturing microphone input
        audioSource.clip = Microphone.Start(microphoneName, true, 1, sampleRate); // Looping, 1-second buffer
        audioSource.loop = true;

        // Wait until the microphone starts recording
        while (!(Microphone.GetPosition(microphoneName) > 0)) { }

        audioSource.Play(); // Start playback to process the microphone audio

        Debug.Log("Microphone started.");
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        lock (lockObject)
        {
            // Copy incoming audio data to the shared buffer for NAudio processing
            if (sharedBuffer == null || sharedBuffer.Length != data.Length)
                sharedBuffer = new float[data.Length];

            System.Array.Copy(data, sharedBuffer, data.Length);
        }

        // Process the shared buffer using NAudio (pseudo-code)
        ProcessWithNAudio(sharedBuffer);

        lock (lockObject)
        {
            // Copy back processed data
            System.Array.Copy(sharedBuffer, data, sharedBuffer.Length);
        }
    }

    enum State
    {
        SILENT,
        SPEAKING
    }

    private void ProcessWithNAudio(float[] buffer)
    {
        // Step 1: Apply a window function (Hamming window)
        int fftLength = AudioProcessHelpers.NextPowerOfTwo(buffer.Length); // FFT length must be a power of 2
        Complex[] fftBuffer = new Complex[fftLength];
        AudioProcessHelpers.ApplyHammingWindow(buffer, fftBuffer);

        // Step 2: Perform FFT
        FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

        // Step 3: Analyze frequency spectrum
        float speechEnergy = AudioProcessHelpers.ComputeSpeechBandEnergy(fftBuffer, fftLength, sampleRate);

        // Step 4: Compare with threshold
        bool brokeAudioThreshold = speechEnergy > energyThreshold;

    }

    private void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
            Debug.Log("Microphone stopped.");
        }
    }
}
