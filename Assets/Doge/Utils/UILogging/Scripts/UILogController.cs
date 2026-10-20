using System.Text;
using UnityEngine;
using Doge.Events;
using TMPro;

namespace Doge.Utils.UILogging
{
    public class UILogController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private bool appendLogs;

        private StringBuilder _logRecord;
        private int _logLine;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void Awake() {
            UILogger.Write.AddListener(ReceiveLog);
        }

        private void OnDestroy() {
            UILogger.Write.RemoveListener(ReceiveLog);
        }
#endif
        private void ReceiveLog(IDogeParam param) {
            if(param.TryParse(out ParamString p)) {
                _logLine++;
                if(this.appendLogs) { // append all logs together
                    if(_logRecord == null) {
                        _logRecord = new StringBuilder();
                    }

                    _logRecord.AppendLine($"{_logLine.ToString()} {p.Value}");

                    this.logText.text = _logRecord.ToString();
                } else {              // log once
                    this.logText.text = $"{_logLine} {p.Value}";
                }
            }
        }
    }
}
