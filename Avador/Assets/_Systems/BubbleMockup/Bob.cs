using UnityEngine;

public class Bob : MonoBehaviour
{
	[Header("Position Animation Curves")]
	public AnimationCurve positionX;
	public AnimationCurve positionY;
	public AnimationCurve positionZ;

	[Header("Rotation Animation Curves")]
	public AnimationCurve rotationX; // Euler angle in degrees
	public AnimationCurve rotationY; // Euler angle in degrees
	public AnimationCurve rotationZ; // Euler angle in degrees

	[Header("Animation Settings")]
	public float animationDuration = 8f; // Duration of the full animation cycle
	public bool loopAnimation = true;

	private float elapsedTime;

	void Update()
	{
		// Update the animation time
		elapsedTime += Time.deltaTime;
		if (loopAnimation)
		{
			elapsedTime %= animationDuration;
		}
		else if (elapsedTime > animationDuration)
		{
			return; // Stop updating if the animation is complete
		}

		// Calculate normalized time (0 to 1)
		float normalizedTime = elapsedTime / animationDuration;

		// Update position
		Vector3 newPosition = new Vector3(
			positionX.Evaluate(normalizedTime),
			positionY.Evaluate(normalizedTime),
			positionZ.Evaluate(normalizedTime)
		);
		transform.localPosition = newPosition;

		// Update rotation
		Vector3 newRotation = new Vector3(
			rotationX.Evaluate(normalizedTime),
			rotationY.Evaluate(normalizedTime),
			rotationZ.Evaluate(normalizedTime)
		);
		transform.localRotation = Quaternion.Euler(newRotation);
	}
}