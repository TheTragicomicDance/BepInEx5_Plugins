# 金庸群侠传X 脚本解密插件

## 参考代码

[BepInEx5的API官方文档](https://docs.bepinex.dev/master/api/index.html "APIi文档")

[【目录&amp;汇总】从0开始教你使用BepInEx为Unity游戏制作插件Mod](https://www.aoe.top/mod/434 "有一些错误的基础入门教程")

[UnityMod开发教程 06 补丁工具Harmony](https://www.jianshu.com/p/7c46b6ace5f7 "算是给我提了个醒")

## 工具

[dnSpy](https://github.com/dnSpy/dnSpy)

## 配置文件

`游戏客户端\BepInex\config\JX_Decode_Plugin.cfg`

具体参数只有一个，第一次启动本插件会自动生成。

## 编译

VS最新版本编译即可

## 发行版下载

[下载页](https://github.com/easternDay/JX_BepInEx5_Plugins/releases "但愿吧……")

## 解密原理

金庸群侠传X中加载Lua的代码集中在 `LuaManager`中，而读取XML的代码集中在 `ResourceManager`中。

其在**原版1.1.0.6**中的源代码分别如下（**不同客户端代码不同，甚至部分客户端代码进行了混淆，具体客户端具体对待**）：

### LuaManager源码Hook

```cs
//LuaManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LuaInterface;
using UnityEngine;

namespace JyGame
{
	// Token: 0x02000018 RID: 24
	public static class LuaManager
	{
		// Token: 0x060000BD RID: 189 RVA: 0x00005644 File Offset: 0x00003844
		// Note: this type is marked as 'beforefieldinit'.
		static LuaManager()
		{
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00005668 File Offset: 0x00003868
		public static byte[] JyGameLuaLoader(string path)
		{
			if (CommonSettings.MOD_MODE)
			{
				if (path.StartsWith("jygame/"))
				{
					string text = ModManager.ModBaseUrlPath + "lua/" + path.Replace("jygame/", string.Empty);
					Debug.Log("loading lua file : " + text);
					using (StreamReader streamReader = new StreamReader(text))
					{
						string s;
						if (GlobalData.CurrentMod.enc)
						{
							s = SaveManager.crcm(streamReader.ReadToEnd());
						}
						else
						{
							s = streamReader.ReadToEnd();
						}
						return new UTF8Encoding(true).GetBytes(s);
					}
				}
				string str = "TextAssets/lua/" + path;
				Debug.Log("loading lua file : " + str);
				return Resource.GetBytes("TextAssets/lua/" + path, false);
			}
			string str2 = "TextAssets/lua/" + path;
			Debug.Log("loading lua file : " + str2);
			return Resource.GetBytes("TextAssets/lua/" + path, false);
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00005790 File Offset: 0x00003990
		public static void Reload()
		{
			LuaManager._luaConfig = null;
			LuaManager.Init(true);
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x000057A0 File Offset: 0x000039A0
		public static void Init(bool forceReset = false)
		{
			if (forceReset)
			{
				LuaManager._inited = false;
				if (LuaManager._lua != null)
				{
					LuaManager._lua.Destroy();
				}
			}
			if (LuaManager._inited)
			{
				return;
			}
			LuaManager._lua = new LuaScriptMgr();
			LuaManager._lua.Start();
			try
			{
				foreach (string str in LuaManager.files)
				{
					LuaManager._lua.DoFile("jygame/" + str);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
				FileLogger.instance.LogError("============LUA语法错误！===========");
				FileLogger.instance.LogError(ex.ToString());
			}
			LuaManager._inited = true;
			LuaTable luaTable = LuaManager.Call<LuaTable>("ROOT_getLuaFiles", new object[0]);
			try
			{
				foreach (object obj in luaTable.Values)
				{
					string str2 = (string)obj;
					LuaManager._lua.DoFile("jygame/" + str2);
				}
			}
			catch (Exception ex2)
			{
				Debug.LogError(ex2.ToString());
				FileLogger.instance.LogError("============LUA语法错误！===========");
				FileLogger.instance.LogError(ex2.ToString());
			}
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00005950 File Offset: 0x00003B50
		public static object[] Call(string functionName, params object[] paras)
		{
			if (!LuaManager._inited)
			{
				LuaManager.Init(false);
			}
			LuaFunction luaFunction = LuaManager._lua.GetLuaFunction(functionName);
			if (luaFunction == null)
			{
				Debug.LogError("调用了未定义的lua 函数:" + functionName);
				return null;
			}
			return luaFunction.Call(paras);
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00005998 File Offset: 0x00003B98
		public static T Call<T>(string functionName, params object[] paras)
		{
			if (!LuaManager._inited)
			{
				LuaManager.Init(false);
			}
			LuaFunction luaFunction = LuaManager._lua.GetLuaFunction(functionName);
			if (luaFunction == null)
			{
				Debug.LogError("调用了未定义的lua 函数:" + functionName);
				return default(T);
			}
			object[] array = luaFunction.Call(paras);
			if (array.Length == 0 || (array[0] is bool && !(bool)array[0]))
			{
				return default(T);
			}
			return (T)((object)array[0]);
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00005A20 File Offset: 0x00003C20
		public static int CallWithIntReturn(string functionName, params object[] paras)
		{
			if (!LuaManager._inited)
			{
				LuaManager.Init(false);
			}
			LuaFunction luaFunction = LuaManager._lua.GetLuaFunction(functionName);
			if (luaFunction == null)
			{
				Debug.LogError("调用了未定义的lua 函数:" + functionName);
				return -1;
			}
			object[] array = luaFunction.Call(paras);
			return Convert.ToInt32(array[0]);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00005A74 File Offset: 0x00003C74
		public static T GetConfig<T>(string key)
		{
			if (LuaManager._luaConfig == null)
			{
				LuaTable luaTable = LuaManager.Call<LuaTable>("ROOT_getConfigList", new object[0]);
				LuaManager._luaConfig = new Dictionary<string, object>();
				foreach (object obj in luaTable)
				{
					DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
					LuaManager._luaConfig.Add(dictionaryEntry.Key.ToString(), dictionaryEntry.Value);
				}
			}
			object obj2 = LuaManager._luaConfig[key];
			return (T)((object)obj2);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00005B30 File Offset: 0x00003D30
		public static string GetConfig(string key)
		{
			return LuaManager.GetConfig<string>(key);
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00005B38 File Offset: 0x00003D38
		public static int GetConfigInt(string key)
		{
			return Convert.ToInt32(LuaManager.GetConfig<object>(key));
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00005B48 File Offset: 0x00003D48
		public static double GetConfigDouble(string key)
		{
			return Convert.ToDouble(LuaManager.GetConfig<object>(key));
		}

		// Token: 0x04000097 RID: 151
		private static string[] files = new string[]
		{
			"main.lua",
			"test.lua"
		};

		// Token: 0x04000098 RID: 152
		private static bool _inited = false;

		// Token: 0x04000099 RID: 153
		public static LuaScriptMgr _lua;

		// Token: 0x0400009A RID: 154
		private static Dictionary<string, object> _luaConfig;
	}
}
```

在 `LuaManager.cs`中，将lua代码载入在 `Init()`中，而这部分代码执行的是原版lua，对应的mod的lua加载可以看到 `public static byte[] JyGameLuaLoader(string path)`该函数，此函数是读取lua文件内容，解密之后返回 `byte[]`类型结果，我们在此处利用BepInEx5结合HarmonyX框架Hook该函数返回结果以及输入参数 `path`，并将其根据 `path`中的文件名输出其 `byte[]`转 `string`的内容即可得到lua原文。

不过实际操作中Hook此函数似乎会出现一些错误，但是我们可以通过主动获取 `path`，然后构造参数调用此函数主动获取返回值。在此处我将载入Mod后的主界面*抚琴*按键点击事件Hook后，主动调用 `JyGameLuaLoader(string path)`获得所有lua源码，并利用游戏框架自带的提示面板显示结果。

具体代码如下：

```cs
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
}
```

### ResourceManager源码Hook

相比于 `LuaManager`，`ResourceManager`的Hook似乎更为简便。首先给出源代码如下：

```cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;

namespace JyGame
{
	// Token: 0x02000021 RID: 33
	public class ResourceManager
	{
		// Token: 0x06000106 RID: 262 RVA: 0x0000779C File Offset: 0x0000599C
		public ResourceManager()
		{
		}

		// Token: 0x06000107 RID: 263 RVA: 0x000077A4 File Offset: 0x000059A4
		// Note: this type is marked as 'beforefieldinit'.
		static ResourceManager()
		{
		}

		// Token: 0x06000108 RID: 264 RVA: 0x000077E0 File Offset: 0x000059E0
		public static void ResetInitFlag()
		{
			ResourceManager._inited = false;
		}

		// Token: 0x06000109 RID: 265 RVA: 0x000077E8 File Offset: 0x000059E8
		public static void Init()
		{
			if (ResourceManager._inited)
			{
				return;
			}
			ResourceManager.Clear();
			ResourceManager.LoadResource<Resource>("resource.xml", "root/resource");
			ResourceManager.LoadResource<Battle>("battles.xml", "root/battle");
			ResourceManager.LoadResource<Skill>("skills.xml", "root/skill");
			ResourceManager.LoadResource<InternalSkill>("internal_skills.xml", "root/internal_skill");
			ResourceManager.LoadResource<SpecialSkill>("special_skills.xml", "root/special_skill");
			ResourceManager.LoadResource<Role>("roles.xml", "root/role");
			ResourceManager.LoadResource<Aoyi>("aoyis.xml", "root/aoyi");
			ResourceManager.LoadResource<Story>("storys.xml", "root/story");
			ResourceManager.LoadResource<Story>("storysPY.xml", "root/story");
			ResourceManager.LoadResource<Story>("storysCG.xml", "root/story");
			ResourceManager.LoadResource<Map>("maps.xml", "root/map");
			ResourceManager.LoadResource<Item>("items.xml", "root/item");
			ResourceManager.LoadResource<ItemTrigger>("item_triggers.xml", "root/item_trigger");
			ResourceManager.LoadResource<GlobalTrigger>("globaltrigger.xml", "root/trigger");
			ResourceManager.LoadResource<Tower>("towers.xml", "root/tower");
			ResourceManager.LoadResource<RoleGrowTemplate>("grow_templates.xml", "root/grow_template");
			ResourceManager.LoadResource<AnimationNode>("animations.xml", "root/animation");
			ResourceManager.LoadResource<Shop>("shops.xml", "root/shop");
			ResourceManager.LoadResource<Menpai>("menpai.xml", "root/menpai");
			ResourceManager.LoadResource<Task>("newbie.xml", "root/task");
			ResourceManager._inited = true;
			LuaManager.Call("ROOT_onInitedResources", new object[0]);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00007948 File Offset: 0x00005B48
		public static IEnumerator Init2(CommonSettings.VoidCallBack callback)
		{
			if (ResourceManager._inited)
			{
				yield return 0;
			}
			ResourceManager.Clear();
			ResourceManager.LoadResource<Resource>("resource.xml", "root/resource");
			ResourceManager.detail = "正在加载战斗设定..";
			ResourceManager.progress = 0f;
			ResourceManager.LoadResource<Battle>("battles.xml", "root/battle");
			yield return 0;
			ResourceManager.detail = "正在加载技能设定..";
			ResourceManager.progress = 0.1f;
			ResourceManager.LoadResource<Skill>("skills.xml", "root/skill");
			yield return 0;
			ResourceManager.detail = "正在加载内功技能设定..";
			ResourceManager.progress = 0.2f;
			ResourceManager.LoadResource<InternalSkill>("internal_skills.xml", "root/internal_skill");
			yield return 0;
			ResourceManager.progress = 0.25f;
			ResourceManager.detail = "正在加载特殊技能设定..";
			ResourceManager.LoadResource<SpecialSkill>("special_skills.xml", "root/special_skill");
			yield return 0;
			ResourceManager.detail = "正在加载角色设定..";
			ResourceManager.progress = 0.3f;
			ResourceManager.LoadResource<Role>("roles.xml", "root/role");
			yield return 0;
			ResourceManager.detail = "正在加载奥义设定..";
			ResourceManager.progress = 0.35f;
			ResourceManager.LoadResource<Aoyi>("aoyis.xml", "root/aoyi");
			yield return 0;
			ResourceManager.detail = "正在加载剧本设定..";
			ResourceManager.progress = 0.5f;
			ResourceManager.LoadResource<Story>("storys.xml", "root/story");
			ResourceManager.LoadResource<Story>("storysPY.xml", "root/story");
			ResourceManager.LoadResource<Story>("storysCG.xml", "root/story");
			yield return 0;
			ResourceManager.detail = "正在加载地图设定..";
			ResourceManager.progress = 0.7f;
			ResourceManager.LoadResource<Map>("maps.xml", "root/map");
			yield return 0;
			ResourceManager.detail = "正在加载物品设定..";
			ResourceManager.progress = 0.9f;
			ResourceManager.LoadResource<Item>("items.xml", "root/item");
			yield return 0;
			ResourceManager.detail = "正在加载物品属性设定..";
			ResourceManager.progress = 0.93f;
			ResourceManager.LoadResource<ItemTrigger>("item_triggers.xml", "root/item_trigger");
			yield return 0;
			ResourceManager.detail = "正在加载触发器设定..";
			ResourceManager.progress = 0.95f;
			ResourceManager.LoadResource<GlobalTrigger>("globaltrigger.xml", "root/trigger");
			yield return 0;
			ResourceManager.detail = "正在加载天关设定..";
			ResourceManager.progress = 0.96f;
			ResourceManager.LoadResource<Tower>("towers.xml", "root/tower");
			yield return 0;
			ResourceManager.detail = "正在加载角色模板..";
			ResourceManager.progress = 0.98f;
			ResourceManager.LoadResource<RoleGrowTemplate>("grow_templates.xml", "root/grow_template");
			yield return 0;
			ResourceManager.detail = "正在加载商店设定..";
			ResourceManager.progress = 0.99f;
			ResourceManager.LoadResource<Shop>("shops.xml", "root/shop");
			yield return 0;
			ResourceManager.LoadResource<AnimationNode>("animations.xml", "root/animation");
			ResourceManager.LoadResource<Menpai>("menpai.xml", "root/menpai");
			ResourceManager.LoadResource<Task>("newbie.xml", "root/task");
			ResourceManager._inited = true;
			LuaManager.Call("ROOT_onInitedResources", new object[0]);
			if (callback != null)
			{
				callback();
			}
			yield return 0;
			yield break;
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x0600010B RID: 267 RVA: 0x0000796C File Offset: 0x00005B6C
		// (set) Token: 0x0600010C RID: 268 RVA: 0x00007974 File Offset: 0x00005B74
		public static string detail
		{
			get
			{
				return ResourceManager._detail;
			}
			set
			{
				ResourceManager._detail = value;
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x0600010D RID: 269 RVA: 0x0000797C File Offset: 0x00005B7C
		// (set) Token: 0x0600010E RID: 270 RVA: 0x00007984 File Offset: 0x00005B84
		public static float progress
		{
			get
			{
				return ResourceManager._progress;
			}
			set
			{
				ResourceManager._progress = value;
			}
		}

		// Token: 0x0600010F RID: 271 RVA: 0x0000798C File Offset: 0x00005B8C
		public static T Get<T>(string key)
		{
			foreach (Type type in ResourceManager._values.Keys)
			{
				if (typeof(T) == type && ResourceManager._values[type].ContainsKey(key))
				{
					return (T)((object)ResourceManager._values[type][key]);
				}
			}
			return default(T);
		}

		// Token: 0x06000110 RID: 272 RVA: 0x00007A3C File Offset: 0x00005C3C
		public static IEnumerable<T> GetAll<T>()
		{
			if (ResourceManager._values.ContainsKey(typeof(T)))
			{
				return ResourceManager._values[typeof(T)].Values.Cast<T>();
			}
			return null;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x00007A84 File Offset: 0x00005C84
		public static T GetRandom<T>()
		{
			return (T)((object)ResourceManager._values[typeof(T)].Values.ToList<object>()[Tools.GetRandomInt(0, ResourceManager._values[typeof(T)].Count - 1)]);
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00007ADC File Offset: 0x00005CDC
		public static T GetRandomInCondition<T>(CommonSettings.JudgeCallback judgeCallback, int retryTime = 100)
		{
			int num = 0;
			for (;;)
			{
				num++;
				if (num > retryTime)
				{
					break;
				}
				T random = ResourceManager.GetRandom<T>();
				if (judgeCallback(random))
				{
					return random;
				}
			}
			return default(T);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00007B20 File Offset: 0x00005D20
		public static void LoadResource<T>(string uri, string nodepath) where T : BasePojo
		{
			if (ResourceManager.visitedUri.Contains(uri))
			{
				return;
			}
			ResourceManager.visitedUri.Add(uri);
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				if (CommonSettings.SECURE_XML)
				{
					string path = "Scripts/Secure/" + uri.Split(new char[]
					{
						'.'
					})[0];
					string input = Resources.Load(path).ToString();
					string xml = SaveManager.crcm(input);
					xmlDocument.LoadXml(xml);
				}
				else if (CommonSettings.MOD_MODE)
				{
					string path2 = "Scripts/" + uri.Split(new char[]
					{
						'.'
					})[0];
					string xml2 = ModEditorResourceManager.GetXml(path2);
					if (GlobalData.CurrentMod.enc)
					{
						xmlDocument.LoadXml(SaveManager.crcm(xml2));
					}
					else
					{
						xmlDocument.LoadXml(xml2);
					}
				}
				else if (Application.platform == RuntimePlatform.WindowsEditor)
				{
					string path3 = Application.dataPath + "/AssetBundleSource/Editor/Scripts/" + uri;
					using (StreamReader streamReader = new StreamReader(path3))
					{
						xmlDocument.LoadXml(streamReader.ReadToEnd());
					}
				}
				else
				{
					xmlDocument.LoadXml(AssetBundleManager.GetXml(uri.Split(new char[]
					{
						'.'
					})[0]));
				}
				Dictionary<string, object> dictionary = null;
				if (ResourceManager._values.ContainsKey(typeof(T)))
				{
					dictionary = ResourceManager._values[typeof(T)];
				}
				else
				{
					dictionary = new Dictionary<string, object>();
				}
				foreach (object obj in xmlDocument.SelectNodes(nodepath))
				{
					XmlNode xmlNode = (XmlNode)obj;
					T t = BasePojo.Create<T>(xmlNode.OuterXml);
					if (dictionary.ContainsKey(t.PK))
					{
						UnityEngine.Debug.LogError("重复key:" + t.PK + ",xml=" + uri);
					}
					else
					{
						dictionary.Add(t.PK, t);
					}
				}
				if (!ResourceManager._values.ContainsKey(typeof(T)))
				{
					ResourceManager._values.Add(typeof(T), dictionary);
				}
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError("xml载入错误:" + uri);
				UnityEngine.Debug.LogError(ex.ToString());
			}
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00007DF4 File Offset: 0x00005FF4
		private static void Clear()
		{
			ResourceManager._values.Clear();
			ResourceManager.visitedUri.Clear();
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00007E0C File Offset: 0x0000600C
		public static void Add<T>(string pk, object obj)
		{
			Dictionary<string, object> dictionary;
			if (ResourceManager._values.ContainsKey(typeof(T)))
			{
				dictionary = ResourceManager._values[typeof(T)];
			}
			else
			{
				dictionary = new Dictionary<string, object>();
			}
			dictionary[pk] = obj;
			if (!ResourceManager._values.ContainsKey(typeof(T)))
			{
				ResourceManager._values.Add(typeof(T), dictionary);
			}
		}

		// Token: 0x040000B0 RID: 176
		private static bool _inited = false;

		// Token: 0x040000B1 RID: 177
		private static string _detail = string.Empty;

		// Token: 0x040000B2 RID: 178
		private static float _progress = 0f;

		// Token: 0x040000B3 RID: 179
		private static List<string> visitedUri = new List<string>();

		// Token: 0x040000B4 RID: 180
		private static Dictionary<Type, Dictionary<string, object>> _values = new Dictionary<Type, Dictionary<string, object>>();

		// Token: 0x0200017D RID: 381
		[CompilerGenerated]
		private sealed class <Init2>c__Iterator4 : IEnumerator, IDisposable, IEnumerator<object>
		{
			// Token: 0x0600120F RID: 4623 RVA: 0x0007B948 File Offset: 0x00079B48
			public <Init2>c__Iterator4()
			{
			}

			// Token: 0x170001D9 RID: 473
			// (get) Token: 0x06001210 RID: 4624 RVA: 0x0007B950 File Offset: 0x00079B50
			object IEnumerator<object>.Current
			{
				[DebuggerHidden]
				get
				{
					return this.$current;
				}
			}

			// Token: 0x170001DA RID: 474
			// (get) Token: 0x06001211 RID: 4625 RVA: 0x0007B958 File Offset: 0x00079B58
			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return this.$current;
				}
			}

			// Token: 0x06001212 RID: 4626 RVA: 0x0007B960 File Offset: 0x00079B60
			public bool MoveNext()
			{
				uint num = (uint)this.$PC;
				this.$PC = -1;
				switch (num)
				{
				case 0U:
					if (ResourceManager._inited)
					{
						this.$current = 0;
						this.$PC = 1;
						return true;
					}
					break;
				case 1U:
					break;
				case 2U:
					ResourceManager.detail = "正在加载技能设定..";
					ResourceManager.progress = 0.1f;
					ResourceManager.LoadResource<Skill>("skills.xml", "root/skill");
					this.$current = 0;
					this.$PC = 3;
					return true;
				case 3U:
					ResourceManager.detail = "正在加载内功技能设定..";
					ResourceManager.progress = 0.2f;
					ResourceManager.LoadResource<InternalSkill>("internal_skills.xml", "root/internal_skill");
					this.$current = 0;
					this.$PC = 4;
					return true;
				case 4U:
					ResourceManager.progress = 0.25f;
					ResourceManager.detail = "正在加载特殊技能设定..";
					ResourceManager.LoadResource<SpecialSkill>("special_skills.xml", "root/special_skill");
					this.$current = 0;
					this.$PC = 5;
					return true;
				case 5U:
					ResourceManager.detail = "正在加载角色设定..";
					ResourceManager.progress = 0.3f;
					ResourceManager.LoadResource<Role>("roles.xml", "root/role");
					this.$current = 0;
					this.$PC = 6;
					return true;
				case 6U:
					ResourceManager.detail = "正在加载奥义设定..";
					ResourceManager.progress = 0.35f;
					ResourceManager.LoadResource<Aoyi>("aoyis.xml", "root/aoyi");
					this.$current = 0;
					this.$PC = 7;
					return true;
				case 7U:
					ResourceManager.detail = "正在加载剧本设定..";
					ResourceManager.progress = 0.5f;
					ResourceManager.LoadResource<Story>("storys.xml", "root/story");
					ResourceManager.LoadResource<Story>("storysPY.xml", "root/story");
					ResourceManager.LoadResource<Story>("storysCG.xml", "root/story");
					this.$current = 0;
					this.$PC = 8;
					return true;
				case 8U:
					ResourceManager.detail = "正在加载地图设定..";
					ResourceManager.progress = 0.7f;
					ResourceManager.LoadResource<Map>("maps.xml", "root/map");
					this.$current = 0;
					this.$PC = 9;
					return true;
				case 9U:
					ResourceManager.detail = "正在加载物品设定..";
					ResourceManager.progress = 0.9f;
					ResourceManager.LoadResource<Item>("items.xml", "root/item");
					this.$current = 0;
					this.$PC = 10;
					return true;
				case 10U:
					ResourceManager.detail = "正在加载物品属性设定..";
					ResourceManager.progress = 0.93f;
					ResourceManager.LoadResource<ItemTrigger>("item_triggers.xml", "root/item_trigger");
					this.$current = 0;
					this.$PC = 11;
					return true;
				case 11U:
					ResourceManager.detail = "正在加载触发器设定..";
					ResourceManager.progress = 0.95f;
					ResourceManager.LoadResource<GlobalTrigger>("globaltrigger.xml", "root/trigger");
					this.$current = 0;
					this.$PC = 12;
					return true;
				case 12U:
					ResourceManager.detail = "正在加载天关设定..";
					ResourceManager.progress = 0.96f;
					ResourceManager.LoadResource<Tower>("towers.xml", "root/tower");
					this.$current = 0;
					this.$PC = 13;
					return true;
				case 13U:
					ResourceManager.detail = "正在加载角色模板..";
					ResourceManager.progress = 0.98f;
					ResourceManager.LoadResource<RoleGrowTemplate>("grow_templates.xml", "root/grow_template");
					this.$current = 0;
					this.$PC = 14;
					return true;
				case 14U:
					ResourceManager.detail = "正在加载商店设定..";
					ResourceManager.progress = 0.99f;
					ResourceManager.LoadResource<Shop>("shops.xml", "root/shop");
					this.$current = 0;
					this.$PC = 15;
					return true;
				case 15U:
					ResourceManager.LoadResource<AnimationNode>("animations.xml", "root/animation");
					ResourceManager.LoadResource<Menpai>("menpai.xml", "root/menpai");
					ResourceManager.LoadResource<Task>("newbie.xml", "root/task");
					ResourceManager._inited = true;
					LuaManager.Call("ROOT_onInitedResources", new object[0]);
					if (this.callback != null)
					{
						this.callback();
					}
					this.$current = 0;
					this.$PC = 16;
					return true;
				case 16U:
					this.$PC = -1;
					return false;
				default:
					return false;
				}
				ResourceManager.Clear();
				ResourceManager.LoadResource<Resource>("resource.xml", "root/resource");
				ResourceManager.detail = "正在加载战斗设定..";
				ResourceManager.progress = 0f;
				ResourceManager.LoadResource<Battle>("battles.xml", "root/battle");
				this.$current = 0;
				this.$PC = 2;
				return true;
			}

			// Token: 0x06001213 RID: 4627 RVA: 0x0007BDE0 File Offset: 0x00079FE0
			[DebuggerHidden]
			public void Dispose()
			{
				this.$PC = -1;
			}

			// Token: 0x06001214 RID: 4628 RVA: 0x0007BDEC File Offset: 0x00079FEC
			[DebuggerHidden]
			public void Reset()
			{
				throw new NotSupportedException();
			}

			// Token: 0x04000652 RID: 1618
			internal CommonSettings.VoidCallBack callback;

			// Token: 0x04000653 RID: 1619
			internal int $PC;

			// Token: 0x04000654 RID: 1620
			internal object $current;

			// Token: 0x04000655 RID: 1621
			internal CommonSettings.VoidCallBack <$>callback;
		}
	}
}
```

其实最开始的打算是通过获取ResourceManager._values的存储值，然后将其序列化导出，但是由于其已经加载到内存中了，所以dump出来之后仍然不是最原版加载进去的xml，而且通过横向对比几款启动器，部分混淆过后的启动器，此处的值会被混淆成unicode码，为了更加通用，因此不难发现，不管其形式如何，最后都会通过 `XmlDocument.LoadXml(string xml)`去进行xml的序列化操作，因此我们只需要Hook此处的函数，将其输入参数获取到然后输出即可。

但是输出之后发现文件名无法获取，这时候我们还是通过 `ResourceManager.visitedUri`来获取读取的xml名字（当然还需要进一步操作，此处不做赘述），然后二者结合将可以输出完整的xml代码。

部分代码如下：

```cs
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
                Debug.LogWarning(xml.Substring(0, 20));
                JX_Decode_Plugin.logXML.Add(xml);
            }
            return true;
        }
    }

}
```
