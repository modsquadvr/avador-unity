using System;
using System.Collections;
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

	public void PlayDormant()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[0]);
	}
	
	public void PlaySwimUp()
	{
		_animationPlayer.PlayAnimation(_playableAnimations[1]);
		StartCoroutine(PlayTalkingIdleInABit());
	}
	private IEnumerator PlayTalkingIdleInABit()
	{
		yield return new WaitForSeconds(_playableAnimations[1].Clip.length);

		PlayTalkingIdle();
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
