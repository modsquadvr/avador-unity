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

    [SerializeField] public SealAnimationRunner _sealAnimationRunner;
    [SerializeField] private SpriteFader _otherImagesDisplay;

    [SerializeField] private Transform _bubbleDestinationOnSelect;
    
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

    public void SelectBubble(int bubble_id)
    {
        Bubble selectedBubble = _bubbles.Find(bubble => bubble.Id == bubble_id);
        if (selectedBubble == null)
        {
            SpawnSpecificBubble(bubble_id);
            selectedBubble = _bubbles.Find(bubble => bubble.Id == bubble_id);
        }
        
        foreach (Bubble bubble in _bubbles)
        {
            if (bubble == selectedBubble)
            {
                bubble.OnSelectionComplete += TransitionToShowcase;
                bubble.OnSelect(_bubbleDestinationOnSelect.position);
            }
            else bubble.OnOtherSelected();
        }
    }

    private bool skippedFirstSwimDown;
    public void EnterBubbleProducingState()
    {
        _otherImagesDisplay.StopFadingSprites();
        List<Bubble> tempBubbles = new(_bubbles);
        foreach (Bubble bubble in tempBubbles)
        {
            RemoveBubble(bubble);
        }
        if (!skippedFirstSwimDown)
        {
            skippedFirstSwimDown = true;
            return;
        }
        _sealAnimationRunner.PlaySwimDown();
        _timer = 0;
        _burstCount = 0;
    }

    private void TransitionToShowcase(Bubble bubble)
    {
        bubble.OnSelectionComplete -= TransitionToShowcase;
        bubble.Pop();
        _otherImagesDisplay.StartFadingSprites(_contentProvider.MuseumObjectSOs[bubble.Id].OtherImages);
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
        
        bubble.transform.position = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.Initialize(_speed, museumObjectSO.MainImage, museumObjectSO.Id);
        bubble.OnBubbleShouldBeDestroyed += RemoveBubble;
    }
    
    private void SpawnSpecificBubble(int id)
    {
        MuseumObjectSO museumObjectSO = _contentProvider.MuseumObjectSOs[id];
        if(museumObjectSO == null) return;
        
        Bubble bubble = Instantiate(_bubblePrefab, transform);
        _museumObjectIdsCurrentlyInBubbles.Add(museumObjectSO.Id);
        _bubbles.Add(bubble);
        
        bubble.transform.localPosition = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.Initialize(_speed, museumObjectSO.MainImage, museumObjectSO.Id, true);
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
