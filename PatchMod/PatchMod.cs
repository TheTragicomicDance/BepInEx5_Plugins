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
        public static void Log(string value)
        {
            Console.WriteLine($"[Log:Patch]Info: {value}");
        }
        //DLL读取路径
        private static string repairDllPath = Path.Combine(Paths.BepInExRootPath, "Fix/Assembly-CSharp.dll");
        private static string patchDllPath = Path.Combine(Paths.ManagedPath, "Assembly-CSharp.dll");

        //待修补DLL
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        // 修补DLL
        public static void Patch(ref AssemblyDefinition patchAssembly)
        {
            //TryLoadDLL();

            //修复用的文件（包含添加进去的内容）
            AssemblyDefinition repairAssembly = AssemblyDefinition.ReadAssembly(repairDllPath);
            //TODO:下面所有方法只修补二者MainModule.
            FixModuleReference(patchAssembly.MainModule, repairAssembly.MainModule);//修复引用
            foreach (TypeDefinition typeDef in repairAssembly.MainModule.Types)
            {
                //修复类型
                FixType(patchAssembly.MainModule, typeDef, (module, belongTypeDef, fieldDef) =>
                {
                    FixField(module, belongTypeDef, fieldDef);
                }, (module, belongTypeDef, methodDef) =>
                {
                    FixMethod(module, belongTypeDef, methodDef);
                });
            }
#if DEBUG
            //输出方便dnSpy来Debug
            patchAssembly.Write("debug.dll");
#endif
        }

        //修复Dll引用，将source添加到target中
        private static void FixModuleReference(ModuleDefinition target, ModuleDefinition source)
        {
            foreach (ModuleReference modRef in source.ModuleReferences)
            {
                string name = modRef.Name;
                //如果存在重名则跳过修补
                if (!target.ModuleReferences.Any(y => y.Name == name))
                {
                    target.ModuleReferences.Add(modRef);
                }
            }
            foreach (AssemblyNameReference asmRef in source.AssemblyReferences)
            {
                string name = asmRef.FullName;
                //如果存在重名则跳过修补
                if (!target.AssemblyReferences.Any(y => y.FullName == name))
                {
                    target.AssemblyReferences.Add(asmRef);
                }
            }
        }

        //修复自定义类型，将source添加到target中
        //TODO:目前只能添加不同命名空间的类型
        private static void FixType(ModuleDefinition target, TypeDefinition source, Action<ModuleDefinition, TypeDefinition, FieldDefinition> func_FixFeild, Action<ModuleDefinition, TypeDefinition, MethodDefinition> func_FixMethod)
        {
            //不合并同名Type
            //TODO:是否添加合并同名Type判断？
            if (!target.Types.Any(x => x.Name == source.Name))
            {
#if DEBUG
                Log($"{source.FullName} is {source.GetType()}");
#endif
                //新建Type
                //如果是自定义Type直接Add会导致报错，因为属于不同的模块，
                //TODO:暂时没用unity工程的Assembly-csharp测试，不知道直接添加可否成功？
                //只向模块添加类型
                TypeDefinition importTypeDefinition = new TypeDefinition(source.Namespace, source.Name, source.Attributes) { };
                //修复基类引用关系
                //例如 Component : MonoBehaviour
                if (source.BaseType != null)
                {
                    importTypeDefinition.BaseType = source.BaseType;
                }
                target.Types.Add(importTypeDefinition);


                //添加类型下的字段
                foreach (FieldDefinition fieldDef in source.Fields)
                {
                    func_FixFeild.Invoke(target, importTypeDefinition, fieldDef);
                }

                //添加类型下的方法
                foreach (MethodDefinition methodDef in source.Methods)
                {
                    func_FixMethod.Invoke(target, importTypeDefinition, methodDef);
                }
            }
        }

        //修复类型中的Field
        private static void FixField(ModuleDefinition target, TypeDefinition typeDef, FieldDefinition fieldDef)
        {
            FieldDefinition importFieldDef = new FieldDefinition(fieldDef.Name, fieldDef.Attributes, target.ImportReference(fieldDef.FieldType, typeDef));
            typeDef.Fields.Add(importFieldDef);
            importFieldDef.Constant = fieldDef.HasConstant ? fieldDef.Constant : importFieldDef.Constant;
            importFieldDef.MarshalInfo = fieldDef.HasMarshalInfo ? fieldDef.MarshalInfo : importFieldDef.MarshalInfo;
            importFieldDef.InitialValue = (fieldDef.InitialValue != null && fieldDef.InitialValue.Length > 0) ? fieldDef.InitialValue : importFieldDef.InitialValue;
            importFieldDef.Offset = fieldDef.HasLayoutInfo ? fieldDef.Offset : importFieldDef.Offset;
#if DEBUG
            Log($"Add {importFieldDef.FullName} to {typeDef.FullName}");
#endif
        }

        //修复类型中的Method
        private static void FixMethod(ModuleDefinition target, TypeDefinition typeDef, MethodDefinition methDef)
        {
            MethodDefinition importMethodDef = new MethodDefinition(methDef.Name, methDef.Attributes, methDef.ReturnType);
            importMethodDef.ImplAttributes = methDef.ImplAttributes;
            typeDef.Methods.Add(importMethodDef);
#if DEBUG
            Log($"Add {importMethodDef.FullName} to {typeDef.FullName}");
#endif

            //复制参数
            foreach (ParameterDefinition gp in methDef.Parameters)
            {
                ParameterDefinition importPara = new ParameterDefinition(gp.Name, gp.Attributes, gp.ParameterType);
                importMethodDef.Parameters.Add(importPara);
#if DEBUG
                Log($"Add Parameter {importPara.Name} to {importMethodDef.FullName}");
#endif
            }

            //修复Method函数体
            ILProcessor ilEditor = importMethodDef.Body.GetILProcessor();
            if (methDef.HasBody)
            {
                importMethodDef.Body = new Mono.Cecil.Cil.MethodBody(importMethodDef);
            }

            //TODO:没看懂，照搬
            if (methDef.HasPInvokeInfo)
            {
                if (methDef.PInvokeInfo == null)
                {
                    // Even if this was allowed, I'm not sure it'd work out
                    //nm.RVA = meth.RVA;
                }
                else
                {
                    importMethodDef.PInvokeInfo = new PInvokeInfo(methDef.PInvokeInfo.Attributes, methDef.PInvokeInfo.EntryPoint, methDef.PInvokeInfo.Module);
                }
            }

            //函数体参数
            foreach (VariableDefinition var in methDef.Body.Variables)
            {
                importMethodDef.Body.Variables.Add(new VariableDefinition(target.ImportReference(var.VariableType, importMethodDef)));
            }
            importMethodDef.Body.MaxStackSize = methDef.Body.MaxStackSize;
            importMethodDef.Body.InitLocals = methDef.Body.InitLocals;
            importMethodDef.Body.LocalVarToken = methDef.Body.LocalVarToken;

            //修复函数覆写
            foreach (MethodReference ov in methDef.Overrides)
                importMethodDef.Overrides.Add(target.ImportReference(ov, importMethodDef));

            //修改函数返回
            importMethodDef.ReturnType = target.ImportReference(methDef.ReturnType, importMethodDef);
            importMethodDef.MethodReturnType.Attributes = methDef.MethodReturnType.Attributes;
            importMethodDef.MethodReturnType.Constant = methDef.MethodReturnType.HasConstant ? methDef.MethodReturnType.Constant : importMethodDef.MethodReturnType.Constant;
            importMethodDef.MethodReturnType.MarshalInfo = methDef.MethodReturnType.HasMarshalInfo ? methDef.MethodReturnType.MarshalInfo : importMethodDef.MethodReturnType.MarshalInfo;

            //TODO:CustomAttribute还就那个不会
            foreach (var il in methDef.Body.Instructions)
            {
#if DEBUG
                Log($"Add IL {il.OpCode.OperandType} - {il.ToString()}");
#endif
                Instruction insertIL;

                if (il.OpCode.Code == Code.Calli)
                {
                    var callSite = (CallSite)il.Operand;
                    CallSite ncs = new CallSite(target.ImportReference(callSite.ReturnType, importMethodDef))
                    {
                        HasThis = callSite.HasThis,
                        ExplicitThis = callSite.ExplicitThis,
                        CallingConvention = callSite.CallingConvention
                    };
                    foreach (ParameterDefinition param in callSite.Parameters)
                    {
                        ParameterDefinition pd = new ParameterDefinition(param.Name, param.Attributes, target.ImportReference(param.ParameterType, importMethodDef));
                        if (param.HasConstant)
                            pd.Constant = param.Constant;
                        if (param.HasMarshalInfo)
                            pd.MarshalInfo = param.MarshalInfo;
                        ncs.Parameters.Add(pd);
                    }
                    insertIL = Instruction.Create(il.OpCode, ncs);
                }
                else switch (il.OpCode.OperandType)
                    {
                        case OperandType.InlineArg:
                        case OperandType.ShortInlineArg:
                            if (il.Operand == methDef.Body.ThisParameter)
                            {
                                insertIL = Instruction.Create(il.OpCode, importMethodDef.Body.ThisParameter);
                            }
                            else
                            {
                                int param = methDef.Body.Method.Parameters.IndexOf((ParameterDefinition)il.Operand);
                                insertIL = Instruction.Create(il.OpCode, importMethodDef.Parameters[param]);
                            }
                            break;
                        case OperandType.InlineVar:
                        case OperandType.ShortInlineVar:
                            int var = methDef.Body.Variables.IndexOf((VariableDefinition)il.Operand);
                            insertIL = Instruction.Create(il.OpCode, importMethodDef.Body.Variables[var]);
                            break;
                        case OperandType.InlineField:
                            insertIL = Instruction.Create(il.OpCode, target.ImportReference((FieldReference)il.Operand, importMethodDef));
                            break;
                        case OperandType.InlineMethod:
                            insertIL = Instruction.Create(il.OpCode, target.ImportReference((MethodReference)il.Operand, importMethodDef));
                            //FixAspNetOffset(nb.Instructions, (MethodReference)il.Operand, parent);
                            break;
                        case OperandType.InlineType:
                            insertIL = Instruction.Create(il.OpCode, target.ImportReference((TypeReference)il.Operand, importMethodDef));
                            break;
                        case OperandType.InlineTok:
                            if (il.Operand is TypeReference)
                                insertIL = Instruction.Create(il.OpCode, target.ImportReference((TypeReference)il.Operand, importMethodDef));
                            else if (il.Operand is FieldReference)
                                insertIL = Instruction.Create(il.OpCode, target.ImportReference((FieldReference)il.Operand, importMethodDef));
                            else if (il.Operand is MethodReference)
                                insertIL = Instruction.Create(il.OpCode, target.ImportReference((MethodReference)il.Operand, importMethodDef));
                            else
                                throw new InvalidOperationException();
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.InlineBrTarget:
                            insertIL = Instruction.Create(il.OpCode, (Instruction)il.Operand);
                            break;
                        case OperandType.InlineSwitch:
                            insertIL = Instruction.Create(il.OpCode, (Instruction[])il.Operand);
                            break;
                        case OperandType.InlineR:
                            insertIL = Instruction.Create(il.OpCode, (double)il.Operand);
                            break;
                        case OperandType.ShortInlineR:
                            insertIL = Instruction.Create(il.OpCode, (float)il.Operand);
                            break;
                        case OperandType.InlineNone:
                            insertIL = Instruction.Create(il.OpCode);
                            break;
                        case OperandType.InlineString:
                            insertIL = Instruction.Create(il.OpCode, (string)il.Operand);
                            break;
                        case OperandType.ShortInlineI:
                            if (il.OpCode == OpCodes.Ldc_I4_S)
                                insertIL = Instruction.Create(il.OpCode, (sbyte)il.Operand);
                            else
                                insertIL = Instruction.Create(il.OpCode, (byte)il.Operand);
                            break;
                        case OperandType.InlineI8:
                            insertIL = Instruction.Create(il.OpCode, (long)il.Operand);
                            break;
                        case OperandType.InlineI:
                            insertIL = Instruction.Create(il.OpCode, (int)il.Operand);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                //ilEditor.InsertAfter(importMethodDef.Body.Instructions.Last(),ilEditor.Create(OpCodes.Nop));
                importMethodDef.Body.Instructions.Add(insertIL);
#if DEBUG
                Log($"Add IL {il.OpCode.OperandType} - {insertIL.ToString()}");
#endif

            }
            importMethodDef.IsAddOn = methDef.IsAddOn;
            importMethodDef.IsRemoveOn = methDef.IsRemoveOn;
            importMethodDef.IsGetter = methDef.IsGetter;
            importMethodDef.IsSetter = methDef.IsSetter;
            importMethodDef.CallingConvention = methDef.CallingConvention;
        }
    }
}