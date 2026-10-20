using Doge.Events.Types;

namespace Doge.Events
{
	public class ActionEvent
	{
		private event EventTypes.ActionEvents _event;

		public void AddListener(EventTypes.ActionEvents eventListener) {
			if(eventListener != null) {
				_event += eventListener;
			}
		}

		public void RemoveListener(EventTypes.ActionEvents eventListener) {
			if(eventListener != null) {
				_event -= eventListener;
			}
		}

		public void Raise() {
			if(_event != null) {
				_event();
			}
		}
	}
}
