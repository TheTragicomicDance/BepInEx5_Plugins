using BepInEx;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

public static class RankPanel_Patcher
{
    //DLL读取路径
    private static string dllName = "RankPanel.dll";
    private static string mergeDllPath = string.Format("{0}/{1}/{2}", Paths.BepInExRootPath, "Fix", dllName);

    //待修补DLL
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    //插件初始化
    /*
    public static void Initialize()
    {
        TryLoadDLL();
    }
    */

    // 修补DLL
    public static void Patch(ref AssemblyDefinition assembly)
    {
        TryLoadDLL();
    }

    //检测是否合并完成
    static void TryLoadDLL()
    {
        //判断合并的dll是否存在
        if (File.Exists(mergeDllPath))
        {
            //提前加载此DLL到内存，防止后续DLL加载到内存
            using (MemoryStream memoryStream = new MemoryStream())
            {
                //写入内存
                AssemblyDefinition.ReadAssembly(mergeDllPath).Write(memoryStream);
                Assembly.Load(memoryStream.ToArray());
                Console.WriteLine($"预加载{ mergeDllPath }");
            }
        }
        else
        {
            Console.WriteLine($"不存在{ mergeDllPath }");
        }
    }

    //合并DLL
    /*
    static void MergeDLL(string dllPath_0,string dllPath_1)
    {
        Process process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = ilMergePath;
        process.StartInfo.Arguments = string.Concat(new string[]
        {
            "/out:",
            RankPanel_Patcher.mergeDll,
            " ",
            Paths.GameRootPath,
            "/JX_Data/Managed/Assembly-CSharp.dll ",
            Paths.GameRootPath,
            "/JX_Data/Managed/RankPanel_Class.dll"
        });
        process.Start();
        Console.WriteLine(process.StandardOutput.ReadToEnd());
        process.WaitForExit();
        process.Close();
    }
    */
}