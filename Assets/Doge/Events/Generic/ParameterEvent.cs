using System;

namespace Doge.Events.Generic
{
    public class ParameterEvent<T>
    {
        private event Action<T> _event;

		public void AddListener(Action<T> parameterEventListener) {
			if(parameterEventListener != null) {
				_event += parameterEventListener;
			}
		}

		public void RemoveListener(Action<T> parameterEventListener) {
			if(parameterEventListener != null) {
				_event -= parameterEventListener;
			}
		}

		public void Raise(T param) {
			if(_event != null && param != null) {
				_event(param);
			}
		}
    }
}
