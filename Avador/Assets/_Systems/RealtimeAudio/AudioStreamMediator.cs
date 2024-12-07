
using System;

public static class AudioStreamMediator
{
    public static event Action OnResponseCreated;
    public static event Action OnAudioInterrupted;

    public static int audio_end_ms;

    private static bool _isAudioPlaying;
    public static bool isAudioPlaying
    {
        get => _isAudioPlaying;
        set
        {
            if (_isAudioPlaying != value)
                _isAudioPlaying = value;
        }
    }

    public static void TriggerResponseCreated()
    => OnResponseCreated?.Invoke();

    public static void TriggerAudioInterrupted()
    => OnAudioInterrupted?.Invoke();

}