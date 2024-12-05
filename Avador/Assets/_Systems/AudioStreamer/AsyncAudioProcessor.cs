using UnityEngine;
using System;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;
using NAudio.Dsp;

public class AsyncAudioProcessor : MonoBehaviour
{
    public AudioSource audioSource;
    WaveFormatConversionStream resampler;
    private WaveFormat inputFormat;
    private WaveFormat targetFormat;
    private BufferedWaveProvider waveProvider;

    void Start()
    {
        inputFormat = new WaveFormat(44100, 1); // Example input format
        targetFormat = new WaveFormat(24000, 16, 1); // Target: 24kHz, 16-bit, mono
        waveProvider = new BufferedWaveProvider(inputFormat);
        WaveStream waveStream = new WaveProviderToWaveStream(waveProvider); // Wrap waveProvider
        resampler = new WaveFormatConversionStream(targetFormat, waveStream);
    }

    public void ProcessAudioAsync(float[] data)
    {
        Task.Run(() =>
        {
            try
            {
                byte[] rawData = ConvertFloatToPCM(data, inputFormat);

                lock (waveProvider)
                {
                    waveProvider.AddSamples(rawData, 0, rawData.Length);
                }

                byte[] resampledData = ResampleAudio();

                PlayResampledAudio(resampledData);
                // Dispatcher.ExecuteOnMainThread.Enqueue(() =>
                // {
                // });
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during processing: " + ex.Message);
            }
        });
    }

    private byte[] ConvertFloatToPCM(float[] data, WaveFormat format)
    {
        int byteCount = data.Length * sizeof(short);
        byte[] byteData = new byte[byteCount];

        for (int i = 0; i < data.Length; i++)
        {
            short pcmSample = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);
            BitConverter.GetBytes(pcmSample).CopyTo(byteData, i * sizeof(short));
        }

        return byteData;
    }

    private byte[] ResampleAudio()
    {
        int targetChunkSize = targetFormat.AverageBytesPerSecond / 10; // Smaller chunks
        byte[] buffer = new byte[targetChunkSize];
        int bytesRead;

        using (MemoryStream memoryStream = new MemoryStream())
        {
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }

            return memoryStream.ToArray();
        }
    }

    public void PlayResampledAudio(byte[] resampledData)
    {
        // Convert resampled data to float[]
        float[] audioData = ConvertPCM16ToFloat(resampledData);
        AudioClip clip = AudioClip.Create("ResampledAudio", audioData.Length, 1, 24000, false);
        clip.SetData(audioData, 0);
        audioSource.clip = clip;
        audioSource.Play();
    }

    private float[] ConvertPCM16ToFloat(byte[] pcmData)
    {
        int sampleCount = pcmData.Length / 2; // 2 bytes per sample (16-bit PCM)
        float[] floatData = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8)); // Little-endian
            floatData[i] = sample / 32768f;
        }

        return floatData;
    }

    //HELPERS
    public class WaveProviderToWaveStream : WaveStream
    {
        private readonly IWaveProvider waveProvider;
        private readonly WaveFormat waveFormat;
        private readonly byte[] buffer;
        private int bufferOffset;
        private int bytesInBuffer;

        public WaveProviderToWaveStream(IWaveProvider waveProvider)
        {
            this.waveProvider = waveProvider;
            this.waveFormat = waveProvider.WaveFormat;
            this.buffer = new byte[waveFormat.AverageBytesPerSecond];
        }

        public override WaveFormat WaveFormat => waveFormat;

        public override long Length => long.MaxValue; // Streaming source, infinite length

        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesCopied = 0;
            while (bytesCopied < count)
            {
                if (bufferOffset >= bytesInBuffer)
                {
                    bytesInBuffer = waveProvider.Read(this.buffer, 0, this.buffer.Length);
                    bufferOffset = 0;
                    if (bytesInBuffer == 0)
                    {
                        break;
                    }
                }

                int bytesToCopy = Math.Min(count - bytesCopied, bytesInBuffer - bufferOffset);
                Array.Copy(this.buffer, bufferOffset, buffer, offset + bytesCopied, bytesToCopy);
                bytesCopied += bytesToCopy;
                bufferOffset += bytesToCopy;
            }
            return bytesCopied;
        }
    }
}
