var ms = UnityEngine.Object.FindFirstObjectByType<KitchenDesign.MenuScene>();
if (ms == null) return "step=state scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " no MenuScene";
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
object F(string n) { return typeof(KitchenDesign.MenuScene).GetField(n, bf).GetValue(ms); }
string Top() { foreach (var h in (System.Collections.IEnumerable)F("m_MenuHistory")) { return ((UnityEngine.Object)h).name; } return "none"; }
bool Up(string n) { return ((Melcher.MelcherUI)F(n)).IsVisible(); }
string step = "state";
if (step == "home") { KitchenDesign.EventBridge.Menu.showHomeUi?.Invoke(); }
else if (step == "clients") { KitchenDesign.EventBridge.Menu.showClientListUi?.Invoke(); }
else if (step == "quotes") { KitchenDesign.ProFitGlobals.activeClientId = 10191; KitchenDesign.EventBridge.Menu.pushClientQuoteListUi?.Invoke(); }
else if (step == "clientinfo") { KitchenDesign.EventBridge.Menu.pushClientInfoUi?.Invoke(10191); }
else if (step == "quoteinfo") { KitchenDesign.EventBridge.Menu.showQuoteInfoUi?.Invoke(10191, 27755); }
else if (step == "quoteinfo_close") { ((KitchenDesign.QuoteInfoUI)F("m_QuoteInfoUi")).Hide(); }
else if (step == "profile") { KitchenDesign.EventBridge.Menu.showUserProfileUi?.Invoke(); }
else if (step == "logout") { KitchenDesign.EventBridge.Menu.showUserLogOutPrompt?.Invoke(); }
else if (step == "logout_close") { ((KitchenDesign.UserLogOutPromptUI)F("m_UserLogOutPromptUi")).Hide(); }
else if (step != "state") { return "unknown step " + step; }
return "step=" + step + " top=" + Top() + " home=" + Up("m_HomeUi") + " clients=" + Up("m_ClientListUi") + " quotes=" + Up("m_ClientQuoteListUi") + " clientinfo=" + Up("m_ClientProfileUi") + " quoteinfo=" + Up("m_QuoteInfoUi") + " profile=" + Up("m_UserProfileUi") + " logout=" + Up("m_UserLogOutPromptUi");
