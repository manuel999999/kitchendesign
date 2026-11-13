if (UnityEngine.Object.FindFirstObjectByType<KitchenDesign.DesignerScreensControl>() == null) return "no designer scene (scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ")";
KitchenDesign.Events.EventSession.OnMenuSceneRequested.Raise(new KitchenDesign.Events.MenuSceneRequestInfo(null));
