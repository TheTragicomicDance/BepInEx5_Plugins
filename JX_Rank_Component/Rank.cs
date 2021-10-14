using BepInEx;
using BepInEx.Configuration;
using JyGame;
using System.Reflection;
using UnityEngine;

namespace JX_Rank_Component
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Rank : BaseUnityPlugin
    {
        //配置文件
        private ConfigEntry<string> rankBundlePath;//AssetBundle所在路径
        private ConfigEntry<KeyboardShortcut> hotKey { get; set; }//解密快捷键

        //面板Obj
        static GameObject RankPanel;

        private void Awake()
        {
            //加载配置文件
            rankBundlePath = Config.Bind("General", "rankBundlePath", string.Format("{0}/{1}/{2}", CommonSettings.persistentDataPath, "组件", "rankpanel.ab"), "组件Bundle的保存路径");
            // 配置默认快捷键为 F2
            hotKey = Config.Bind("General", "hotKey", new BepInEx.Configuration.KeyboardShortcut(KeyCode.F2), "解密的快捷按键");
            // Plugin startup logic
            Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 已载入!");
            //Assembly.LoadFile(string.Format("{0}/{1}/{2}", CommonSettings.persistentDataPath, "组件", "RankPanel.dll"));
        }

        void Update()
        {
            if (hotKey.Value.IsDown())
            {
                //Assembly.LoadFile(string.Format("{0}/{1}/{2}", CommonSettings.persistentDataPath, "组件", "RankPanelDll.dll"));
                //载入AssetBundle包
                AssetBundle ab = AssetBundle.CreateFromFile(rankBundlePath.Value);
                RankPanel = GameObject.Instantiate(ab.LoadAsset<GameObject>("RankingPanel"));
                RankPanel.transform.SetParent(GameObject.Find("MapRoot/Canvas").transform);
                ab.Unload(false);

                //RankPanel = GameObject.Instantiate(RankPanel);
            }
        }

    }
}
