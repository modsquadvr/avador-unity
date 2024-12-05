using System;
using System.IO;
using NAudio.Wave;
using System.Threading.Tasks;

public class AudioPlayer
{
    private WaveOutEvent waveOut;
    private RawSourceWaveStream waveStream;


    public void ResampleAndPlayAudio(float[] rawData)
    {
        Task.Run(() =>
        {
            try
            {
                // Resample audio in a separate thread
                byte[] resampledData = AudioProcessHelpers.Resample(rawData);

                // Play resampled audio using NAudio (on main thread if needed)
                Dispatcher.ExecuteOnMainThread.Enqueue(() =>
                {
                    PlayResampledAudio(resampledData);
                });
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Error in audio processing: " + ex.Message);
            }
        });
    }


    public void PlayResampledAudio(byte[] resampledData)
    {
        // Define the audio format (16-bit PCM, 24 kHz, mono)
        var waveFormat = new WaveFormat(24000, 16, 1);

        // Create a stream for the audio data
        MemoryStream memoryStream = new MemoryStream(resampledData);

        // Wrap the stream in a RawSourceWaveStream
        waveStream = new RawSourceWaveStream(memoryStream, waveFormat);

        // Create a WaveOutEvent for playback
        waveOut = new WaveOutEvent();
        waveOut.Init(waveStream);
        UnityEngine.Debug.Log($"{waveOut.DeviceNumber}");
        waveOut.Play();

        // Handle playback completion
        waveOut.PlaybackStopped += (s, e) =>
        {
            Dispose();
        };

        Console.WriteLine("Playing resampled audio...");
    }

    public void Stop()
    {
        waveOut?.Stop();
    }

    public void Dispose()
    {
        waveOut?.Dispose();
        waveStream?.Dispose();
    }
}
