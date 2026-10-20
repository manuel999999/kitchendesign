using Doge.Events;

namespace Doge.Messaging
{
	public static class Message
	{
		static Message() {
			Send = new ParameterEvent();
			ExitAll = new ActionEvent();
			ExitByID = new ParameterEvent();
			ExitByTag = new ParameterEvent();
		}

		public static ParameterEvent Send { get; private set; }
		public static ActionEvent ExitAll { get; private set; }
		public static ParameterEvent ExitByID { get; private set; }
		public static ParameterEvent ExitByTag { get; private set; }
	}
}
