using System;
using System.Runtime.InteropServices;
using System.Threading;

public class AudioStreamer
{
    public event Action<byte[]> AudioDataAvailable;
    private const int SAMPLE_RATE = 44100;
    private const short BITS_PER_SAMPLE = 16;
    private const short CHANNELS = 1;
    private IntPtr waveInHandle;
    private AutoResetEvent recordEvent = new AutoResetEvent(false);

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveFormat
    {
        public short wFormatTag;
        public short nChannels;
        public int nSamplesPerSec;
        public int nAvgBytesPerSec;
        public short nBlockAlign;
        public short wBitsPerSample;
        public short cbSize;
    }

    [DllImport("winmm.dll")]
    private static extern int waveInOpen(out IntPtr hWaveIn, int uDeviceID, WaveFormat lpFormat, WaveInProc dwCallback, int dwInstance, int dwFlags);

    [DllImport("winmm.dll")]
    private static extern int waveInStart(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInStop(IntPtr hWaveIn);

    [DllImport("winmm.dll")]
    private static extern int waveInClose(IntPtr hWaveIn);

    private delegate void WaveInProc(IntPtr hwi, uint uMsg, int dwInstance, int dwParam1, int dwParam2);

    public void StartCapture()
    {
        WaveFormat waveFormat = new WaveFormat
        {
            wFormatTag = 1, // WAVE_FORMAT_PCM
            nChannels = CHANNELS,
            nSamplesPerSec = SAMPLE_RATE,
            wBitsPerSample = BITS_PER_SAMPLE,
            nBlockAlign = (short)(CHANNELS * (BITS_PER_SAMPLE / 8)),
            nAvgBytesPerSec = SAMPLE_RATE * CHANNELS * (BITS_PER_SAMPLE / 8),
            cbSize = 0
        };

        waveInOpen(out waveInHandle, 0, waveFormat, WaveInCallback, 0, 0x00030000); // CALLBACK_FUNCTION
        waveInStart(waveInHandle);
    }

    private void WaveInCallback(IntPtr hwi, uint uMsg, int dwInstance, int dwParam1, int dwParam2)
    {
        if (uMsg == 0x3BD) // WIM_DATA
        {
            byte[] buffer = new byte[dwParam1];
            Marshal.Copy((IntPtr)dwParam2, buffer, 0, buffer.Length);

            // Trigger the AudioDataAvailable event with the captured audio data
            AudioDataAvailable?.Invoke(buffer);
            recordEvent.Set();
        }
    }

    public void StopCapture()
    {
        waveInStop(waveInHandle);
        waveInClose(waveInHandle);
        recordEvent.Dispose();
    }
}