using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleControllerFloating : MonoBehaviour
{
    [SerializeField] private Bubble _bubblePrefab;
    [SerializeField] private float _speed;
    [SerializeField] private float _spawnRate;
    [SerializeField] private float _spawnRange;
    [SerializeField] private int _burstAmount;
    [SerializeField] private int _burstDelay;
    [SerializeField] private CanvasGroup _showcaseInfo;
    
    [SerializeField] private ContentProvider _contentProvider;
    private List<int> _museumObjectIdsCurrentlyInBubbles = new();

    private int _burstCount;

    private List<Bubble> _bubbles = new ();
    private float _timer = 0;

    public void UpdateBubbles()
    { 
        _timer += Time.deltaTime;
        if (_burstCount > _burstAmount)
        {
            if (_timer > _burstDelay)
            {
                _timer = 0;
                _burstCount = 0;
            }
            return;
        }
        if (_timer > 1/_spawnRate)
        {
            _timer -= 1/_spawnRate;
            SpawnBubble();
            _burstCount++;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
            SelectBubble(0);
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectBubble(1);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectBubble(2);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectBubble(3);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            SelectBubble(4);
    }

    public void SelectBubble(int bubble_id)
    {
        Debug.Log("Selecting bubble");
        Bubble selectedBubble = _bubbles.Find(bubble => bubble.Id == bubble_id);
        if (selectedBubble == null) Debug.LogWarning("Requested bubble not present.");
        
        foreach (Bubble bubble in _bubbles)
        {
            if (bubble == selectedBubble)
            {
                bubble.OnSelectionComplete += TransitionToShowcase;
                bubble.OnSelect();
            }
            else bubble.OnOtherSelected();
        }
    }

    private void TransitionToShowcase(Bubble bubble)
    {
        bubble.OnSelectionComplete -= TransitionToShowcase;
        Debug.Log("Transition complete");
        StartCoroutine(FadeInShowcaseScreen());
    }

    private IEnumerator FadeInShowcaseScreen()
    {
        float alpha = 0;
        while (alpha < 1)
        {
            alpha += Time.deltaTime;
            _showcaseInfo.alpha = alpha;
            yield return null;
        }
    }

    private void RemoveBubble(Bubble bubble)
    {
        bubble.OnBubbleShouldBeDestroyed -= RemoveBubble;
        _museumObjectIdsCurrentlyInBubbles.Remove(bubble.Id);
        _bubbles.Remove(bubble);
        Destroy(bubble.gameObject);
    }

    private void SpawnBubble()
    {
        MuseumObjectSO museumObjectSO = GetMuseumObjectNotAlreadyInABubble();
        if(museumObjectSO == null) return;
        
        Bubble bubble = Instantiate(_bubblePrefab, transform);
        _museumObjectIdsCurrentlyInBubbles.Add(museumObjectSO.Id);
        _bubbles.Add(bubble);
        
        bubble.Initialize(_speed, museumObjectSO.MainImage, museumObjectSO.Id);
        bubble.transform.position = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.OnBubbleShouldBeDestroyed += RemoveBubble;
    }

    private static int _museumObjectIdIterator;
    private MuseumObjectSO GetMuseumObjectNotAlreadyInABubble()
    {
        if (_contentProvider.MaximumId < 0) return null;
            
        int start = _museumObjectIdIterator;
        for (_museumObjectIdIterator = start; _museumObjectIdIterator != start - 1; _museumObjectIdIterator++)
        {
            if (_museumObjectIdIterator > _contentProvider.MaximumId) _museumObjectIdIterator = 0;
            if (!_museumObjectIdsCurrentlyInBubbles.Contains(_museumObjectIdIterator)) 
                return _contentProvider.MuseumObjectSOs[_museumObjectIdIterator];
            if (_museumObjectIdIterator == _contentProvider.MaximumId && start == 0) break;
        }
        return null;
    }
}
