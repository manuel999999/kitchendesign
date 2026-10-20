using UnityEngine;
using Doge.Events;

namespace Doge.Messaging
{
    public abstract class MessagingAgentManager : MonoBehaviour
    {
        [Tooltip("Messaging Agent Prefab")]
        [SerializeField] protected MessagingAgent messageBox;
        [Tooltip("Messaging Agent UI Container")]
        [SerializeField] protected RectTransform container;

        [SerializeField] protected MessageGroup messageGroup;

        protected virtual void Awake() {
            Message.Send.AddListener(ReceiveMessage);
        }

        protected virtual void OnDestroy() {
            Message.Send.RemoveListener(ReceiveMessage);
        }

        protected virtual MessagingAgent GetAgent() {
            if(this.messageBox == null) {
                Debug.Log($"Cannot Find Available Message Agent... {gameObject.name}", this);
                return null;
            }
            
            // instantiate instead of pooling fixes hierachy issues
            // in other words, new messages are always on top
            var agent = Instantiate(messageBox, container);
            
            return agent;
        }

        protected virtual MessagingAgent GetAgent(ParamMessage p) {
            if(p == null) {
                return null;
            }

            var agent = GetAgent();
            if(agent == null) {
                Debug.Log("Message Agent Missing...");
                return null;
            }

            agent.AutoClose = p.AutoClose;
            agent.Tag = p.Tag;
            agent.ButtonTexts = p.ButtonTexts;

            return agent;
        }

        public abstract void ReceiveMessage(IDogeParam param);
    }
}