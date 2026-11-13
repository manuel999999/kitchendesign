namespace KitchenDesign.UI.DesignerScreens
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using DG.Tweening;

    /// <summary>
    /// Shows an indicator where the mouse cursor is when it is being clicked
    /// and moved. Used when recording video of the PC version and we want to
    /// show where interactions are happening.
    /// </summary>
    public class RecordingUI : MelcherUI
    {
        private const float TweenDuration = 0.35f;

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [Header(nameof(RecordingUI))]

        [SerializeField] private Image m_PointerFollower = null;
        [SerializeField] private Image m_ClickIndicator = null;

        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private Color m_DefaultColor;
        private Color _offColor;

        // ====================================================================
        // Methods
        // ====================================================================

        protected override void Awake()
        {
            base.Awake();

            m_DefaultColor = m_PointerFollower.color;
            _offColor = m_DefaultColor;
            _offColor.a = 0.0f;

            m_ClickIndicator.rectTransform.localScale = Vector3.zero;

            gameObject.SetActive(Application.isEditor || Debug.isDebugBuild);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            DOTween.Kill(GetTweenID());
            m_PointerFollower.rectTransform.localScale = Vector3.zero;
            m_PointerFollower.color = _offColor;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected void Update()
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RectTransform,
                Input.mousePosition,
                m_Canvas.worldCamera,
                out Vector2 localPoint
            ))
            {
                m_PointerFollower.rectTransform.anchoredPosition = localPoint;
            }

            int tweenId = GetTweenID();

            if (Input.GetMouseButtonDown(0))
            {
                DOTween.Kill(tweenId);
                m_PointerFollower.color = m_DefaultColor;
                m_PointerFollower.rectTransform.DOScale(endValue: 1.0f, TweenDuration * 0.2f)
                    .SetUpdate(isIndependentUpdate: true)
                    .SetId(tweenId);
                m_ClickIndicator.rectTransform.localScale = Vector3.zero;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                DOTween.Kill(tweenId);
                m_PointerFollower.DOColor(_offColor, TweenDuration)
                    .SetUpdate(isIndependentUpdate: true)
                    .SetId(tweenId);
                m_PointerFollower.rectTransform.DOScale(endValue: 0.0f, TweenDuration)
                    .SetUpdate(isIndependentUpdate: true)
                    .SetId(tweenId);
                m_ClickIndicator.rectTransform.DOScale(Vector3.one, TweenDuration * 0.5f)
                    .SetUpdate(isIndependentUpdate: true)
                    .SetId(tweenId);
            }
        }

        protected int GetTweenID()
        {
            return GetEntityId().GetHashCode();
        }

    }   // class RecordingUI

}   // namespace KitchenDesign.UI.DesignerScreens
