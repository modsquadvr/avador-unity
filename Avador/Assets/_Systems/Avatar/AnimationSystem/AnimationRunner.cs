using System.Collections.Generic;
using UnityEngine;

//handles the logic for which animation should play and when
public class AnimationRunner : MonoBehaviour
{
	[SerializeField] private AnimationPlayer _animationPlayer;
	[SerializeField] private List<AnimationInfo> _playableAnimations;

	private bool _talking;

	private void Start()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[0]);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			SetTalking(!_talking);
		}
	}

	public void SetTalking(bool value)
	{
		_talking = value;
		DecideAnimation();
	}

	private void DecideAnimation()
	{
		if (_talking)
		{
			LoopPlayTalkingAnimations();
		}
		else
		{
			PlayIdleAnimation();
		}
	}
	private void PlayIdleAnimation()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[2]);
	}

	private void LoopPlayTalkingAnimations()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[1]);

	}
}
