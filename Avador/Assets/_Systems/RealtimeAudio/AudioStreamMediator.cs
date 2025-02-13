
using System;

public class AudioStreamMediator
{
    //singletone
    private static AudioStreamMediator _instance;
    public static AudioStreamMediator Instance
    {
        get
        {
            if (_instance is null)
                _instance = new();
            return _instance;
        }
    }

    public event Action OnResponseCreated;
    public event Action OnAudioInterrupted;

    public int audio_end_ms;

    private bool _isAudioPlaying;
    public bool isAudioPlaying
    {
        get => _isAudioPlaying;
        set
        {
            if (_isAudioPlaying != value)
                _isAudioPlaying = value;
        }
    }

    public void TriggerResponseCreated()
    => OnResponseCreated?.Invoke();

    public void TriggerAudioInterrupted()
    => OnAudioInterrupted?.Invoke();

}