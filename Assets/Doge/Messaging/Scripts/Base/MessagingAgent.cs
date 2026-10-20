using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Doge.Events;

namespace Doge.Messaging
{
    public abstract class MessagingAgent : MonoBehaviour
    {
        protected bool _called;
        protected string _id;
        public bool AutoClose { get; set; }
        public string Tag { get; set; }
        public UnityAction OnMessageBoxReady { get; set; }
        public UnityAction<MessageResponse> Callback { get; set; }
        public Dictionary<MessageResponse, string> ButtonTexts;

        protected void Awake() {
            AutoClose = true;
            ButtonTexts = new Dictionary<MessageResponse, string>();
        }

        protected virtual void Start() {
            if(_called == false) {
                gameObject.SetActive(false);
            }

            Message.ExitAll.AddListener(Close);
            Message.ExitByID.AddListener(CloseByID);
            Message.ExitByTag.AddListener(CloseByTag);
        }

        protected virtual void OnDestroy() {
            Message.ExitAll.RemoveListener(Close);
            Message.ExitByID.RemoveListener(CloseByID);
            Message.ExitByTag.RemoveListener(CloseByTag);
        }

        protected virtual void Close() {
            Destroy(gameObject);
        }

        protected virtual void ConditionalClose() {
            if(this.AutoClose) {
                Close();
            }
        }

        protected virtual void CloseByID(IDogeParam param) {
            if(param is ParamString) {
                var p = param as ParamString;

                if(p.Value == _id) {
                    Close();
                }
            }
        }

        protected virtual void CloseByTag(IDogeParam param) {
            if(param is ParamString) {
                var p = param as ParamString;

                if(p.Value == this.Tag) {
                    Close();
                }
            }
        }

        public abstract void UpdateDisplay(string id, string title, string message);
    }
}