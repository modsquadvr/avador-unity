using System;
using System.Diagnostics;
using NAudio.Dsp;
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

}