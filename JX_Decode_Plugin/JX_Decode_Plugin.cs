using BepInEx;
using HarmonyLib;
using JyGame;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JX_Decode_Plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class JX_Decode_Plugin : BaseUnityPlugin
    {
        private static Harmony harmony = new Harmony("JX_Decode_Patch");
        public static Harmony Harmony { get => harmony; set => harmony = value; }

        public static string savePath = string.Format("{0}/{1}", CommonSettings.persistentDataPath, "解密");

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"插件 {PluginInfo.PLUGIN_GUID} 已加载!");
        }

        private void Start()
        {
            DoPatching();
        }

        void DoPatching()
        {
            //Hook所有代码
            Harmony = new Harmony("JX_Decode_Patch");
            Harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(MainMenu), "OnMusic")]
    class MainMenu_OnMusic_Patch
    {
        //xmls的对应位置，参考原版函数
        private static string xmlsHookPath = "\u200E\u202B\u206C\u206A\u206A\u202C\u200E\u206A\u206C\u200F\u206F\u206E\u202E\u206A\u200F\u202B\u206A\u200F\u202C\u202E\u202E\u200E\u206B\u200E\u206B\u206F\u206C\u200E\u200D\u202E\u200C\u202B\u206F\u202B\u200C\u200F\u202A\u202B\u202A\u206A\u202E";

        //获得lua列表
        private static List<string> getLuaFileList()
        {
            List<string> fileList = new List<string>();
            DirectoryInfo root = new DirectoryInfo(ModManager.ModBaseUrlPath + "lua/");
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
        private static void saveLuaFile()
        {
            string savePath = string.Format("{0}/{1}/", JX_Decode_Plugin.savePath, "LUA");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            foreach (string s in getLuaFileList())
            {
                using (StreamWriter streamWriter = new StreamWriter(savePath + s.Replace("jygame/", "")))
                {
                    streamWriter.Write(System.Text.Encoding.UTF8.GetString(LuaManager.JyGameLuaLoader(s)));
                }
            }
        }

        //保存解密xml文件
        private static void saveXmlFile()
        {
            string savePath = string.Format("{0}/{1}/", JX_Decode_Plugin.savePath, "XML");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            Dictionary<Type, Dictionary<string, object>> gameInfo = (Dictionary<Type, Dictionary<string, object>>)Traverse.Create<JyGame.ResourceManager>().Field(xmlsHookPath).GetValue();
            foreach (KeyValuePair<Type, Dictionary<string, object>> kv in gameInfo)
            {
                //拼接
                string content = string.Format("<{0}>\n", kv.Key.ToString());
                foreach (KeyValuePair<string, object> kv1 in kv.Value)
                {
                    content += "\t" + (JyGame.Tools.SerializeXML(kv1.Value)) + "\n";
                }
                content += string.Format("</{0}>", kv.Key.ToString());
                //输出
                using (StreamWriter streamWriter = new StreamWriter(string.Format("{0}/{1}", savePath, kv.Key.ToString() + ".xml")))
                {
                    streamWriter.Write(content);
                }
            }

        }

        //Hook代码
        public static bool Prefix(MainMenu __instance)
        {
            //清空创建文件夹
            try
            {
                Directory.Delete(JX_Decode_Plugin.savePath, true);
            }
            finally
            {
                Directory.CreateDirectory(JX_Decode_Plugin.savePath);
            }
            __instance.messageBoxObj.GetComponent<MessageBoxUI>().Show("提示", "解密中……", Color.green, null, "请等待");
            saveLuaFile();
            saveXmlFile();
            __instance.messageBoxObj.GetComponent<MessageBoxUI>().Show("提示", "成功解密到" + JX_Decode_Plugin.savePath, Color.green, null, "ED");
            return false;
        }
    }

}
