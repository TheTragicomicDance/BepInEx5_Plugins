using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JyGame;
using System.Collections.Generic;
using System.IO;
using System.Xml;
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

        /*
        void OnGUI()
        {
            if (GUI.Button(new Rect(20, 40, 80, 20), "点这里！"))
            {
                Debug.Log("OK");
                StartCoroutine(LoadTestScenece());
            }
        }

        IEnumerator LoadTestScenece()
        {
            //第三种加载方式   使用UnityWbRequest  服务器加载使用http本地加载使用file
            string url = @"file:///C:\Users\84991\Desktop\JX_Plugins\Components\Components\AssetBundles\testbundle.ab";

            WWW bundle = new WWW(url);
            yield return bundle;
            if (bundle.error == null)
            {
                AssetBundle ab = bundle.assetBundle; //将场景通过AssetBundle方式加载到内存中 
                AsyncOperation asy = Application.LoadLevelAsync("TestSence"); //sceneName不能加后缀,只是场景名称
                yield return asy;
            }
            else
            {
                Debug.LogError(bundle.error);
            }
        }
        */
    }
    
    //MainMenu下的OnMusic函数Hook
    [HarmonyPatch(typeof(MainMenu), "OnMusic")]
    class MainMenu_OnMusic_Patch
    {
        //获得lua列表
        private static List<string> GetLuaFileList()
        {
            List<string> fileList = new();
            DirectoryInfo root = new(ModManager.ModBaseUrlPath + "lua/");
            foreach (FileInfo f in root.GetFiles())
            {
                if (f.Name.EndsWith(".lua"))
                {
                    fileList.Add("jygame/" + f.Name);
                }
            }
            return fileList;
        }

        //保存解密lua文件
        private static void SaveLuaFile()
        {
            foreach (string s in GetLuaFileList())
            {
                using StreamWriter streamWriter = new(JX_Decode_Plugin.savePath_LUA + s.Replace("jygame/", ""));
                streamWriter.Write(System.Text.Encoding.UTF8.GetString(LuaManager.JyGameLuaLoader(s)));
            }
        }

        //Hook代码
        public static bool Prefix(MainMenu __instance)
        {
            List<string> visitedUrl = (List<string>)Traverse.Create<ResourceManager>().Field("visitedUri").GetValue();
            foreach (string s in visitedUrl) JX_Decode_Plugin.logXML_name.Add(s);
            //Debug.LogWarning(visitedUrl.Count);
            //Debug.LogWarning(JX_Decode_Plugin.logXML.Count);
            //Debug.LogWarning(JX_Decode_Plugin.logXML_name.Count);
            __instance.messageBoxObj.GetComponent<MessageBoxUI>().Show("提示", "解密中……", Color.green, null, "请等待");
            SaveLuaFile();
            if(JX_Decode_Plugin.logXML_name.Count == JX_Decode_Plugin.logXML.Count)
            {
                for(int i=0;i< JX_Decode_Plugin.logXML_name.Count; i++)
                {
                    using StreamWriter streamWriter = new(JX_Decode_Plugin.savePath_XML + JX_Decode_Plugin.logXML_name[i]);
                    streamWriter.Write(JX_Decode_Plugin.logXML[i]);
                }
            }
            __instance.messageBoxObj.GetComponent<MessageBoxUI>().Show("提示", $"成功解密到\n{JX_Decode_Plugin.savePath_XML}\n和\n{JX_Decode_Plugin.savePath_LUA}", Color.green, null, "ED");
            JX_Decode_Plugin.isLog = false;
            return false;
        }
    }

    //XmlDocument下的LoadXml函数Hook
    [HarmonyPatch(typeof(XmlDocument), "LoadXml")]
    class XmlDocument_LoadXml_Patch
    {
        public static bool Prefix(ref string xml)
        {
            if (JX_Decode_Plugin.isLog)
            {
                Debug.LogWarning(xml.Substring(0, 20));
                JX_Decode_Plugin.logXML.Add(xml);
            }
            return true;
        }
    }


    //XmlDocument下的LoadXml函数Hook
    [HarmonyPatch(typeof(ModItemUI), "OnLoad")]
    class ModItemUI_OnLoad_Patch
    {
        public static bool Prefix()
        {
            JX_Decode_Plugin.isLog = true;
            Debug.LogWarning("开始记录XML……");
            JX_Decode_Plugin.logXML.Clear();
            JX_Decode_Plugin.logXML_name.Clear();
            //设置第一个提示xml
            JX_Decode_Plugin.logXML_name.Add("resource_suggesttips.xml");
            return true;
        }
    }

}
