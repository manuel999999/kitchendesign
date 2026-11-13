var ms = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.MenuNavigationControl>();
if (ms == null) return "step=state scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " no MenuNavigationControl";
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object F(string n) { return typeof(KitchenDesign.MenuNavigationControl).GetField(n, bf).GetValue(ms); }
string Top() { foreach (var h in (System.Collections.IEnumerable)F("m_MenuHistory")) { return ((UnityEngine.Object)h).name; } return "none"; }
bool Up(string n) { return ((Melcher.MelcherUI)F(n)).IsVisible(); }
string step = "state";
if (step == "home") { KitchenDesign.Events.EventSession.OnHomeRequested.Raise(); }
else if (step == "clients") { KitchenDesign.Events.EventMenu.OnClientListRequested.Raise(); }
else if (step == "quotes") { UnityEngine.Object.FindFirstObjectByType<KitchenDesign.AppInitializer>().SystemsForEditor.ClientList.ActiveClientId = 10191; KitchenDesign.Events.EventMenu.OnClientQuoteListRequested.Raise(); }
else if (step == "clientinfo") { KitchenDesign.Events.EventMenu.OnClientInfoRequested.Raise(new KitchenDesign.Events.ClientInfo(10191)); }
else if (step == "quoteinfo") { KitchenDesign.Events.EventMenu.OnQuoteInfoRequested.Raise(new KitchenDesign.Events.ClientQuoteInfo(10191, 27755)); }
else if (step == "quoteinfo_close") { ((KitchenDesign.UI.MenuScreens.QuoteInfoUI)F("m_QuoteInfoUi")).Hide(); }
else if (step == "profile") { KitchenDesign.Events.EventMenu.OnUserProfileRequested.Raise(); }
else if (step == "logout") { KitchenDesign.Events.EventMenu.OnLogOutPromptRequested.Raise(); }
else if (step == "logout_close") { ((KitchenDesign.UI.MenuScreens.UserLogOutPromptUI)F("m_UserLogOutPromptUi")).Hide(); }
else if (step != "state") { return "unknown step " + step; }
return "step=" + step + " top=" + Top() + " home=" + Up("m_HomeUi") + " clients=" + Up("m_ClientListUi") + " quotes=" + Up("m_ClientQuoteListUi") + " clientinfo=" + Up("m_ClientProfileUi") + " quoteinfo=" + Up("m_QuoteInfoUi") + " profile=" + Up("m_UserProfileUi") + " logout=" + Up("m_UserLogOutPromptUi");
