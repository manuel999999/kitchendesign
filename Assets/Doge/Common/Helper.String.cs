namespace Doge.Common
{
    public static partial class Helper
    {
        public static float GetFloat(this string input) {
            float outReturn;
            float.TryParse(input, out outReturn);
            return outReturn;
        }

        public static double GetDouble(this string input) {
            double outReturn;
            double.TryParse(input, out outReturn);
            return outReturn;
        }

        public static int GetInt(this string input) {
            int outReturn;
            int.TryParse(input, out outReturn);
            return outReturn;
        }

        public static bool GetBool(this string input) {
            bool output;
            bool.TryParse(input.ToLower(), out output);
            return output;
        }
    }
}
