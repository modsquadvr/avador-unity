using UnityEngine;

public class FocusState: IState
{
	private ContentProvider _contentProvider;
	private BubbleControllerFloating _bubbleController;
	private StateMachine _stateMachine;

	public AvadorStates StateEnum { get; } = AvadorStates.FOCUS;
	public FocusState(ContentProvider content_provider, BubbleControllerFloating bubble_controller, StateMachine state_machine)
	{
		_contentProvider = content_provider;
		_bubbleController = bubble_controller;
		_stateMachine = state_machine;
	}

	public void Enter()
	{
		_bubbleController.SelectBubble(_contentProvider.CurrentObjectId);
		if (_stateMachine.PreviousStateEnum != AvadorStates.FOCUS) _bubbleController._sealAnimationRunner.PlaySwimUp();
	}

	public void Update()
	{
	}

	public void Exit()
	{
	}
	
}
