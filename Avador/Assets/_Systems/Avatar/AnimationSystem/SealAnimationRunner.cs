using System;
using System.Collections.Generic;
using UnityEngine;

//handles the logic for which animation should play and when
public class SealAnimationRunner : MonoBehaviour
{
	[SerializeField] private AnimationPlayer _animationPlayer;
	[SerializeField] private List<AnimationInfo> _playableAnimations;

	private void Start()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[0]);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			_animationPlayer.PlayAnimation(_playableAnimations[0]);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			_animationPlayer.PlayAnimation(_playableAnimations[1]);

		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			_animationPlayer.PlayAnimation(_playableAnimations[2]);

		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			_animationPlayer.PlayAnimation(_playableAnimations[3]);

		}
	}

	// private bool _talking;
	//
	// private void Start()
	// {
	// 	_animationPlayer.PlayAnimation(_playableAnimations[0]);
	// }
	//
	// private void Update()
	// {
	// 	if (Input.GetKeyDown(KeyCode.Alpha1))
	// 	{
	// 		SetTalking(!_talking);
	// 	}
	// }
	//
	// private void SetTalking(bool value)
	// {
	// 	_talking = value;
	// 	DecideAnimation();
	// }
	//
	// private void DecideAnimation()
	// {
	// 	if (_talking)
	// 	{
	// 		LoopPlayTalkingAnimations();
	// 	}
	// 	else
	// 	{
	// 		PlayIdleAnimation();
	// 	}
	// }
	// private void PlayIdleAnimation()
	// {
	// 	_animationPlayer.PlayAnimation(_playableAnimations[2]);
	// }
	//
	// private void LoopPlayTalkingAnimations()
	// {
	// 	_animationPlayer.PlayAnimation(_playableAnimations[1]);
	//
	// }
}
