using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Doge.Scenes
{
    public class AdditiveSceneManager : MonoBehaviour
	{
        [Tooltip("Optional: Progress bar")]
        [SerializeField] protected Slider progressReport;

        [Tooltip("Initially fill the progress bar while loading the scene..")]
        [SerializeField] protected bool advanceProgressReport;
        
        [Tooltip("Optional: Loading label")]
        [SerializeField] protected TextMeshProUGUI textReport;

		[SerializeField] protected AdditiveScene[] scenes;

        [Tooltip("Load scenes on Start..")]
        [SerializeField] protected bool autoStart;

        protected void Start() {
            if(this.autoStart) {
                LoadScenes();
            }
        }

        public void LoadScenes() {
            StartCoroutine(LoadScenesAsync());
        }

        protected virtual IEnumerator LoadScenesAsync() {
            if(this.scenes == null || this.scenes.Length == 0) {
                yield break;
            }

            if(this.progressReport != null) {
                this.progressReport.value = 0;
                this.progressReport.maxValue = this.scenes.Length;
            }

            foreach(var scene in this.scenes) {
                if(scene.Trigger) {
                    if(this.textReport !=null) {
                        this.textReport.text = $"{scene.LoadingText}...";
                    }

                    if(this.advanceProgressReport) {
                        IncrementProgress();
                    }

                    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.SceneName, LoadSceneMode.Additive);

                    while (asyncLoad.isDone == false)
                    {
                        yield return null;
                    }
                }

                if(this.advanceProgressReport == false) {
                    IncrementProgress();
                }
            }

            Debug.Log("Scenes Loading Finished");
            Scenes.LoadFinished.Raise();
        }

        protected void IncrementProgress() {
            if(this.progressReport != null) {
                this.progressReport.value += 1;
            }  
        }
	}
}
