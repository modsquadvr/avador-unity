using System;
using UnityEngine;

public class StateController : MonoBehaviour
{
    [SerializeField] private BubbleControllerFloating _bubbleController;
    [SerializeField] private ContentProvider _contentProvider;
    private StateMachine _stateMachine;

    private void Start()
    {
        _stateMachine = new StateMachine(_contentProvider, _bubbleController);
        _stateMachine.Initialize(_stateMachine.IntroState);

        RealtimeClient.Instance.OnItemSelected += ItemSelected;
    }

    private void Update()
    {
        _stateMachine.Update();
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
            {
                _contentProvider.CurrentObjectId = i;
                TransitionTo(AvadorStates.FOCUS);
            }
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            TransitionTo(AvadorStates.INTRO);
        }
    }

    private void ItemSelected(int id)
    {
        _contentProvider.CurrentObjectId = id;
        TransitionTo(AvadorStates.FOCUS);
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
