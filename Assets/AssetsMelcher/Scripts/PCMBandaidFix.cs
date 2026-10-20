namespace Melcher
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PCMBandaidFix : MelcherMonoBehaviour
    {
        [SerializeField] private List<GameObject> _toDisable;
        [SerializeField] private List<GameObject> _toEnable;

        void Awake() => StartCoroutine(ForcedRoutine());

        private IEnumerator ForcedRoutine()
        {
            float duration = Time.time + 1.0f;

            while (Time.time < duration)
            {
                DisableObjs();
                EnableObjs();
                yield return new WaitForSeconds(0.01f);
            }
        }

        private void DisableObjs()
        {
            foreach (GameObject obj in _toDisable)
                obj.SetActive(false);
        }

        private void EnableObjs()
        {
            foreach (GameObject obj in _toEnable)
                obj.SetActive(true);
        }
    }   // class PCMBandaidFix

}   // namespace Melcher

