# Mod形式的Dll注入插件

## 综述

本插件利用[Mono.cecil](https://github.com/jbevain/cecil)静态注入模块([BepInEx](https://github.com/BepInEx/BepInEx)包含的一个dll)实现在Unity游戏预加载(PreLoader)阶段的Dll修补工作，用以达到通过同版本Unity创建AssetBundle时候，无法打包脚本导致的游戏运行过程中利用[Harmony](https://github.com/pardeike/Harmony)等动态注入模块通过Hook函数或其他方式加载外部AssetBundle中的GameObject出现如下图所示的脚本缺失问题(*The referenced script on this Behaviour is missing!*)。

![The referenced script on this Behaviour is missing!](https://raw.githubusercontent.com/easternDay/ReadMe.IMGS/master/imgs20211019201946.png)                                    |

## 使用方法

## 目录结构

只给出了与项目中**所给例子**相匹配的目录结构，具体结构自行结合实际修改。

* *BepInEx*
  * *config*
  * *core*
  * **patches**

    * **PatchMod.dll**
    * **PatchModInfo.dll**
    * **YamlDotNet.dll**
  * **plugins**

    * **RankPanel_Trigger.dll**
    * **BundleLoader**
      * **BundleLoader.dll**
      * **PatchModInfo.dll**
      * **YamlDotNet.dll**
* *doorstop_config.ini*
* *winhttp.dll*
* **PatchMod**
  * **PatchMod.cfg**
  * **RankPanel**
    * **mods.yml**
    * **Dlls**
      * **Assembly-CSharp.dll**
    * **AseetBundles**
      * **rankpanel.ab**
* *其他文件*

### 构建

将 `PatchMod.dll` 放入 `BepInEx\patchers` 文件夹中，将 `BundleLoader.dll` 放入 `BepInEx\plugins` 文件夹中。

对应Mod包的结构参考 `PatchMod_Example.zip`进行开发，将解压后的 `PatchMod`文件夹放入游戏根目录中。

![PatchMod放置位置](https://cdn.jsdelivr.net/gh/easternDay/ReadMe.IMGS//PatchMod/20211029223822.png)

目录中包含 `PatchMod.cfg`与各个Mod的包文件。

`PatchMod.cfg`文件内容如下：

```ini
[General]
# 是否预先加载进内存，预先加载进去可以防止其他Assembly-csharp加载
preLoad=true
# 是否将修补后的Dll输出到本地，用于调试查看
save2local=false
```

样板Mod中包含一个排行榜Mod，其打包过程如下：自己根据所要开发插件的游戏的Unity版本，用相同版本开发出组件并编写脚本，将要加入到游戏内的 `Object`打包为 `AssetBundle`，并记住其名字，然后插件项目整体进行构建，得到插件项目的 `Assembly-csharp.dll`，放到文件夹内。

将自己编写的Unity工程生成的Dll文件(虽然是打包成为AssetBundle，但还是需要脚本都放到一起Build一下生成 `Assembly-CSharp.dll` ，然后将 `Assembly-CSharp.dll` 命名为其他名称)放入 `BepInEx\Fix` 文件夹中。

![Mod文件夹结构](https://cdn.jsdelivr.net/gh/easternDay/ReadMe.IMGS//PatchMod/20211029224345.png)

在这里我的Dll文件放到了 `Dlls`文件夹下，AssetBundle文件放到了 `Resources`文件夹下，并在 `mod.yml`(拓展名为 `.yml`的文件即可)内编辑Mod相关设置。

```yaml
# Mod名
name: 排行榜面板
# Dll读取路径
dlls:
    - Dlls/Assembly-CSharp.dll
# AssetBundle读取路径
resources:
    - AseetBundles/rankpanel.ab
```

进入游戏后在BepInEx控制台内即可以看到相关插件输出内容以及Mod组件加载列表。此后再根据其他插件Hook某些触发调用 `Object`即可。本项目内自带一个[测试本用例的插件](https://github.com/easternDay/JX_BepInEx5_Plugins/tree/main/RankPanel_Trigger)，亦可以下载[完整版测试用例【金庸群侠传X】](https://drive.google.com/file/d/1enfsl-EUBEWBLl91j-4gDZHzE0gXxIdu/view?usp=sharing)进行测试。

## 温馨提示

*采用本插件开发时，请注意 `Nuget`的包和引用库版本务必和自己的 `Unity`版本相匹配.*

## 参考项目

| 名称                                         | 大概参考文件                                                          |
| -------------------------------------------- | --------------------------------------------------------------------- |
| [IL-Repack](https://github.com/gluck/il-repack) | `ILRepack/ILRepack.cs`及其所关联的文件                              |
| [dnSpy](https://github.com/dnSpy/dnSpy)         | 反编译器，使用下面的dnlib完成IL合并，最终没有选用，但有一定的参考意义 |
| [dnlib](https://github.com/0xd4d/dnlib)         | 最终没有选用，但有一定的参考意义                                      |
