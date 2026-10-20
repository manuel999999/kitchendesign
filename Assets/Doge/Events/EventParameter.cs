namespace Doge.Events
{
	public interface IDogeParam { }

    public class EmptyDogeParam : IDogeParam { }

    public class DogeParam<T> : IDogeParam {
        public DogeParam(T value) {
            this.Value = value;
        }

        public T Value { get; }
    }

	public class ParamString : IDogeParam {
        public ParamString(string input) {
            this.Value = input;
        }

        public string Value { get; }
    }

    public class ParamInt : IDogeParam {
        public ParamInt(int input) {
            this.Value = input;
        }

        public int Value { get; }
    }

    public class ParamFloat : IDogeParam {
        public ParamFloat(float input) {
            this.Value = input;
        }

        public float Value { get; }
    }

    public class ParamBool : IDogeParam {
        public ParamBool(bool input) {
            this.Value = input;
        }

        public bool Value { get; }
    }
}
