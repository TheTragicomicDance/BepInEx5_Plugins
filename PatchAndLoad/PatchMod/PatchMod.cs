using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using PatchModInfo;
using SharpConfig;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
/*
* INFO: IL Repack - Version 2.0.18
* INFO: ------------- IL Repack Arguments -------------
* /out:1.DLL  D:\金X\NTR传_自用测试\JX_Data\Managed\Assembly-CSharp.dll D:\金X\NTR传_自用测试\JX_Data\Managed\RankPanel_Class.dll
* -----------------------------------------------
* INFO: Adding assembly for merge: D:\金X\NTR传_自用测试\JX_Data\Managed\Assembly-CSharp.dll
* INFO: Adding assembly for merge: D:\金X\NTR传_自用测试\JX_Data\Managed\RankPanel_Class.dll
* INFO: Processing references
* INFO: Processing types
* INFO: Merging <Module>
* INFO: Merging <Module>
* INFO: Processing exported types
* INFO: Processing resources
* INFO: Fixing references
* INFO: Writing output assembly to disk
* INFO: Finished in 00:00:01.7130349
*/
namespace PatchMod
{
    public class PatchMain
    {
        // 日志记录
        private readonly static ManualLogSource Logger = new("PatchMod");

        // MOD文件读取路径
        private static readonly string modIndexPath = Path.Combine(Paths.GameRootPath, "PatchMod");
        // 插件配置文件读取路径
        private static readonly string configPath = Path.Combine(modIndexPath, "PatchMod.cfg");

        // 待修补DLL
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        //插件配置
        private static bool isPreLoad = false;      //是否预先加载dll到内存
        private static bool isSave = false;         //是否保存合并后的dll内容到本地
        private static readonly List<ModInfo> mods = new();  //Mod目录


        // 修补前的准备
        public static void Initialize()
        {
            // BepInEx将自定义日志注册
            BepInEx.Logging.Logger.Sources.Add(Logger);
            // 读取插件配置文件
            Configuration config = Configuration.LoadFromFile(configPath);
            Section section = config["General"];
            isPreLoad = section["preLoad"].GetValue<bool>();
            isSave = section["save2local"].GetValue<bool>();
            //读取mods
            foreach (string dir in Directory.GetDirectories(modIndexPath))
            {
                Logger.LogInfo($"加载MOD：{dir}");
                mods.Add(ModInfo.GetModInfo(dir));
            }
        }

        // 修补DLL
        public static void Patch(ref AssemblyDefinition patchAssembly)
        {
            //修复Dll
            foreach (string path in mods.SelectMany(x => x.Dlls))
            {
                MergeDll.Fix(path, ref patchAssembly);
            }
            //预加载
            if (isPreLoad)
            {
                //参考BepInEx:BepInEx.Preloader.Core\Patching\AssemblyPatcher.cs
                using MemoryStream memoryStream = new();
                patchAssembly.Write(memoryStream);
                Assembly.Load(memoryStream.ToArray());
            }
            //保存到本地
            if (isSave)
            {
                //输出方便dnSpy来Debug
                patchAssembly.Write("debug.dll");
            }

        }

    }
}