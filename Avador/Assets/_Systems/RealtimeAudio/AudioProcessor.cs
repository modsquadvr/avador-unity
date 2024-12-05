using System;
using System.Threading.Tasks;
using UnityEngine;

public class AudioProcessor
{
    //singleton
    private static AudioProcessor _instance;
    public static AudioProcessor Instance
    {
        get
        {
            if (_instance is null)
                _instance = new();
            return _instance;
        }
    }

    public event Action<byte[]> OnInputAudioProcessed;
    public event Action<float[]> OnOutputAudioProcessed;

    public void ProcessAudioIn(float[] rawAudio)
    {
        Task.Run(() =>
        {
            byte[] processedAudio = ResampleInput(rawAudio);
            OnInputAudioProcessed?.Invoke(processedAudio);
        });
    }

    public void ProcessAudioOut(byte[] rawAudio)
    {
        Task.Run(() =>
        {
            float[] processedAudio = ResampleOutput(rawAudio);
            OnOutputAudioProcessed?.Invoke(processedAudio);
        });
    }

    /// <summary>
    /// resample audio data recieved from the microphone to a format that OpenAI can process (raw 16 bit PCM audio at 24kHz, 1 channel, little-endian)
    /// </summary>
    /// <param name="inputAudio"></param>
    /// <returns></returns>
    private byte[] ResampleInput(float[] inputAudio)
    {
        if (AudioConfig.targetSampleRate <= 0)
        {
            throw new ArgumentException("Input sample rate must be greater than target sample rate.");
        }

        int outputSampleCount = inputAudio.Length / AudioConfig.targetSampleRate;

        byte[] outputAudio = new byte[outputSampleCount * sizeof(short)]; // 16-bit PCM = 2 bytes / sample

        for (int i = 0, j = 0; i < outputSampleCount; i++, j += AudioConfig.targetSampleRate)
        {
            float clampedValue = Mathf.Clamp(inputAudio[j], -1.0f, 1.0f);

            // Convert to 16-bit PCM
            short pcmValue = (short)(clampedValue * short.MaxValue);

            // Write as little-endian bytes
            outputAudio[i * 2] = (byte)(pcmValue & 0xFF);
            outputAudio[i * 2 + 1] = (byte)((pcmValue >> 8) & 0xFF);
        }

        return outputAudio;
    }

    /// <summary>
    /// resample audio data recieved from OpenAI to a format that unity can process with an AudioSource
    /// </summary>
    /// <param name="inputAudio"></param>
    /// <returns></returns>
    private float[] ResampleOutput(byte[] inputAudio)
    {
        if (inputAudio == null || inputAudio.Length == 0)
            throw new ArgumentException("Input audio data is null or empty.");

        // Decode PCM16 (16-bit little-endian) to float[]
        float[] floatAudio = DecodePCM16ToFloat(inputAudio);

        return floatAudio;
    }

    //HELPERS
    // Helper: Decode PCM16 (16-bit) to float[]
    private float[] DecodePCM16ToFloat(byte[] pcmData)
    {
        int sampleCount = pcmData.Length / 2; // 2 bytes per sample
        float[] floatData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2); // Convert 2 bytes to short
            floatData[i] = sample / 32768f; // Normalize to range -1.0 to 1.0
        }

        return floatData;
    }
}
