public class FocusState: IState
{
	private ContentProvider _contentProvider;
	private BubbleControllerFloating _bubbleController;

	public FocusState(ContentProvider content_provider, BubbleControllerFloating bubble_controller)
	{
		_contentProvider = content_provider;
		_bubbleController = bubble_controller;
	}

	public void Enter()
	{
		_bubbleController.SelectBubble();
	}

	public void Update()
	{
	}

	public void Exit()
	{
	}
}
