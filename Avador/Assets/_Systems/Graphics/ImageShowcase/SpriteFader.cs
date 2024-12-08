using UnityEngine;
using System.Collections;

public class SpriteFader : MonoBehaviour
{
    [Tooltip("SpriteRenderer to display the sprites.")]
    public SpriteRenderer SpriteRenderer;

    public SpriteScaler SpriteScaler;

    [Tooltip("Time in seconds for each fade in or fade out.")]
    public float FadeDuration = 1f;

    [Tooltip("Time in seconds to hold each sprite before fading out.")]
    public float HoldDuration = 2f;

    private int _currentSpriteIndex = 0;

    public void StartFadingSprites(Sprite[] sprites)
    {
        StopAllCoroutines();

        StartCoroutine(FadeSprites(sprites));
    }

    public void StopFadingSprites()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeSprites(Sprite[] sprites)
    {
        _currentSpriteIndex = 0;
        if(sprites.Length == 0)
        {
            StartCoroutine(FadeOut());
            yield break;
        }
        
        while (true)
        {
            // Set the current sprite
            SpriteScaler.SetSprite(sprites[_currentSpriteIndex]);

            // Fade in the sprite
            yield return StartCoroutine(FadeIn());

            if (sprites.Length == 1) yield break;

            // Hold the sprite
            yield return new WaitForSeconds(HoldDuration);

            // Fade out the sprite
            yield return StartCoroutine(FadeOut());

            // Move to the next sprite (loop back to the start if at the end)
            _currentSpriteIndex = (_currentSpriteIndex + 1) % sprites.Length;
        }
    }

    private IEnumerator FadeIn()
    {
        float timer = 0f;
        Color color = SpriteRenderer.color;

        while (timer < FadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / FadeDuration);
            SpriteRenderer.color = color;
            yield return null;
        }
        color.a = 1f;
        SpriteRenderer.color = color;
    }

    private IEnumerator FadeOut()
    {
        Color color = SpriteRenderer.color;
        float beginningAlpha = color.a;
        float timer = 1 - beginningAlpha;

        while (timer < FadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(beginningAlpha, 0f, timer / FadeDuration);
            SpriteRenderer.color = color;
            yield return null;
        }
        color.a = 0f;
        SpriteRenderer.color = color;
    }
}
