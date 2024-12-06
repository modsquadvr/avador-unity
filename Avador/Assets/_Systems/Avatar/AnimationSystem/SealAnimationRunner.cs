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

	public void PlayDormant()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[0]);
	}
	public void PlaySwimUp()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[1]);
	}
	public void PlayTalkingIdle()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[2]);
	}
	public void PlaySwimDown()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[3]);
	}
}
