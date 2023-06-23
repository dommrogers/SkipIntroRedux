using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppTLD.News;
using MelonLoader;
using System.Text.RegularExpressions;

namespace SkipIntroRedux;

[HarmonyPatch]
internal static class Patches
{
	private static bool BootUpdateRunning { get; set; }
	private static bool hasCrunched = false;


	private static string _fileURL = "https://raw.githubusercontent.com/dommrogers/SkipIntroRedux/master/lines.txt";
	public static string GetText()
	{
		string fileContent = new System.Net.WebClient().DownloadString(_fileURL);
		string[] lines = Regex.Split(fileContent.Trim(), "\r\n|\r|\n");
		return lines[UnityEngine.Random.Range(0, lines.Length)];
	}

	[HarmonyPatch(typeof(Localization), nameof(Localization.Get), new Type[] { typeof(string) })]
	internal static class Localization_Get
	{
		internal static void Postfix(string key, ref string __result)
		{
			if (hasCrunched)
			{
				return;
			}

			if (key == "GAMEPLAY_DevMessageCrunch")
			{
				string timeofday = DateTime.Now.ToString();
				__result = GetText();

				hasCrunched = true;
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BootUpdate), nameof(BootUpdate.Start))]
	public static void BootUpdateIsRunning(BootUpdate __instance)
	{
		__instance.m_BootState = BootUpdate.BootState.Idle;
		BootUpdateRunning = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BootUpdate), nameof(BootUpdate.Update))]
	public static void CheckForLoadingMainMenu(BootUpdate __instance)
	{
		if (__instance.m_BootState == BootUpdate.BootState.LoadingMainMenu)
		{
			BootUpdateRunning = false;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(InputManager), nameof(InputManager.AnyButtonsOrKeysPressed))]
	[HarmonyPatch(typeof(InputManager), nameof(InputManager.CheckForActiveController))]
	public static void FakeKeyPressToLoadTheMainMenu(ref bool __result)
	{
		if (BootUpdateRunning)
		{
			__result = true;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Panel_MainMenu), nameof(Panel_MainMenu.Enable))]
	public static void SkipIntroVideo()
	{
		MoviePlayer.m_HasIntroPlayedForMainMenu = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Panel_Sandbox.GameEditionArt), nameof(Panel_Sandbox.GameEditionArt.SetActive))]
	private static void DisableUpdateTitle(Panel_Sandbox.GameEditionArt __instance)
	{
		//This method is deduplicated, so other methods are also getting patched by this.
		//We have to check that our object type is correct.
		if (__instance.GetIl2CppType() == Il2CppType.Of<Panel_Sandbox.GameEditionArt>())
		{
			if (__instance.m_UpdateTitle != null)
			{
				__instance.m_UpdateTitle.SetActive(false);
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NewsCarousel), nameof(NewsCarousel.Awake))]
	private static void DisableNews(NewsCarousel __instance)
	{
		__instance.transform.parent.gameObject.SetActive(false);
	}
}
