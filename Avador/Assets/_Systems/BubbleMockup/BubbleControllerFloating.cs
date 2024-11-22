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
    [SerializeField] private GameObject _mockupPictureOne;
    [SerializeField] private GameObject _mockupPictureTwo;
    [SerializeField] private AnimationRunner _animationRunner;

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

    public void SelectBubble()
    {
        Debug.Log("Selecting bubble");
        int selectedBubble = Random.Range(0, _bubbles.Count);
        for (int i = 0; i < _bubbles.Count; i++)
        {
            if (i == selectedBubble)
            {
                _bubbles[i].OnSelectionComplete += TransitionToShowcase;
                _bubbles[i].OnSelect();
            }
            else _bubbles[i].OnOtherSelected();
        }
        
        _animationRunner.SetTalking(true);

        StartCoroutine(MoveAvatar());
    }

    private IEnumerator MoveAvatar()
    {
        float time = 0;
        Vector3 startingPos = _mockupPictureTwo.transform.position;
        Quaternion startingRos = _mockupPictureTwo.transform.rotation;
        
        while (time < 1)
        {
            time += Time.deltaTime;
            _mockupPictureTwo.transform.position =
                Vector3.Lerp(startingPos, _mockupPictureOne.transform.position, time);
            _mockupPictureTwo.transform.rotation = Quaternion.Lerp(startingRos, _mockupPictureOne.transform.rotation, time);
            yield return null;
        }
        
        // _mockupPictureTwo.SetActive(false);
        // _mockupPictureOne.SetActive(true);
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
        _bubbles.Remove(bubble);
        Destroy(bubble.gameObject);
    }

    private void SpawnBubble()
    {
        Bubble bubble = Instantiate(_bubblePrefab, transform);
        _bubbles.Add(bubble);
        bubble.Initialize(_speed);
        bubble.transform.position = new Vector3(Random.Range(-_spawnRange,_spawnRange), transform.position.y, 0);
        bubble.OnBubbleShouldBeDestroyed += RemoveBubble;
    }
}
