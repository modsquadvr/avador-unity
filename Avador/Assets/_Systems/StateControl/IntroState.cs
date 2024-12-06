public class IntroState : IState
{
    private ContentProvider _contentProvider;
    private BubbleControllerFloating _bubbleController;

    public IntroState(ContentProvider content_provider, BubbleControllerFloating bubble_controller)
    {
        _contentProvider = content_provider;
        _bubbleController = bubble_controller;
    }


    public void Enter()
    {
        _bubbleController.EnterBubbleProducingState();
    }

    public void Update()
    {
        _bubbleController.UpdateBubbles();
    }

    public void Exit()
    {
    }
}
