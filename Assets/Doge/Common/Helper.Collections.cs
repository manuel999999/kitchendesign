using System.Collections.Generic;

namespace Doge.Common
{
    public static partial class Helper
    {
        /// <summary>
        /// Clear and reinitialize list.
        /// </summary>
        public static void InitializeList<T>(ref List<T> list) {
            if(list != null) {
                list.Clear();
            }

            list = new List<T>();
        }
    }
}
