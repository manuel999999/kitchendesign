int ci, qi;
if (!KitchenDesign.ProFitGlobals.TryGetClientAndQuoteIndex(10191, 27755, out ci, out qi)) return "not found";
KitchenDesign.ProFitGlobals.activeClientId = 10191;
KitchenDesign.ProFitGlobals.activeQuoteId = 27755;
Melcher.MelcherSceneManager.LoadSceneAsync(Melcher.MelcherSceneRequest.FromName("DesignerScene"), Melcher.MelcherSceneLoadMode.Single);
return "loading quote 27755";
