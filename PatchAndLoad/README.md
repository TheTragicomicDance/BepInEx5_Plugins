# Mod形式的Dll注入插件

## 综述

本插件利用[Mono.cecil](https://github.com/jbevain/cecil)静态注入模块([BepInEx](https://github.com/BepInEx/BepInEx)包含的一个dll)实现在Unity游戏预加载(PreLoader)阶段的Dll修补工作，用以达到通过同版本Unity创建AssetBundle时候，无法打包脚本导致的游戏运行过程中利用[Harmony](https://github.com/pardeike/Harmony)等动态注入模块通过Hook函数或其他方式加载外部AssetBundle中的GameObject出现如下图所示的脚本缺失问题(*The referenced script on this Behaviour is missing!*)。

![The referenced script on this Behaviour is missing!](https://raw.githubusercontent.com/easternDay/ReadMe.IMGS/master/imgs20211019201946.png)

## 参考项目

| 名称                                            | 大概参考文件                                                          |
| ----------------------------------------------- | --------------------------------------------------------------------- |
| [IL-Repack](https://github.com/gluck/il-repack) | `ILRepack/ILRepack.cs`及其所关联的文件                                |
| [dnSpy](https://github.com/dnSpy/dnSpy)         | 反编译器，使用下面的dnlib完成IL合并，最终没有选用，但有一定的参考意义 |
| [dnlib](https://github.com/0xd4d/dnlib)         | 最终没有选用，但有一定的参考意义                                      |

# 使用方法

### 测试

将 `PatchMod.dll` 放入 `BepInEx\patchers` 文件夹中，将自己编写的Unity工程生成的Dll文件(虽然是打包成为AssetBundle，但还是需要脚本都放到一起Build一下生成 `Assembly-CSharp.dll` ，然后将 `Assembly-CSharp.dll` 命名为其他名称)放入 `BepInEx\Fix` 文件夹中。

将生成的 `LoadAssetBundle.dll` 放入 `BepInEx\plugins` 文件夹中，将 `AssetBundle\AssetBundles_Generate\rankpanel.ab` 放入和插件设置的 `rankBundlePath` 路径下（配置文件位于 `BepInEx\config\LoadAssetBundle.cfg`，第一次运行游戏会自动生成，默认在 `gamedata\组件` 中，也可以自己修改源文件或配置文件更改位置）。

所有东西准备完毕后，启动游戏，会在 `MapRoot/Canvas` 下复制一个排行榜组件，并且会调用脚本输出一些内容（如果没有 `MapRoot/Canvas` 则不显示，请自行更改）。

### 自定义使用

以 `金庸群侠传X` 为测试用例（自带 `gamedata` ），结构图如下：

![项目结构](https://raw.githubusercontent.com/easternDay/ReadMe.IMGS/master/imgs20211019210522.png)
![项目结构1](https://raw.githubusercontent.com/easternDay/ReadMe.IMGS/master/imgs20211019210743.png)

`Fix` 文件夹中存放[Assembly-CSharp.dll](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/AssetBundle/Components/Library/ScriptAssemblies/Assembly-CSharp.dll)（请更名为其他文件，同时在[PatchAndLoad\PatchMod\PatchMod.cs](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/PatchAndLoad/PatchMod/PatchMod.cs)源文件中修改）

`patchers` 文件夹中存放 `PatchMod.dll` （由[PatchAndLoad\PatchMod\PatchMod.cs](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/PatchAndLoad/PatchMod/PatchMod.cs)编译而来）

`plugins` 文件夹中存放 `LoadAssetBundle.dll` （由[PatchAndLoad\Example_LoadAssetBundle\Plugin.cs](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/PatchAndLoad/Example_LoadAssetBundle/Plugin.cs)编译而来）

`gamedata\组件` 文件夹中存放 `rankpanel.ab` （由[AssetBundle\Components](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/PatchAndLoad/Example_LoadAssetBundle/Plugin.cs)下的Unity工程编译而来），编译方法如下图所示(点击菜单栏 `AssetBundle/Build AssetBundles` ,编译完成后文件自动生成到[AssetBundle\AssetBundles_Generate](https://github.com/easternDay/JX_BepInEx5_Plugins/blob/main/AssetBundle/AssetBundles_Generate)):
![20211019212326](https://cdn.jsdelivr.net/gh/easternDay/ReadMe.IMGS//imgs20211019212326.png)

文件放置完毕后，启动 `金庸群侠传X` 即可,各个路径在源文件中位置如下：

**PatchAndLoad\PatchMod\PatchMod.cs**
```cs
/*
 * PatchAndLoad\PatchMod\PatchMod.cs
 */
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using System;
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
    public class Ptach
    {
        //日志记录
        public static void Log(string value);
        //DLL读取路径
        private static string repairDllPath = Path.Combine(Paths.BepInExRootPath, "Fix/Assembly-CSharp.dll");//自己写的dll，要加载入游戏里（建议改名字）
        private static string patchDllPath = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");//游戏本身的dll

        //待修补DLL
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        // 
		......
		其他函数
		......
	//
    }
}
```


**PatchAndLoad\Example_LoadAssetBundle\Plugin.cs**
```cs
/*
 * PatchAndLoad\Example_LoadAssetBundle\Plugin.cs
 */
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
            this.rankBundlePath = base.Config.Bind<string>("General", "rankBundlePath", string.Format("{0}/{1}/{2}", CommonSettings.persistentDataPath, "组件", "rankpanel.ab"), "组件Bundle的保存路径");//AssetBundle的读取位置
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
```
