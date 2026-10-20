using Doge.Events;

namespace Doge.Messaging
{
    public class MessageBoxManager : MessagingAgentManager
    {
        public override void ReceiveMessage(IDogeParam param) {
            if(param.TryParse(out ParamMessage p) && p.MessageGroup == this.messageGroup) {
                var agent = GetAgent(p);
                
                if(agent != null) {
                    agent.Callback = p.Callback;
                    agent.UpdateDisplay(p.ID, p.Title, p.Message);
                }
            }
        }
    }
}