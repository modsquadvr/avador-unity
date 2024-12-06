using UnityEngine;
using NAudio.Dsp;
using System;

public partial class MicrophoneStreamerWithLocalVAD : MonoBehaviour
{
    [SerializeField] private AudioSettings settings;

    public int sampleRate = 44100;
    public int bufferSize = 1024;
    private float energyThreshold = 0.6f;
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
    public static MicrophoneStreamerWithLocalVAD Instance;

    void Awake()
    {
        if (Instance != null)
        {
            throw new Exception("There can only be one MicrophoneStreamer_OLD");
        }
    }

    void Start()
    {

        Instance = this;
        stateMachine = new();
        stateMachine.AddState(MicrophoneState.SILENT, new Silent(this));
        stateMachine.AddState(MicrophoneState.SPEAKING, new Speaking(this));
        stateMachine.Init(MicrophoneState.SILENT);

        audioSource = gameObject.AddComponent<AudioSource>();

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
        audioSource.clip = Microphone.Start(microphoneName, true, 1, sampleRate);
        audioSource.loop = true;

        while (!(Microphone.GetPosition(microphoneName) > 0)) { }

        audioSource.Play();

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
        int fftLength = AudioProcessHelpers.NextPowerOfTwo(buffer.Length); // FFT length must be a power of 2
        Complex[] fftBuffer = new Complex[fftLength];
        AudioProcessHelpers.ApplyHammingWindow(buffer, fftBuffer);

        FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

        float speechEnergy = AudioProcessHelpers.ComputeSpeechBandEnergy(fftBuffer, fftLength, sampleRate);

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
    }
}
