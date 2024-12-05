using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "ScriptableObjects/AudioSettings")]
public class AudioSettings : ScriptableObject
{

    public int sampleRate = 44100; // Adjust based on your requirements
    public int bufferSize = 1024;  // Size of the audio buffer
    [Tooltip("Used to compute energy bands for local VAD")]
    public const float energyThreshold = 0.6f;

}