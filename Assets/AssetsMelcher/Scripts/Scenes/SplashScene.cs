namespace KitchenDesign
{
    using Melcher;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Video;
    using DG.Tweening;
    using MEC;

    /// <summary>
    /// Plays our splash movie and then loads the next scene.
    /// </summary>
    public class SplashScene : MelcherStateMachineState
    {
        // ====================================================================
        // Methods
        // ====================================================================

        public override void OnStateAdded(IMelcherStateMachineState fromState)
        {
            try
            {
                string videoStreamingAssetsPath =
                    MelcherTransformFilePath.From(m_VideoAsset.dataRelativePath, relativeTo: MelcherPredefinedFilePath.data)
                        .To(Application.streamingAssetsPath);

                MelcherLog.Log("StreamingPath: " + videoStreamingAssetsPath, severity: MelcherLogSeverity.Verbose);

                if (Application.platform != RuntimePlatform.Android &&
                    Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    videoStreamingAssetsPath = "file://" + videoStreamingAssetsPath;
                }

                m_VideoPlayer.url = videoStreamingAssetsPath;
                m_VideoPlayer.Prepare();

                // Firefox doesn't always recieve a prepareCompleted or
                // errorReceived event. In those cases, we will just skip
                // the video and jump straight to the game
                Timing.CallDelayed(
                    delay: 2.0f,
                    action: () =>
                    {
                        if (!m_VideoPlayer.isPrepared)
                        {
                            m_VideoPlayer.enabled = false;
                            LoadNextScene();
                        }
                    }
                );
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);

#if !UNITY_EDITOR
                LoadNextScene();
#endif  // if !UNITY_EDITOR
            }
        }

        private void Awake()
        {
            m_WhiteOut.color = m_WhiteOut.color.WithAlpha(0.0f);
        }

        private void OnEnable()
        {
            m_VideoPlayer.prepareCompleted += OnVideoPrepared;
            m_VideoPlayer.loopPointReached += OnVideoFinished;
            m_VideoPlayer.errorReceived += OnVideoError;
        }

        private void OnDisable()
        {
            m_VideoPlayer.prepareCompleted -= OnVideoPrepared;
            m_VideoPlayer.loopPointReached -= OnVideoFinished;
            m_VideoPlayer.errorReceived -= OnVideoError;
        }

        /// <summary>
        /// Start the video as soon as it is loaded.
        /// </summary>
        private void OnVideoPrepared(VideoPlayer source)
        {
            m_VideoPlayer.prepareCompleted -= OnVideoPrepared;

            // Do nothing when the video player is disabled because that means
            // the video preparing timed out
            if (m_VideoPlayer.enabled)
            {
                StartSplashMovie();
            }
        }

        /// <summary>
        /// Fade to white for scene transition.
        /// </summary>
        private void OnVideoFinished(VideoPlayer source)
        {
            m_WhiteOut.DOFade(endValue: 1.0f, duration: 2.0f)
                .SetDelay(1.4f)
                .SetEase(Ease.Linear)
                .OnComplete(
                    LoadNextScene
                );
        }

        /// <summary>
        /// Ensure the next scene starts if there are any errors playing the
        /// video.
        /// </summary>
        private void OnVideoError(VideoPlayer source, string message)
        {
            MelcherLog.LogError(
                message,
                context: this
            );

            LoadNextScene();
        }

        /// <summary>
        /// Fade the camera's background color to match the video's background
        /// color. When the fade finishes, start the video.
        /// </summary>
        private void StartSplashMovie()
        {
            DOTween.To(
                getter: () => m_Camera.backgroundColor,
                setter: (c) => m_Camera.backgroundColor = c,
                endValue: m_VideoBackgroundColor,
                duration: 1.0f
            ).OnComplete(() => m_VideoPlayer.Play());
        }

        /// <summary>
        /// Load the next scene.
        /// </summary>
        private void LoadNextScene()
        {
            if (m_SceneLoadOperation == null)
            {
                int nextSceneIndex = gameObject.scene.buildIndex + 1;

                m_SceneLoadOperation =
                    MelcherSceneManager.LoadSceneAsync(
                        MelcherSceneRequest.FromBuildIndex(nextSceneIndex),
                        MelcherSceneLoadMode.Single
                    );
            }
        }

        // ====================================================================
        // Inspector-serialized members
        // ====================================================================

        [Tooltip("The video file to play. This is referenced via a " +
            "WeakReference to prevent it from being loaded immediately.")]
        [SerializeField] private MelcherPathField<Object> m_VideoAsset = null;
        [SerializeField] private Color m_VideoBackgroundColor = Color.black;
        [SerializeField] protected Image m_WhiteOut = null;

        // ====================================================================
        // OnValidate-serialized members
        // ====================================================================

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Scene)]
        private Camera m_Camera = null;

        [SerializeField, MelcherReadOnlyInInspector, MelcherFindComponent(MelcherFindComponentMode.Down)]
        private VideoPlayer m_VideoPlayer = null;

        // ====================================================================
        // Non-serialized members
        // ====================================================================

        private AsyncOperation m_SceneLoadOperation;

    }   // class SplashScene

}   // namespace KitchenDesign
