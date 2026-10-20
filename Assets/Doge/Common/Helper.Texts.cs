using UnityEngine.UI;
using TMPro;

namespace Doge.Common
{
	public static partial class Helper
	{
		public static void SafeText(this Text text, string input) {
			if(text != null) {
				text.text = input;
			}
		}

		public static void SafeText(this TextMeshPro text, string input) {
			if(text != null) {
				text.text = input;
			}
		}

		public static void SafeText(this TextMeshProUGUI text, string input) {
			if(text != null) {
				text.text = input;
			}
		}
	}
}
