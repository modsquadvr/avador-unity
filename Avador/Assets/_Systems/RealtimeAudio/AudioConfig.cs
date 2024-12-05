public static class AudioConfig
{
    public const int microphoneSampleRate = 44100;
    public const int targetSampleRate = 24000;

    //input resampling
    public const int targetNumChannels = 1;
    public const int targetSampleInterval = microphoneSampleRate / targetSampleRate;
}