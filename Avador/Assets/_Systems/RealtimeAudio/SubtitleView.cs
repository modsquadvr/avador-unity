using TMPro;
using UnityEngine;

public class SubtitleView : MonoBehaviour
{
    public TMP_Text subtitleText;

    void Awake()
    => RealtimeClient.Instance.OnResponseDone += UpdateText;

    void OnDestroy()
    => RealtimeClient.Instance.OnResponseDone -= UpdateText;

    public void UpdateText(string text)
    {
        subtitleText.text = text;
    }
}