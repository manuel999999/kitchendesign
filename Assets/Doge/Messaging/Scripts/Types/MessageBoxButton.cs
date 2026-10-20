using UnityEngine.UI;
using TMPro;

namespace Doge.Messaging
{
	[System.Serializable]
	public class MessageBoxButton
	{
		public Button button;
		public TextMeshProUGUI text;
		public MessageResponse response;
	}
}
