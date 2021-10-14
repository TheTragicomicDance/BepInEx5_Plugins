using HarmonyLib;
using JyGame;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JX_Decode_Plugin
{
    //MainMenu下的OnMusic函数Hook
    [HarmonyPatch(typeof(MainMenu), "OnMusic")]
    class MainMenu_OnMusic_Patch
    {
        //获得lua列表
        private static List<string> GetLuaFileList(string path)
        {
            List<string> fileList = new();
            DirectoryInfo root = new(path);
            foreach (FileInfo f in root.GetFiles())
            {
                if (f.Name.EndsWith(".lua"))
                {
                    fileList.Add("jygame/" + f.Name);
                }
            }
            foreach (DirectoryInfo d in root.GetDirectories())
            {
                List<string> subFileList = GetLuaFileList(d.FullName);
                foreach(string s in subFileList)
                {
                    fileList.Add($"jygame/{d.Name}/{s}");
                }
            }
            return fileList;
        }

        //保存解密lua文件
        public static void SaveLuaFile()
        {
            foreach (string s in GetLuaFileList(ModManager.ModBaseUrlPath + "lua/"))
            {
                Debug.LogWarning(s);
                string savePath = JX_Decode_Plugin.savePath_LUA + s.Replace("jygame/", "");
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(savePath)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(savePath));
                }
                using StreamWriter streamWriter = new(savePath);
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

}
