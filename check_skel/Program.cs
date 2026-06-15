using System;
using System.IO;

class Program
{
    static void Main()
    {
        var paths = new[] {
            @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.idle.skel.bytes",
            @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1\vestal\base\vestal.sprite.walk.skel.bytes"
        };
        foreach (var p in paths)
        {
            if (!File.Exists(p)) { Console.WriteLine($"NOT FOUND: {p}"); continue; }
            var bytes = File.ReadAllBytes(p);
            var ver = bytes[4] + bytes[5]*256 + bytes[6]*65536 + bytes[7]*16777216;
            var ver3 = bytes[4] + bytes[5]*256 + bytes[6]*65536; // 3.6 ~ 3.7 版本字段
            Console.WriteLine($"{Path.GetFileName(p)}: {bytes.Length}B, raw_ver={ver}, ver3={ver3}");
            Console.WriteLine($"  head32: {BitConverter.ToString(bytes, 0, 32)}");
        }
    }
}
