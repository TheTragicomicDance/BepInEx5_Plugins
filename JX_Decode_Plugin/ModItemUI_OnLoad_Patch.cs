using HarmonyLib;
using UnityEngine;

namespace JX_Decode_Plugin
{
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
