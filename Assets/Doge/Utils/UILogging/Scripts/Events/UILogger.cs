using Doge.Events;

namespace Doge.Utils.UILogging
{
    public static class UILogger
    {
        static UILogger() {
            Write = new ParameterEvent();
        }

        public static ParameterEvent Write { get; private set; }
    }
}
