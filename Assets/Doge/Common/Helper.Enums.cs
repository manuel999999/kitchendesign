using System;

namespace Doge.Common
{
    public static partial class Helper
	{
		public static T FindEnumByString<T>(string input) where T : System.Enum {
			if (!typeof(T).IsEnum)  {
				throw new ArgumentException("Static class Doge.Helper.FindEnumByString T must be an enumerated type.");
			}

        	return (T)Enum.Parse(typeof(T), input, true);
		}

		public static T GetEnum<T>(this string input) where T : System.Enum {
			if (!typeof(T).IsEnum)  {
				throw new ArgumentException("Static class Doge.Helper.GetEnum T must be an enumerated type.");
			}

        	return (T)Enum.Parse(typeof(T), input, true);
		}
	}
}
