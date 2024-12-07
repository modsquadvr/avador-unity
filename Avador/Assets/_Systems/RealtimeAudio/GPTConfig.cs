using UnityEngine;

[CreateAssetMenu(fileName = "GPTConfig", menuName = "GPTConfig")]
public class GPTConfig : ScriptableObject
{

    public Voices voice = Voices.sage;
    [TextArea(15, 20)]
    public string instructions = "You are a helpful assistant.";

    public enum Voices
    {
        alloy,
        echo,
        shimmer,
        ash,
        ballad,
        coral,
        sage,
        verse
    }
}