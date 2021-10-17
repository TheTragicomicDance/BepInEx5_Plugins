using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
public static class RankPanel_Patcher
{
    //DLL读取路径
    private static string dllName = "RankPanel_Class.dll";
    private static string mergeDllPath = string.Format("{0}/{1}/{2}", Paths.BepInExRootPath, "Fix", dllName);

    //待修补DLL
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    //ARGS
    static string[] args =
    {
        Path.Combine(Paths.ManagedPath,"Assembly-CSharp.dll"),
        Path.Combine(Paths.ManagedPath, dllName),
        "/out:out.dll"
    };

// 修补DLL
public static void Patch(ref AssemblyDefinition originalAssembly)
    {
        TryLoadDLL();
        ModuleDefinition originalAssemblyMainMoudle = originalAssembly.MainModule;
        //仿照IL-Repack写法写的合并DLL
        AssemblyDefinition targetAssemblyDefinition = AssemblyDefinition.ReadAssembly(mergeDllPath);
        //合并Moudle引用
        foreach (ModuleReference moduleReference in targetAssemblyDefinition.Modules.SelectMany(x => x.ModuleReferences))
        {
            string name = moduleReference.Name;
            if (!originalAssemblyMainMoudle.ModuleReferences.Any(y => y.Name == name))
            {
                Console.WriteLine($"合并{ name }……");
                originalAssemblyMainMoudle.ModuleReferences.Add(moduleReference);
            }
        }

        IMetadataScope scope = default(IMetadataScope);
        foreach (var targetType in targetAssemblyDefinition.Modules.SelectMany(x => x.Types))
        {
            string name = targetType.Name;
            Console.WriteLine($"查找到Type:{targetType.Namespace}-{ name }……");
            //不添加同名空间
            if (!originalAssemblyMainMoudle.Types.Any(y => y.Name == name))
            {
                /*
                TypeDefinition cloneTypeDefinition = new TypeDefinition(targetType.Namespace, targetType.Name, targetType.Attributes) { };
                originalAssemblyMainMoudle.Types.Add(cloneTypeDefinition);
                Console.WriteLine($"添加Type:{targetType.Namespace}-{ name }");

                //如果是函数则添加参数
                foreach (GenericParameter gp in targetType.GenericParameters)
                {
                    GenericParameter ngp = new GenericParameter(gp.Name, cloneTypeDefinition);

                    ngp.Attributes = gp.Attributes;
                    cloneTypeDefinition.GenericParameters.Add(ngp);
                }
                //修复基类引用关系
                //例如 Component : MonoBehaviour
                if (targetType.BaseType != null)
                {
                    cloneTypeDefinition.BaseType = targetType.BaseType;
                }

                //TODO:修复安全声明、修复关系引用，参考ILRepack\RepackImporter.cs，还没看懂

                //修复Fields
                foreach (FieldDefinition field in targetType.Fields)
                {
                    TypeReference cloneTypeReference = originalAssemblyMainMoudle.ImportReference(field.FieldType, cloneTypeDefinition);
                    FieldDefinition nf = new FieldDefinition(field.Name, field.Attributes, cloneTypeReference);
                    cloneTypeDefinition.Fields.Add(nf);

                    //还没研究下面三个if什么功能
                    if (field.HasConstant)
                        nf.Constant = field.Constant;

                    if (field.HasMarshalInfo)
                        nf.MarshalInfo = field.MarshalInfo;

                    if (field.InitialValue != null && field.InitialValue.Length > 0)
                        nf.InitialValue = field.InitialValue;

                    if (field.HasLayoutInfo)
                        nf.Offset = field.Offset;

                    //CustomAttribute还没修复，没看懂
                }

                //修复Methoss
                foreach (MethodDefinition meth in targetType.Methods)
                {
                    // use void placeholder as we'll do the return type import later on (after generic parameters)
                    MethodDefinition nm = new MethodDefinition(meth.Name, meth.Attributes, originalAssemblyMainMoudle.TypeSystem.Void);
                    nm.ImplAttributes = meth.ImplAttributes;
                    cloneTypeDefinition.Methods.Add(nm);
                    //复制参数
                    foreach (GenericParameter gp in meth.GenericParameters)
                    {
                        GenericParameter ngp = new GenericParameter(gp.Name, nm);

                        ngp.Attributes = gp.Attributes;
                        nm.GenericParameters.Add(ngp);
                    }
                    //TODO:没看懂，照搬
                    if (meth.HasPInvokeInfo)
                    {
                        if (meth.PInvokeInfo == null)
                        {
                            // Even if this was allowed, I'm not sure it'd work out
                            //nm.RVA = meth.RVA;
                        }
                        else
                        {
                            nm.PInvokeInfo = new PInvokeInfo(meth.PInvokeInfo.Attributes, meth.PInvokeInfo.EntryPoint, meth.PInvokeInfo.Module);
                        }
                    }
                    //参数定义
                    foreach (ParameterDefinition param in meth.Parameters)
                    {
                        ParameterDefinition pd = new ParameterDefinition(param.Name, param.Attributes, originalAssemblyMainMoudle.ImportReference(param.ParameterType, nm));
                        if (param.HasConstant)
                            pd.Constant = param.Constant;
                        if (param.HasMarshalInfo)
                            pd.MarshalInfo = param.MarshalInfo;
                        //TODO:还就那个不会
                        nm.Parameters.Add(pd);
                    }

                    //修复函数覆写
                    foreach (MethodReference ov in meth.Overrides)
                        nm.Overrides.Add(originalAssemblyMainMoudle.ImportReference(ov, nm));

                    //修改函数返回
                    nm.ReturnType = originalAssemblyMainMoudle.ImportReference(meth.ReturnType, nm);
                    nm.MethodReturnType.Attributes = meth.MethodReturnType.Attributes;
                    if (meth.MethodReturnType.HasConstant)
                        nm.MethodReturnType.Constant = meth.MethodReturnType.Constant;
                    if (meth.MethodReturnType.HasMarshalInfo)
                        nm.MethodReturnType.MarshalInfo = meth.MethodReturnType.MarshalInfo;
                    //TODO:CustomAttribute还就那个不会


                    //修复函数体
                    if (meth.HasBody)
                    {
                        nm.Body = new Mono.Cecil.Cil.MethodBody(nm);
                        var body = meth.Body;
                        var nb = nm.Body;

                        nb.MaxStackSize = body.MaxStackSize;
                        nb.InitLocals = body.InitLocals;
                        nb.LocalVarToken = body.LocalVarToken;

                        foreach (VariableDefinition var in body.Variables)
                            nb.Variables.Add(new VariableDefinition(originalAssemblyMainMoudle.ImportReference(var.VariableType, nm)));
                        foreach (Instruction instr in body.Instructions)
                        {
                            Instruction ni;

                            if (instr.OpCode.Code == Code.Calli)
                            {
                                var callSite = (CallSite)instr.Operand;
                                CallSite ncs = new CallSite(originalAssemblyMainMoudle.ImportReference(callSite.ReturnType, nm))
                                {
                                    HasThis = callSite.HasThis,
                                    ExplicitThis = callSite.ExplicitThis,
                                    CallingConvention = callSite.CallingConvention
                                };
                                foreach (ParameterDefinition param in callSite.Parameters)
                                {
                                    ParameterDefinition pd = new ParameterDefinition(param.Name, param.Attributes, originalAssemblyMainMoudle.ImportReference(param.ParameterType, nm));
                                    if (param.HasConstant)
                                        pd.Constant = param.Constant;
                                    if (param.HasMarshalInfo)
                                        pd.MarshalInfo = param.MarshalInfo;
                                    //TODO:CustomAttribute修复
                                    ncs.Parameters.Add(pd);
                                }
                                ni = Instruction.Create(instr.OpCode, ncs);
                            }
                            else switch (instr.OpCode.OperandType)
                                {
                                    case OperandType.InlineArg:
                                    case OperandType.ShortInlineArg:
                                        if (instr.Operand == body.ThisParameter)
                                        {
                                            ni = Instruction.Create(instr.OpCode, nb.ThisParameter);
                                        }
                                        else
                                        {
                                            int param = body.Method.Parameters.IndexOf((ParameterDefinition)instr.Operand);
                                            ni = Instruction.Create(instr.OpCode, nm.Parameters[param]);
                                        }
                                        break;
                                    case OperandType.InlineVar:
                                    case OperandType.ShortInlineVar:
                                        int var = body.Variables.IndexOf((VariableDefinition)instr.Operand);
                                        ni = Instruction.Create(instr.OpCode, nb.Variables[var]);
                                        break;
                                    case OperandType.InlineField:
                                        ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((FieldReference)instr.Operand, nm));
                                        break;
                                    case OperandType.InlineMethod:
                                        ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((MethodReference)instr.Operand, nm));
                                        //FixAspNetOffset(nb.Instructions, (MethodReference)instr.Operand, parent);
                                        break;
                                    case OperandType.InlineType:
                                        ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((TypeReference)instr.Operand, nm));
                                        break;
                                    case OperandType.InlineTok:
                                        if (instr.Operand is TypeReference)
                                            ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((TypeReference)instr.Operand, nm));
                                        else if (instr.Operand is FieldReference)
                                            ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((FieldReference)instr.Operand, nm));
                                        else if (instr.Operand is MethodReference)
                                            ni = Instruction.Create(instr.OpCode, originalAssemblyMainMoudle.ImportReference((MethodReference)instr.Operand, nm));
                                        else
                                            throw new InvalidOperationException();
                                        break;
                                    case OperandType.ShortInlineBrTarget:
                                    case OperandType.InlineBrTarget:
                                        ni = Instruction.Create(instr.OpCode, (Instruction)instr.Operand);
                                        break;
                                    case OperandType.InlineSwitch:
                                        ni = Instruction.Create(instr.OpCode, (Instruction[])instr.Operand);
                                        break;
                                    case OperandType.InlineR:
                                        ni = Instruction.Create(instr.OpCode, (double)instr.Operand);
                                        break;
                                    case OperandType.ShortInlineR:
                                        ni = Instruction.Create(instr.OpCode, (float)instr.Operand);
                                        break;
                                    case OperandType.InlineNone:
                                        ni = Instruction.Create(instr.OpCode);
                                        break;
                                    case OperandType.InlineString:
                                        ni = Instruction.Create(instr.OpCode, (string)instr.Operand);
                                        break;
                                    case OperandType.ShortInlineI:
                                        if (instr.OpCode == OpCodes.Ldc_I4_S)
                                            ni = Instruction.Create(instr.OpCode, (sbyte)instr.Operand);
                                        else
                                            ni = Instruction.Create(instr.OpCode, (byte)instr.Operand);
                                        break;
                                    case OperandType.InlineI8:
                                        ni = Instruction.Create(instr.OpCode, (long)instr.Operand);
                                        break;
                                    case OperandType.InlineI:
                                        ni = Instruction.Create(instr.OpCode, (int)instr.Operand);
                                        break;
                                    default:
                                        throw new InvalidOperationException();
                                }
                            //ni.SequencePoint = instr.SequencePoint;
                            nb.Instructions.Add(ni);
                        }
                    }

                    nm.IsAddOn = meth.IsAddOn;
                    nm.IsRemoveOn = meth.IsRemoveOn;
                    nm.IsGetter = meth.IsGetter;
                    nm.IsSetter = meth.IsSetter;
                    nm.CallingConvention = meth.CallingConvention;
                }
                */
                //targetAssemblyDefinition.Write("2.dll");
                //输出，供dnspy查看
                originalAssembly.Write("1.dll");

                //ILRepacking.ILRepack.Main(args);
            }
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
                    //AssemblyDefinition.ReadAssembly(mergeDllPath).Write(memoryStream);
                    //Assembly.Load(memoryStream.ToArray());
                }
            }
            else
            {
                Console.WriteLine($"不存在{ mergeDllPath }");
            }
        }
    }
}