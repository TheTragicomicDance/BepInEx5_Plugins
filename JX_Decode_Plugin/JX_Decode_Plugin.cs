using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JyGame;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JX_Decode_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class JX_Decode_Plugin : BaseUnityPlugin
    {
        //配置文件
        private ConfigEntry<string> savePath;//保存路径
        public static string savePath_LUA;//LUA保存路径
        public static string savePath_XML;//XML保存路径
        private ConfigEntry<KeyboardShortcut> hotKey { get; set; }//解密快捷键

        //GUI文本信息
        private bool isShowGUI = false;
        private string guiInfo = "";

        //Hook框架
        private static Harmony harmony = new("JX_Decode_Patch");
        public static Harmony Harmony { get => harmony; set => harmony = value; }

        //记录标志
        public static bool isLog = false;
        public static List<string> logXML = new();
        public static List<string> logXML_name = new();

        private void Awake()
        {
            //加载配置文件
            savePath = Config.Bind("General",      // The section under which the option is shown
                             "savePath",  // The key of the configuration option in the configuration file
                             string.Format("{0}/{1}", CommonSettings.persistentDataPath, "解密"), // The default value
                             "配置文件的保存路径"); // Description of the option to show in the config file
            // 配置默认快捷键为 左Ctrl + D
            hotKey = Config.Bind("General", "hotKey", new BepInEx.Configuration.KeyboardShortcut(KeyCode.D, KeyCode.LeftControl), "解密的快捷按键");
            //设置基础路径
            savePath_LUA = string.Format("{0}/{1}/", savePath.Value, "LUA");
            savePath_XML = string.Format("{0}/{1}/", savePath.Value, "XML");



            //清空创建文件夹
            try
            {
                Directory.Delete(savePath.Value, true);
                Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 删除 {savePath.Value}!");
            }
            finally
            {
                Directory.CreateDirectory(savePath.Value);
                Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 创建 {savePath.Value}!");
                Directory.CreateDirectory(savePath_XML);
                Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 创建 {savePath_XML}!");
                Directory.CreateDirectory(savePath_LUA);
                Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 创建 {savePath_LUA}!");
                // 控制台提示语
                Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 已加载!");
            }

        }

        void Start()
        {
            //执行所有代码Hook
            DoPatching();
        }

        void Update()
        {
            if (hotKey.Value.IsDown())
            {
                isShowGUI = true;
                Decode();
            }
        }

        void Decode()
        {
            List<string> visitedUrl = (List<string>)Traverse.Create<ResourceManager>().Field("visitedUri").GetValue();
            foreach (string s in visitedUrl) JX_Decode_Plugin.logXML_name.Add(s);
            guiInfo = "解密中……";
            MainMenu_OnMusic_Patch.SaveLuaFile();
            if (JX_Decode_Plugin.logXML_name.Count == JX_Decode_Plugin.logXML.Count)
            {
                for (int i = 0; i < JX_Decode_Plugin.logXML_name.Count; i++)
                {
                    using StreamWriter streamWriter = new(JX_Decode_Plugin.savePath_XML + JX_Decode_Plugin.logXML_name[i]);
                    streamWriter.Write(JX_Decode_Plugin.logXML[i]);
                }
            }
            guiInfo = $"成功解密到\n{JX_Decode_Plugin.savePath_XML}\n和\n{JX_Decode_Plugin.savePath_LUA}";
            JX_Decode_Plugin.isLog = false;
            //isShowGUI = false;
        }

        //自动代码Hook，此处也可改写为手动代码Hook
        void DoPatching()
        {
            //Hook所有代码
            Harmony = new Harmony("JX_Decode_Patch");
            Harmony.PatchAll();
            // 控制台提示语
            Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 成功Hook代码!");
        }

        
        void OnGUI()
        {
            if (isShowGUI)
            {
                // 定义窗口位置 x y 宽 高
                Rect windowRect = new Rect(500, 200, 500, 300);
                // 创建一个新窗口
                // 注意：第一个参数(20210218)为窗口ID，ID尽量设置的与众不同，若与其他Mod的窗口ID相同，将会导致窗口冲突
                windowRect = GUI.Window(849919718/1, windowRect, DoMyWindow, "信息提示框");
            }
            else
            {
                if (GUILayout.Button("解密"))
                {
                    isShowGUI = true;
                    Decode();
                }
            }
        }
        public void DoMyWindow(int winId)
        {
            GUILayout.BeginArea(new Rect(10, 20, 490, 250));
            // 这里的大括号是可选的，我个人为了代码的阅读性,习惯性的进行了添加
            // 建议大家也使用大括号这样包裹起来，让代码看起来不那么的乱
            {
                GUILayout.Label(guiInfo);
                if (GUILayout.Button("隐藏"))
                {
                    isShowGUI = false;
                }
            }
            GUILayout.EndArea();
        }
    }

}
