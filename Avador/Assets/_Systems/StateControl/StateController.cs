using System;
using UnityEngine;

public class StateController : MonoBehaviour
{
    [SerializeField] private BubbleControllerFloating _bubbleController;
    private StateMachine _stateMachine;
    private ContentProvider _contentProvider;

    private void Start()
    {
        _stateMachine = new StateMachine(_contentProvider, _bubbleController);
        _stateMachine.Initialize(_stateMachine.IntroState);
    }

    private void Update()
    {
        _stateMachine.Update();
        if (Input.GetKeyDown(KeyCode.End)) //DEBUG
        {
            TransitionTo(AvadorStates.FOCUS);
        }
    }

    public void TransitionTo(AvadorStates state)
    {
        switch (state)
        {
            case AvadorStates.INTRO:
                _stateMachine.TransitionTo(_stateMachine.IntroState);
                break;
            case AvadorStates.FOCUS:
                _stateMachine.TransitionTo(_stateMachine.FocusState);
                break;
            case AvadorStates.SUGGESTION:
                _stateMachine.TransitionTo(_stateMachine.SuggestionState);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
