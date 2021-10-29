using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using PatchModInfo;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BundleLoader
{
    [BepInPlugin("com.EasternDay.BundleLoader", "Mod的Bundle加载器示例", "0.0.1")]
    public class BundleLoader : BaseUnityPlugin
    {
        // 日志记录
        private readonly static ManualLogSource Logger = new("BundleLoader");

        // MOD文件读取路径
        private static readonly string modIndexPath = Path.Combine(Paths.GameRootPath, "PatchMod");

        //插件配置
        private static readonly List<ModInfo> mods = new();                 //Mod目录
        private static readonly Dictionary<string, Object> objects = new(); //游戏物体列表

        private void Awake()
        {
            // BepInEx将自定义日志注册
            BepInEx.Logging.Logger.Sources.Add(Logger);
        }

        private void Start()
        {
            //读取mods
            foreach (string dir in Directory.GetDirectories(modIndexPath))
            {
                ModInfo curInfo = ModInfo.GetModInfo(dir);
                mods.Add(curInfo);
                foreach (string bundlePath in curInfo.Resources)
                {
                    // TODO:不同版本会报错
                    foreach (Object obj in AssetBundle.CreateFromFile(bundlePath).LoadAllAssets())
                    {
                        Logger.LogInfo($"加载MOD资源：{dir}-{obj.name}");
                        objects.Add(obj.name, obj);
                    }
                }
            }
        }
    }
}
