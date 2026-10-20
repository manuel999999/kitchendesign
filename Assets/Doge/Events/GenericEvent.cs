using Doge.Events.Types;

namespace Doge.Events
{
    public class GenericEvent<T>
    {
        private event EventTypes.GenericEvents<T> Event;

		public void AddListener(EventTypes.GenericEvents<T> genericEventListener) {
			if(genericEventListener != null) {
				Event += genericEventListener;
			}
		}

		public void RemoveListener(EventTypes.GenericEvents<T> genericEventListener) {
			if(genericEventListener != null) {
				Event -= genericEventListener;
			}
		}

		public void Raise(T param) {
			if(Event != null && param != null) {
				Event(param);
			}
		}
    }
}
