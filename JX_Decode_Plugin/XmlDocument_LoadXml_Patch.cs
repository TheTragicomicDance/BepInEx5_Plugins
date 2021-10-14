using HarmonyLib;
using System.Xml;
using UnityEngine;

namespace JX_Decode_Plugin
{
    //XmlDocument下的LoadXml函数Hook
    [HarmonyPatch(typeof(XmlDocument), "LoadXml")]
    class XmlDocument_LoadXml_Patch
    {
        public static bool Prefix(ref string xml)
        {
            if (JX_Decode_Plugin.isLog)
            {
                //Debug.LogWarning(xml.Substring(0, 20));
                JX_Decode_Plugin.logXML.Add(xml);
            }
            return true;
        }
    }

}
