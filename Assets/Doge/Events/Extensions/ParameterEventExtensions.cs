namespace Doge.Events
{
    public static partial class ParameterEventExtensions
    {
        /// <summary>
        /// Creates a StringEventParameter for your string input.
        /// </summary>
        public static void Raise(this ParameterEvent e, string value) {
            e.Raise(new ParamString(value));
        }

        /// <summary>
        /// Creates a IntEventParameter for your int input.
        /// </summary>
        public static void Raise(this ParameterEvent e, int value) {
            e.Raise(new ParamInt(value));
        }

        /// <summary>
        /// Creates a FloatEventParameter for your float input.
        /// </summary>
        public static void Raise(this ParameterEvent e, float value) {
            e.Raise(new ParamFloat(value));
        }

        /// <summary>
        /// Creates a BoolEventParameter for your bool input.
        /// </summary>
        public static void Raise(this ParameterEvent e, bool value) {
            e.Raise(new ParamBool(value));
        }

        /// <summary>
        /// Creates a generic EventParameter for your type input.
        /// </summary>
        public static void Raise<T>(this ParameterEvent e, T value) {
            e.Raise(new DogeParam<T>(value));
        }
    }
}
