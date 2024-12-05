using System;
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
            float window = 0.54f - 0.46f * (float)Math.Cos(2.0 * Math.PI * i / (len - 1));
            fftBuffer[i].X = buffer[i] * window;
            fftBuffer[i].Y = 0;
        }

        for (int i = len; i < fftBuffer.Length; i++)
        {
            fftBuffer[i].X = 0;
            fftBuffer[i].Y = 0;
        }
    }
    public static float ComputeSpeechBandEnergy(Complex[] fftBuffer, int fftLength, float SampleRate)
    {
        float binSize = (float)SampleRate / fftLength; // Hz per bin

        int startBin = (int)(300 / binSize);
        int endBin = (int)(3400 / binSize);

        //total energy in speech band
        float energy = 0f;
        for (int i = startBin; i <= endBin; i++)
        {
            energy += fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y;
        }

        // Scale energy
        energy *= 1e6f;

        return energy;
    }
}