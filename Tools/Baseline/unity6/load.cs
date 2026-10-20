int ci, qi;
if (!Melcher.ProFitGlobals.TryGetClientAndQuoteIndex(10192, 36449, out ci, out qi)) return "not found";
Melcher.ProFitGlobals.activeClientId = 10192;
Melcher.ProFitGlobals.activeQuoteId = 36449;
Melcher.MelcherSceneManager.LoadSceneAsync(Melcher.MelcherSceneRequest.FromName("DesignerScene"), Melcher.MelcherSceneLoadMode.Single);
return "loading quote 36449";
