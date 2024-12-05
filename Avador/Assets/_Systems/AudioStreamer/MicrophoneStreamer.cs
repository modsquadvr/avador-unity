using UnityEngine;
using NAudio.Dsp;
using System;

public partial class MicrophoneStreamer : MonoBehaviour
{
    [SerializeField] private AudioSettings settings;

    public int sampleRate => settings.sampleRate;
    public int bufferSize => settings.bufferSize;
    private float energyThreshold => AudioSettings.energyThreshold;
    private AudioSource audioSource;
    private string microphoneName;
    public float[] sharedBuffer;
    private readonly object lockObject = new object();
    private MicrophoneStateMachine stateMachine;
    public Action OnStartedSpeaking;
    public Action OnStoppedSpeaking;
    public Action<float[]> OnAudioProvided;
    private bool isVoiceActive;

    //singleton
    public static MicrophoneStreamer Instance;

    private AudioPlayer player;
    void Awake()
    {
        if (Instance != null)
        {
            throw new Exception("There can only be one MicrophoneStreamer");
        }
    }
    // private AsyncAudioProcessor processor;

    void Start()
    {

        Instance = this;
        stateMachine = new();
        stateMachine.AddState(MicrophoneState.SILENT, new Silent(this));
        stateMachine.AddState(MicrophoneState.SPEAKING, new Speaking(this));
        stateMachine.Init(MicrophoneState.SILENT);

        audioSource = gameObject.AddComponent<AudioSource>();
        // processor = GetComponent<AsyncAudioProcessor>();
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

        player = new();
    }

    private void StartMicrophone()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
        }

        // // Start capturing microphone input
        audioSource.clip = Microphone.Start(microphoneName, true, 1, sampleRate); // Looping, 1-second buffer
        audioSource.loop = true;

        // // Wait until the microphone starts recording
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

            Array.Copy(data, sharedBuffer, data.Length);
        }

        // Process the shared buffer using NAudio (pseudo-code)
        // processor.ProcessAudioAsync(data);
        ProcessWithNAudio(sharedBuffer);
        stateMachine.Update();

        lock (lockObject)
        {
            // Copy back processed data
            Array.Copy(sharedBuffer, data, sharedBuffer.Length);
        }
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
        isVoiceActive = speechEnergy > energyThreshold;
    }



    private void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
            Debug.Log("Microphone stopped.");
        }
        Instance = null;
        player.Dispose();
    }
}
