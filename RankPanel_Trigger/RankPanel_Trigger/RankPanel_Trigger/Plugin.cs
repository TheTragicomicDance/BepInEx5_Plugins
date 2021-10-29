using BepInEx;
using HarmonyLib;
using JX_Plugin;
using JyGame;
using UnityEngine;
using UnityEngine.UI;

namespace RankPanel_Trigger
{
    [HarmonyPatch(typeof(RoleStatePanelUI), "Refresh")]
    class RoleStatePanelUI_Refresh_Patch
    {
        public static bool Prefix(RoleStatePanelUI __instance)
        {
            if (!__instance.transform.FindChild("HardIcon").GetComponent<Button>())
            {
                __instance.transform.FindChild("HardIcon").gameObject.AddComponent<Button>();
            }
            __instance.transform.FindChild("HardIcon").GetComponent<Button>().onClick.RemoveAllListeners();
            __instance.transform.FindChild("HardIcon").GetComponent<Button>().onClick.AddListener(() =>
            {
                GameObject go = (GameObject)GameObject.Instantiate(BundleLoader.BundleLoader.objects["RankPanel"]); //生成面板
                go.transform.SetParent(GameObject.Find("MapRoot/Canvas").transform);
                go.name = "RankPanel";
                go.SetActive(true);
                go.GetComponent<RankPanel>().SetContent("排行榜", LuaManager.Call<string>("RankPanel_Content", new object[0]));
            });
            return true;
        }
    }
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.EasternDay.BundleLoader")]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony harmony = new("JX_Decode_Patch");
        private void Awake()
        {
            //Hook所有代码
            harmony.PatchAll();
            // 控制台提示语
            Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 成功Hook代码!");
        }
    }
}
