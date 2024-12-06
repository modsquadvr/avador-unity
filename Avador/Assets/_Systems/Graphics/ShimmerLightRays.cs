using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShimmerLightRays : MonoBehaviour
{
	[SerializeField] private List<Transform> _rays;
	[SerializeField] private List<float> _amplitudes; // Amplitude for each ray
	[SerializeField] private List<float> _speeds;     // Speed for each ray

	private List<float> _startYPositions;

	private void Start()
	{
		if (_rays.Count != _amplitudes.Count || _rays.Count != _speeds.Count)
		{
			Debug.LogError("Rays, amplitudes, and speeds lists must have the same number of elements.");
			return;
		}

		// Store the starting y positions of the rays
		_startYPositions = new List<float>();
		foreach (var ray in _rays)
		{
			_startYPositions.Add(ray.position.y);
		}

		// Start the animation coroutine for each ray
		for (int i = 0; i < _rays.Count; i++)
		{
			int index = i; // Capture index for the closure
			StartCoroutine(AnimateRay(index));
		}
	}

	private IEnumerator AnimateRay(int index)
	{
		Transform ray = _rays[index];
		float amplitude = _amplitudes[index];
		float speed = _speeds[index];
		float startY = _startYPositions[index];

		while (true)
		{
			float offsetY = Mathf.Sin(Time.time * speed) * amplitude;
			ray.position = new Vector3(ray.position.x, startY + offsetY, ray.position.z);
			yield return null; // Wait for the next frame
		}
	}
}
