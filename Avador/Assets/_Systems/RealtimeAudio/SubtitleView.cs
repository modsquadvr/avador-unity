using TMPro;
using UnityEngine;

public class SubtitleView : MonoBehaviour
{
    public TMP_Text subtitleText;

    void Awake()
    => HandleSubscriptions();

    void OnDestroy()
    => HandleUnsubscriptions();


    private void StartSubtitle()
    {
        // clear when a new subtitle begins
        subtitleText.text = "";
    }

    public void UpdateSubtitle(string chunk)
    => subtitleText.text += chunk;

    // HELPERS - - -
    private void HandleSubscriptions()
    {
        RealtimeClient.Instance.OnResponseCreated += StartSubtitle;
        RealtimeClient.Instance.OnResponseAudioTranscriptDelta += UpdateSubtitle;
    }

    private void HandleUnsubscriptions()
    {
        if (RealtimeClient.Instance is null)
            return;

        RealtimeClient.Instance.OnResponseCreated -= StartSubtitle;
        RealtimeClient.Instance.OnResponseAudioTranscriptDelta -= UpdateSubtitle;
    }

}