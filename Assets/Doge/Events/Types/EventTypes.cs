namespace Doge.Events.Types
{
	public class EventTypes
	{
		public delegate void ActionEvents();
		public delegate void ParameterEvents(IDogeParam param);
		public delegate void GenericEvents<T>(T param);
	}
}
