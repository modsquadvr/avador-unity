using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteScaler : MonoBehaviour
{
	public float TargetWidth = 2f; 
	public float TargetHeight = 2f;
	public SpriteRenderer SpriteRenderer;

	public void SetSprite(Sprite new_sprite)
	{
		if (new_sprite == null) return;
		
		SpriteRenderer.sprite = new_sprite;
		Vector2 spriteSize = SpriteRenderer.bounds.size;
		
		float scaleX = TargetWidth / spriteSize.x;
		float scaleY = TargetHeight / spriteSize.y;

		// Set the new scale, maintaining aspect ratio
		float uniformScale = Mathf.Min(scaleX, scaleY);
		transform.localScale *= uniformScale;
	}
}