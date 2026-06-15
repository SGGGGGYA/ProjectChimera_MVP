using System;
using System.IO;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        // 检查源
        var src = @"E:\xlyp\DD.v26186\heroes\vestal\anim";
        Console.WriteLine("=== 源目录文件 ===");
        if (Directory.Exists(src))
        {
            foreach (var f in Directory.GetFiles(src))
            {
                var info = new FileInfo(f);
                var first4 = new byte[4];
                using (var fs = File.OpenRead(f)) fs.Read(first4, 0, 4);
                Console.WriteLine($"  {info.Name} ({info.Length} bytes) head: {BitConverter.ToString(first4)}");
            }
        }
        else
        {
            Console.WriteLine("  源目录不存在");
        }

        // 检查目标
        var dst = @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base";
        Console.WriteLine("\n=== 目标目录文件 ===");
        if (Directory.Exists(dst))
        {
            foreach (var f in Directory.GetFiles(dst))
            {
                var info = new FileInfo(f);
                var first4 = new byte[4];
                using (var fs = File.OpenRead(f)) fs.Read(first4, 0, 4);
                Console.WriteLine($"  {info.Name} ({info.Length} bytes) head: {BitConverter.ToString(first4)}");
            }
        }
        else
        {
            Console.WriteLine("  目标目录不存在");
        }

        return 0;
    }
}
