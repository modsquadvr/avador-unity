public class SuggestionState: IState
{
	private ContentProvider _contentProvider;
	public AvadorStates StateEnum { get; } = AvadorStates.SUGGESTION;
	public SuggestionState(ContentProvider content_provider)
	{
		_contentProvider = content_provider;
	}

	public void Enter()
	{
	}

	public void Update()
	{
	}

	public void Exit()
	{
	}
}
