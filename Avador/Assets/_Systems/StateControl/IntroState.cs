public class IntroState : IState
{
    private ContentProvider _contentProvider;
    private BubbleControllerFloating _bubbleController;
    private StateMachine _stateMachine;
    
    public AvadorStates StateEnum { get; } = AvadorStates.INTRO;

    public IntroState(ContentProvider content_provider, BubbleControllerFloating bubble_controller,
        StateMachine state_machine)
    {
        _contentProvider = content_provider;
        _bubbleController = bubble_controller;
        _stateMachine = state_machine;
    }


    public void Enter()
    {
        _bubbleController.EnterBubbleProducingState();
        if (_stateMachine.PreviousStateEnum != AvadorStates.INTRO) _bubbleController._sealAnimationRunner.PlaySwimDown();
    }

    public void Update()
    {
        _bubbleController.UpdateBubbles();
    }

    public void Exit()
    {
    }
}
