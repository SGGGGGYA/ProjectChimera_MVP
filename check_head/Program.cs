using System;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        var files = new[] {
            @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes",
            @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.atlas.txt",
            @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.png"
        };
        foreach (var f in files)
        {
            if (!File.Exists(f)) { Console.WriteLine($"NOT FOUND: {f}"); continue; }
            var bytes = File.ReadAllBytes(f);
            var name = Path.GetFileName(f);
            // 打印前 64 字节（hex + ascii 视图）
            Console.WriteLine($"\n=== {name} ({bytes.Length} bytes) ===");
            int showLen = Math.Min(64, bytes.Length);
            for (int i = 0; i < showLen; i += 16)
            {
                var hex = new System.Text.StringBuilder();
                var asc = new System.Text.StringBuilder();
                for (int j = 0; j < 16 && i + j < showLen; j++)
                {
                    hex.AppendFormat("{0:X2} ", bytes[i + j]);
                    char c = (char)bytes[i + j];
                    asc.Append(char.IsControl(c) || c > 127 ? '.' : c);
                }
                Console.WriteLine($"  {i:X4}  {hex,-48}  {asc}");
            }
        }
        return 0;
    }
}
