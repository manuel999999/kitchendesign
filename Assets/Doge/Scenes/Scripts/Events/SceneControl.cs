using Doge.Events;

namespace Doge.Scenes
{
    public static class SceneControl
	{
		static SceneControl() {
			Activate = new ParameterEvent();
			Deactivate = new ParameterEvent();
			Toggle = new ParameterEvent();
		}

		public static ParameterEvent Activate { get; private set; }
		public static ParameterEvent Deactivate { get; private set; }
		public static ParameterEvent Toggle { get; private set; }
	}
}
