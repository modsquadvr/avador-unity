using System.Collections.Generic;
using UnityEngine;

public class BubbleController : MonoBehaviour
{
	[SerializeField] private GameObject _bubblePrefab;
	[SerializeField] private float _radius;
	[SerializeField] private float _speed;
	[SerializeField] private float _numberOfBubbles;

	private List<GameObject> _bubbles = new ();
	
	private void Start()
	{
		//Spawn bubbles in a circle
		for (int i = 0; i < _numberOfBubbles; i++)
		{
			float angle = i * Mathf.PI * 2f / _numberOfBubbles;
			Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * _radius;
			GameObject bubble = Instantiate(_bubblePrefab, transform);
			bubble.transform.localPosition = pos;
			_bubbles.Add(bubble);
		}
	}

	private void Update()
	{
		transform.Rotate(0,0,_speed*Time.deltaTime);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			foreach (GameObject bub in _bubbles)
				Destroy(bub);
			_bubbles.Clear();
			Start();
		}
	}
}
