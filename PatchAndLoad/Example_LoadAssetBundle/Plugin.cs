using BepInEx;
using BepInEx.Configuration;
using JyGame;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LoadAssetBundle
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        private static GameObject RankPanel;
        private ConfigEntry<string> rankBundlePath;
        private void Awake()
        {
            // Plugin startup logic
            this.rankBundlePath = base.Config.Bind<string>("General", "rankBundlePath", string.Format("{0}/{1}/{2}", CommonSettings.persistentDataPath, "组件", "rankpanel.ab"), "组件Bundle的保存路径");
            base.Logger.LogInfo("插件 JX_Rank_Component 已载入!");
        }


        private void Start()
        {
            AssetBundle assetBundle = AssetBundle.CreateFromFile(this.rankBundlePath.Value);
            RankPanel = UnityEngine.Object.Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>("RankingPanel"));
            RankPanel.transform.SetParent(GameObject.Find("MapRoot/Canvas").transform);
            assetBundle.Unload(false);
        }
    }
}
