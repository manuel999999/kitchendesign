using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Doge.Scenes
{
    public class SceneUIToggleBasic : SceneUIToggle<string>
    {
        [SerializeField] private Button basicToggleButton;
        [SerializeField] private string target;

        protected override UnityEvent ClickEvent => this.basicToggleButton.onClick;
        protected override string Target => this.target;
    }
}
