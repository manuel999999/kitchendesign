namespace Doge.Events
{
    public static class ParameterExtensions
	{
		public static bool TryParse<T>(this IDogeParam param, out T output) where T: IDogeParam {
			if(param is not null and T) {
				output = (T)param;
                return true;
            } else {
                output = default;
                return false;
			}
		}
	}
}
