using Doge.Events;

namespace Doge.Scenes
{
	public static class Scenes
	{
		static Scenes() {
			LoadFinished = new ActionEvent();
		}

		public static ActionEvent LoadFinished { get; private set; }
	}
}
