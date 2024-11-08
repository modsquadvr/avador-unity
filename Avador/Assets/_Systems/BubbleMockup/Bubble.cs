using System;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// NOTE: THIS IS A TEMPORARY MOCKUP CLASS. This is not to be built off of and should be redone once design is finalized.
/// </summary>
public class Bubble : MonoBehaviour
{
	public void Initialize(float speed)
	{
		Speed = speed + Random.Range(-0.2f,0.2f);
	}
	
	private enum State
	{
		FLOAT,
		SELECTION
	}

	private State _state = State.FLOAT;
	
	public Action<Bubble> OnSelectionComplete;
	public Action<Bubble> OnBubbleShouldBeDestroyed;
	
	public float Speed = 1.0f; // Controls the vertical floating speed
	public float NoiseScale = 4f; // Controls the scale of the Perlin noise
	public float NoiseIntensity = 1f; // Controls the strength of noise movement

	private Vector3 _initialPosition;
	private Vector3 _initialScale;
	private Vector3 _finalPosition;
	private float _randomOffset;
	private float _startTime;
	private bool _bubblePlayedSpawnAnimation = false;

	void Start()
	{
		// Record the initial position of each bubble
		_initialPosition = transform.localPosition;
		_finalPosition = _initialPosition;
		_initialScale = transform.localScale;
		_startTime = Time.time;
		_bubblePlayedSpawnAnimation = false;
		transform.localPosition = Vector3.zero;
		transform.localScale = Vector3.zero;
	}

	void Update()
	{
		if (_state != State.FLOAT) return;
		if (!_bubblePlayedSpawnAnimation)
			SpawnBubbleAnimation();
		MoveBubble();
		if (Camera.main.WorldToScreenPoint(transform.position).y > Screen.height + 100)
		{
			OnBubbleShouldBeDestroyed?.Invoke(this);
		}
	}

	private float _timeInSpawnAnimation = 0;
	private void SpawnBubbleAnimation()
	{
		_timeInSpawnAnimation += Time.deltaTime /2f;
		transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale, _timeInSpawnAnimation*2);
		transform.localPosition = Vector3.Lerp(Vector3.zero, _finalPosition, _timeInSpawnAnimation);
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

	public async void OnSelect()
	{
		//Disable regular update/switch states
		_state = State.SELECTION;
		transform.localScale = _initialScale;
		//Get center screen position and lerp to it
		Vector3 destination = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/2f,Screen.height/2f,0));
		destination.z = 0;

		Debug.Log($"[Bubble.OnSelect] destination: {destination}");
		float time = 0;
		while (time < 3)
		{
			time += Time.deltaTime;
			if ((transform.position - destination).magnitude < 0.01f) break;
			if (this == null) return;
			transform.position = Vector3.Lerp(transform.position, destination, time/3);
			await Task.Yield();
		}
		//Play pop animation or something when reaching center
			//Omitted in mockup
		//Trigger event for center reached
		OnSelectionComplete.Invoke(this);
	}

	public async void OnOtherSelected()
	{
		//Disable regular update/switch states
		_state = State.SELECTION;
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
		}
		//Destroy when off screen
		OnBubbleShouldBeDestroyed?.Invoke(this);
	}
}