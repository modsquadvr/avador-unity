using System;

public enum AvadorStates
{
    INTRO,
    FOCUS,
    SUGGESTION
}

public interface IState
{
    public void Enter();
    public void Update();
    public void Exit();
    public AvadorStates StateEnum { get; }
}

[Serializable]
public class StateMachine
{
    public IState CurrentState { get; private set; }

    public IState IntroState;
    public IState FocusState;
    public IState SuggestionState;

    public AvadorStates PreviousStateEnum;

    public StateMachine(ContentProvider _content_provider, BubbleControllerFloating _bubble_controller)
    {
        this.IntroState = new IntroState(_content_provider, _bubble_controller);
        this.FocusState = new FocusState(_content_provider, _bubble_controller, this);
        this.SuggestionState = new SuggestionState(_content_provider);
    }
    
    public void Initialize(IState starting_state)
    {
        CurrentState = starting_state;
        starting_state.Enter();
    }
    
    public void TransitionTo(IState next_state)
    {
        PreviousStateEnum = CurrentState.StateEnum;
        CurrentState.Exit();
        CurrentState = next_state;
        next_state.Enter();
    }
    
    public void Update()
    {
        if (CurrentState != null)
        {
            CurrentState.Update();
        }
    }
}
