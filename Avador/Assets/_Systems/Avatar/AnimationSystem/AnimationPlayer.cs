using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

[Serializable]
public struct AnimationInfo
{
    public AnimationClip Clip;
    public float BlendInTime;
    public bool Loop; //Loop is really something that should be set in the inspector for the clip. Perhaps it is included here for clarity?
    public bool BlendLoop;
    public bool RandomizeClip;
    public AnimationClip[] Clips;
    //public Action ClipFinishedPlaying; //DOES NOT GET CALLED IF THE ANIMATION LOOPS. DOES NOT GET CALLED IF BLENDING OUT BEFORE FINISHING - not implemented yet
}

public class AnimationPlayer : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    private PlayableGraph _playableGraph;
    private AnimationPlayableOutput _playableOutput;
    private AnimationMixerPlayable _topLevelMixer;

    private AnimationClipPlayable _activeClipPlayable;
    private AnimationInfo _activeClipInfo;
    
    
    //create (and destroy) graph.
    private void Awake()
    {
        _playableGraph = PlayableGraph.Create("AnimationSystem");
        
        _playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", _animator);
        
        _topLevelMixer = AnimationMixerPlayable.Create(_playableGraph, 2);
        _playableOutput.SetSourcePlayable(_topLevelMixer);
        _playableGraph.GetRootPlayable(0).SetInputWeight(0,1f);
    }

    private void OnDestroy()
    {
        _playableGraph.Destroy();
    }

    private void Update()
    {
        CheckAndHandleAnimationFinished();
    }

    //create playables for all the animations we want
    public void PlayAnimation(AnimationInfo clip_info)
    {
        _activeClipInfo = clip_info;

        //could have some error handling if no clips are given
        AnimationClip clipToPlay = clip_info.Clip;
        if (clip_info.RandomizeClip)
            clipToPlay = clip_info.Clips[Random.Range(0, clip_info.Clips.Length)];
            
        _activeClipPlayable = AnimationClipPlayable.Create(_playableGraph, clipToPlay);
        _activeClipPlayable.GetAnimationClip().wrapMode = clip_info.Loop && !clip_info.BlendLoop && !clip_info.RandomizeClip ? WrapMode.Loop : WrapMode.Once;
        
        var oldMixer = _topLevelMixer;
        _topLevelMixer = AnimationMixerPlayable.Create(_playableGraph, 2);
        _playableOutput.SetSourcePlayable(_topLevelMixer);
        
        var previousAnimation = oldMixer.GetInput(0);
        
        if (previousAnimation.IsNull())
        {
            //we don't have to blend
            _topLevelMixer.ConnectInput(0, _activeClipPlayable, 0);
            _topLevelMixer.SetInputWeight(0,1f);
            DestroyPlayableRecursively(oldMixer);
        }
        else
        {
            //we do have to blend. Make the old mixer a child of the new one, and blend the weight down from it and up on the new animation clip. It is done this way so we can blend even if we are already blending
            _topLevelMixer.ConnectInput(0, _activeClipPlayable, 0);
            _topLevelMixer.ConnectInput(1, oldMixer, 0);
            _topLevelMixer.SetInputWeight(0,0f);
            _topLevelMixer.SetInputWeight(1,1f);
            
            BlendTwoInputMixer(_topLevelMixer, clip_info.BlendInTime);
        }
        
        _playableGraph.Play();
    }

    private void CheckAndHandleAnimationFinished()
    {
        if (_activeClipPlayable.IsNull()) return; 
        
        if (_activeClipPlayable.GetTime() >= _activeClipPlayable.GetAnimationClip().length)
        {
            //the animation is over.
            if (_activeClipInfo.Loop && _activeClipInfo.RandomizeClip)
            {
                PlayAnimation(_activeClipInfo); //play it again. Play animation will randomize the clip
            }
            else
            {
                //return to default animation, i guess.
            }
        }
        else if (_activeClipInfo.BlendInTime < _activeClipInfo.Clip.length && _activeClipInfo.BlendLoop && _activeClipInfo.Loop && _activeClipPlayable.GetTime() + _activeClipInfo.BlendInTime  >= _activeClipPlayable.GetAnimationClip().length)
        {
            PlayAnimation(_activeClipInfo); //play it again as a new clip - this will result in it blending with itself.
        }
    }

    //assumes input 0 is blending up, input 1 is blending down
    private void BlendTwoInputMixer(AnimationMixerPlayable mixer, float blend_time, bool remove_blended_down_when_done = true)
    {
        StartCoroutine(blend());

        IEnumerator blend()
        {
            float weight = 0;
            float elapsedTime = 0f;

            while (weight < 1)
            {
                if (!mixer.IsValid())
                {
                    yield break; //NOTE: BREAKS WTIHOUT CALLING FINISHED, BC FINISHED ONLY DESTROYS CHILDREN, WHICH AN INVALID MIXER DOESN'T HAVE
                }
                mixer.SetInputWeight(0, weight);
                mixer.SetInputWeight(1, 1 - weight);

                yield return null;

                elapsedTime += Time.deltaTime;
                weight = elapsedTime / blend_time;
            }
            
            mixer.SetInputWeight(0, 1);
            mixer.SetInputWeight(1, 0);

            finished();
        }
        
        //see comment above about the yield break if you add more functionality to this function
        void finished()
        {
            if (remove_blended_down_when_done)
            {
                DestroyPlayableRecursively(mixer.GetInput(1));
                mixer.DisconnectInput(1);
            }
        }
    }
    
    public static void DestroyPlayableRecursively(Playable playable)
    {
        // Loop through all inputs of the playable (children in the graph)
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            // Get the child playable at this input
            Playable child = playable.GetInput(i);

            // Recursively destroy all children of this child playable
            if (child.IsValid())
            {
                DestroyPlayableRecursively(child);
            }
        }

        // Finally, destroy the playable itself
        if (playable.IsValid())
        {
            playable.Destroy();
        }
    }
}