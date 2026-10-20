using Doge.Events.Types;

namespace Doge.Events
{
	public class ParameterEvent
	{
		private event EventTypes.ParameterEvents Event;

		public void AddListener(EventTypes.ParameterEvents parameterEventListener) {
			if(parameterEventListener != null) {
				Event += parameterEventListener;
			}
		}

		public void RemoveListener(EventTypes.ParameterEvents parameterEventListener) {
			if(parameterEventListener != null) {
				Event -= parameterEventListener;
			}
		}

		public void Raise(IDogeParam param) {
			if(Event != null && param != null) {
				Event(param);
			}
		}

		public void Raise() {
            Event?.Invoke(new EmptyDogeParam());
        }
    }
}
