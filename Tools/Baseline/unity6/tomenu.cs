if (KitchenDesign.EventBridge.Designer.loadMenuScene == null) return "no loadMenuScene listener (scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + ")";
KitchenDesign.EventBridge.Designer.loadMenuScene.Invoke(null);
return "leaving designer";
