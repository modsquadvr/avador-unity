using UnityEngine;
using System;
using System.IO;
using NAudio.Wave;
using NAudio.Dsp;
using System.Runtime.Remoting.Messaging;

public static class AudioProcessHelpers
{
    public static int NextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value) power <<= 1;
        return power;
    }

    public static void ApplyHammingWindow(float[] buffer, Complex[] fftBuffer)
    {
        int len = buffer.Length;
        for (int i = 0; i < len; i++)
        {
            // Hamming window function
            float window = 0.54f - 0.46f * (float)Math.Cos(2.0 * Math.PI * i / (len - 1));
            fftBuffer[i].X = buffer[i] * window; // Real part
            fftBuffer[i].Y = 0;                 // Imaginary part
        }

        // Zero-pad if FFT length is greater than input buffer
        for (int i = len; i < fftBuffer.Length; i++)
        {
            fftBuffer[i].X = 0;
            fftBuffer[i].Y = 0;
        }
    }
    public static float ComputeSpeechBandEnergy(Complex[] fftBuffer, int fftLength, float SampleRate)
    {
        // Frequency resolution (bin size)
        float binSize = (float)SampleRate / fftLength; // Hz per bin

        // Calculate bin indices for 300 Hz to 3400 Hz
        int startBin = (int)(300 / binSize);  // Lower bound of speech band
        int endBin = (int)(3400 / binSize);  // Upper bound of speech band

        Console.WriteLine($"Frequency Resolution: {binSize} Hz per bin");
        Console.WriteLine($"Speech Band Bin Range: {startBin} to {endBin}");

        // Calculate total energy in the speech band
        float energy = 0f;
        for (int i = startBin; i <= endBin; i++)
        {
            energy += fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y;
        }

        // Scale energy for better interpretability
        energy *= 1e6f; // Scale factor to make values more human-readable

        Console.WriteLine($"Speech Band Energy: {energy}");
        return energy;
    }


    private static AudioResamplerAndEncoder audioResamplerAndEncoder;
    /// <summary>
    /// Resample a byte array from 44100 stereo to 24k mono
    /// </summary>
    /// <returns></returns>
    public static byte[] Resample(float[] buffer)
    {
        if (audioResamplerAndEncoder is null)
            audioResamplerAndEncoder = new();
        return audioResamplerAndEncoder.Resample(buffer);
    }
    public static string Encode(byte[] buffer)
    {
        if (audioResamplerAndEncoder is null)
            audioResamplerAndEncoder = new();
        return audioResamplerAndEncoder.Encode(buffer);
    }

    private class AudioResamplerAndEncoder
    {

        BufferedWaveProvider waveProvider;
        WaveFormat inputFormat;
        WaveFormat targetFormat;
        WaveFormatConversionStream resampler;
        private MemoryStream outputStream;

        public AudioResamplerAndEncoder()
        {
            inputFormat = new WaveFormat(MicrophoneStreamer.Instance.sampleRate, 1); // Unity audio format (mono for simplicity)
            targetFormat = new WaveFormat(24000, 16, 1); // Target: 24 kHz, 16-bit PCM, mono
            waveProvider = new BufferedWaveProvider(inputFormat);
            WaveStream waveStream = new WaveProviderToWaveStream(waveProvider); // Wrap waveProvider
            resampler = new WaveFormatConversionStream(targetFormat, waveStream);
            outputStream = new MemoryStream();
        }

        public byte[] Resample(float[] data)
        {
            byte[] byteData = ConvertFloatToPCM(data, inputFormat);
            waveProvider.AddSamples(byteData, 0, byteData.Length);

            return ResampleAudio();
        }
        public string Encode(byte[] data) => Convert.ToBase64String(data);

        //HELPERS
        private byte[] ConvertFloatToPCM(float[] data, WaveFormat format)
        {
            // Convert Unity float[-1, 1] audio to 16-bit PCM
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
            try
            {
                int targetChunkSize = targetFormat.AverageBytesPerSecond / 10; // Adjust for smaller chunks
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
            catch (Exception ex)
            {
                Debug.LogError("Error during resampling: " + ex.Message);
                return null;
            }
        }

    }
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