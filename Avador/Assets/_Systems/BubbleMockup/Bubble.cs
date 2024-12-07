using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// NOTE: THIS IS A TEMPORARY MOCKUP CLASS. This is not to be built off of and should be redone once design is finalized.
/// </summary>
public class Bubble : MonoBehaviour
{
	public void Initialize(float speed, Sprite object_image, int object_id, bool start_at_full_size = false)
	{
		Speed = speed + Random.Range(-0.2f,0.2f);
		MuseumObjectSpriteRenderer.SetSprite(object_image);
		Id = object_id;
		IdText.text = Id.ToString();
		
		_initialPosition = transform.localPosition;
		_finalPosition = _initialPosition;
		_initialScale = transform.localScale;
		_startTime = Time.time;
		_bubblePlayedSpawnAnimation = false;

		_bubbleRadius = GetComponent<Collider2D>().bounds.size.x/2;
		
		if(start_at_full_size) return;
		transform.localPosition = Vector3.zero;
		transform.localScale = Vector3.zero;
	}
	
	private enum State
	{
		FLOAT,
		SELECTED,
		NOT_SELECTED
	}

	private State _state = State.FLOAT;
	
	public Action<Bubble> OnSelectionComplete;
	public Action<Bubble> OnBubbleShouldBeDestroyed;

	public int Id; // Corresponds to the Id of the object pictured in the bubble
	public SpriteScaler MuseumObjectSpriteRenderer;
	public TMP_Text IdText;
	public ParticleSystem BubblePop;
	public SpriteRenderer BubbleSpriteRenderer;
	
	public float Speed = 1.0f; // Controls the vertical floating speed
	public float NoiseScale = 4f; // Controls the scale of the Perlin noise
	public float NoiseIntensity = 1f; // Controls the strength of noise movement
	public float CollisionForceWithOtherBubbles = 2f;

	private Vector3 _initialPosition;
	private Vector3 _initialScale;
	private Vector3 _finalPosition;
	private float _randomOffset;
	private float _startTime;
	private bool _bubblePlayedSpawnAnimation = false;
	private float _bubbleRadius;
	
	void Update()
	{
		if (_state != State.FLOAT) return;
		if (!_bubblePlayedSpawnAnimation)
			SpawnBubbleAnimation();
		MoveBubble();
		if (Camera.main.WorldToScreenPoint(transform.position).y > Screen.height + 250)
		{
			OnBubbleShouldBeDestroyed?.Invoke(this);
		}
	}

	private float _timeInSpawnAnimation = 0;

	private void SpawnBubbleAnimation()
	{
		_timeInSpawnAnimation += Time.deltaTime /2f;
		transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale, _timeInSpawnAnimation*2);
		// if (_timeInSpawnAnimation < 0.5f) transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.left, _timeInSpawnAnimation*2);
		// else 
			transform.localPosition = Vector3.Lerp(Vector3.left*1.2f, _finalPosition, (_timeInSpawnAnimation * 1.5f - 0.5f));
		_initialPosition = transform.localPosition;
		
		if (_timeInSpawnAnimation >= 1) _bubblePlayedSpawnAnimation = true;
	}

	private void MoveBubble()
	{
		// Calculate the new Y position with upward movement
		float newY = _initialPosition.y + (Speed * (Time.time - _startTime));

		// Apply Perlin noise to X and Z for subtle horizontal drift
		float noiseX = Mathf.PerlinNoise((Time.time - _startTime) * NoiseScale + _randomOffset, 0) - 0.5f;
		
		// Update bubble position
		transform.localPosition = new Vector3(
			_initialPosition.x + noiseX * NoiseIntensity,
			newY, 0
		);
	}
	
	public async void OnSelect(Vector3 destination)
	{
		GetComponent<Collider2D>().enabled = false;
		//Disable regular update/switch states
		_state = State.SELECTED;
		transform.localScale = _initialScale;

		float time = 0;
		while (time < 3)
		{
			time += Time.deltaTime;
			if ((transform.position - destination).magnitude < 0.01f) break;
			if (this == null) return;
			transform.position = Vector3.Lerp(transform.position, destination, time/3);
			await Task.Yield();
			if (_state == State.NOT_SELECTED) //If a new bubble was selected, return
				return;
		}
		//Play pop animation or something when reaching center
			//Omitted in mockup
		//Trigger event for center reached
		OnSelectionComplete.Invoke(this);
	}

	public async void OnOtherSelected()
	{
		GetComponent<Collider2D>().enabled = false;
		
		//Disable regular update/switch states
		_state = State.NOT_SELECTED;
		//Get the direction to the center, and move in the opposite of that direction
		Vector3 directionToCenter = (Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/2f,Screen.height/2f,0)) - transform.position);
		directionToCenter.z = 0;
		directionToCenter = directionToCenter.normalized;
		
		float time = 0;
		while (time < 3)
		{
			time += Time.deltaTime;
			if (this == null) return;
			transform.position -= directionToCenter * 4f *Time.deltaTime;
			await Task.Yield();
			if (_state == State.SELECTED) //if it got selected while moving away, don't destroy it
				return;
		}
		//Destroy when off screen
		OnBubbleShouldBeDestroyed?.Invoke(this);
	}

	private void OnCollisionStay2D(Collision2D other)
	{
		if (_state != State.FLOAT) return;
		//Move away from other bubble
		Vector3 direction = new Vector2(transform.position.x, transform.position.y) - other.contacts[0].point;
		direction.y *= 0.2f; // favour horizontal movement
		direction.Normalize();
		float distance = Vector3.Distance(new Vector2(transform.position.x, transform.position.y), other.contacts[0].point);
		_initialPosition += direction * CollisionForceWithOtherBubbles * Time.deltaTime * (_bubbleRadius - distance);
	}

	public void Pop()
	{
		BubbleSpriteRenderer.enabled = false;
		BubblePop.Play();
		IdText.enabled = false;
	}
}
