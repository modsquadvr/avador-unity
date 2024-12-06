using UnityEngine;

public class MicrophoneStreamer : MonoBehaviour
{

    private AudioClip micInput;
    private string microphoneName;
    private int lastSamplePosition;


    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        microphoneName = Microphone.devices[0];
        micInput = Microphone.Start(microphoneName, true, 1, AudioConfig.microphoneSampleRate);
        lastSamplePosition = 0;


        print($"Using microphone '{microphoneName}'");
    }

    void Update()
    {
        if (micInput == null)
            return;

        int currentPosition = Microphone.GetPosition(microphoneName);
        int sampleCount = currentPosition - lastSamplePosition;

        if (sampleCount < 0)
        {
            sampleCount += micInput.samples;
        }

        if (sampleCount > 0)
        {
            float[] audioData = new float[sampleCount];
            micInput.GetData(audioData, lastSamplePosition);
            lastSamplePosition = currentPosition;

            AudioProcessor.Instance.ProcessAudioIn(audioData);

        }
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(microphoneName))
        {
            Microphone.End(microphoneName);
        }
    }
}