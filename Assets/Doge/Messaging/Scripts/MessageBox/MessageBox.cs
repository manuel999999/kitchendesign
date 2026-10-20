using UnityEngine;
using TMPro;

namespace Doge.Messaging
{
    public class MessageBox : MessagingAgent
    {
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected TextMeshProUGUI messageText;
        [SerializeField] protected MessageBoxButton[] buttons;

        public override void UpdateDisplay(string id, string title, string message)
        {
            if(_called == false) {
                _called = true;
            }
            
            this._id = id;
            this.titleText.text = title;
            this.messageText.text = message;
            SetupButtons();
            gameObject.SetActive(true);

            OnMessageBoxReady?.Invoke();
        }

        protected void SetupButtons() {
            if(this.buttons.Length == 0) {
                return;
            }

            for(int i = 0; i < this.buttons.Length; i++) {
                SetButtonEvents(this.buttons[i]);
                SetButtonTexts(this.buttons[i]);
            }
        }

        protected void SetButtonEvents(MessageBoxButton button) {
            button.button.onClick.AddListener(() => { Callback(button.response); });
            button.button.onClick.AddListener(ConditionalClose);
        }

        protected void SetButtonTexts(MessageBoxButton button) {
            if(ButtonTexts.ContainsKey(button.response)) {
                button.text.text = ButtonTexts[button.response];
            }
        }
    }
}
