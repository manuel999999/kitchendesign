using System.Collections.Generic;
using UnityEngine.Events;
using Doge.Events;

namespace Doge.Messaging
{
	public class ParamMessage : IDogeParam {
        public ParamMessage(MessageGroup messageType = MessageGroup.Default) :
        this ("Message Box", "Hello", messageType) { }

        public ParamMessage(string title, string message) :
        this (title, message, (x) => { return; }, MessageGroup.Default) { }

        public ParamMessage(string title, string message, MessageGroup messageGroup) :
        this (title, message, (x) => { return; }, messageGroup) { }

        public ParamMessage(string title, string message, UnityAction<MessageResponse> callback, MessageGroup messageGroup = MessageGroup.Default) {
            this.ID = Constants.UNTAGGED;
            this.Title = title;
            this.Message = message;
            this.ButtonTexts = new Dictionary<MessageResponse, string>();
            this.AutoClose = true;
            this.Tag = Constants.UNTAGGED;
            this.MessageGroup = messageGroup;
            this.Callback = callback;
        }

        public string ID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public Dictionary<MessageResponse, string> ButtonTexts { get; set; }
        public bool AutoClose { get; set; }
        public string Tag { get; set; }
        public MessageGroup MessageGroup { get; set; }
        public UnityAction<MessageResponse> Callback { get; set; }
    }
}
