using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PatchModInfo
{
    public class ModInfo
    {
        private static readonly IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
        public string Name { get; set; }
        public List<string> Dlls { get; set; }
        public List<string> Resources { get; set; }

        public static ModInfo GetModInfo(string path)
        {
            //读取配置文件
            foreach (string file in Directory.GetFiles(path))
            {
                if (file.EndsWith(".yml"))
                {
                    using (StreamReader content = new StreamReader(file))
                    {
                        ModInfo info = deserializer.Deserialize<ModInfo>(content);
                        //转化为完整地址
                        for (int i = 0; i < info.Dlls.Count; i++)
                        {
                            info.Dlls[i] = Path.Combine(path, info.Dlls[i]);
                        }
                        for (int i = 0; i < info.Resources.Count; i++)
                        {
                            info.Resources[i] = Path.Combine(path, info.Resources[i]);
                        }
                        return info;
                    }
                }
            }
            return new ModInfo();
        }
    }
}
