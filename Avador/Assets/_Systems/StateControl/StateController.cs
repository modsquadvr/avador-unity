using System;
using UnityEngine;

public class StateController : MonoBehaviour
{
    [SerializeField] private BubbleControllerFloating _bubbleController;
    [SerializeField] private ContentProvider _contentProvider;
    [SerializeField] private float _timeToGoBackToResting = 120f;
    private StateMachine _stateMachine;
    private float _timeSinceLastResponse;

    private void Start()
    {
        _stateMachine = new StateMachine(_contentProvider, _bubbleController);
        _stateMachine.Initialize(_stateMachine.IntroState);

        RealtimeClient.Instance.OnItemSelected += ItemSelected;
        RealtimeClient.Instance.OnResponseCreated += ResponseCreated;
        RealtimeClient.Instance.OnReturnToBubbles += ReturnToIntroState;
    }

    private void OnDestroy()
    {
        RealtimeClient.Instance.OnItemSelected -= ItemSelected;
        RealtimeClient.Instance.OnResponseCreated -= ResponseCreated;
        RealtimeClient.Instance.OnReturnToBubbles -= ReturnToIntroState;
    }

    private void Update()
    {
        if (_stateMachine.CurrentState.StateEnum != AvadorStates.INTRO)
        {
            _timeSinceLastResponse += Time.deltaTime;
            if (_timeSinceLastResponse > _timeToGoBackToResting)
                TransitionTo(AvadorStates.INTRO);
        }
        
        _stateMachine.Update();
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)))
            {
                ItemSelected(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.End))
        {
            ReturnToIntroState();
        }
    }

    private void ReturnToIntroState()
    {
        TransitionTo(AvadorStates.INTRO);
    }

    private void ItemSelected(int id)
    {
        _contentProvider.CurrentObjectId = id;
        TransitionTo(AvadorStates.FOCUS);
    }
    private void ResponseCreated()
    {
        _timeSinceLastResponse = 0f;
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
