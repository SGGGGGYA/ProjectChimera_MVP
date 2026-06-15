using System;
using System.IO;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        var root = @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1";
        if (!Directory.Exists(root)) { Console.WriteLine("DD1 目录不存在"); return 1; }

        // 删除所有 atlas.txt 和 .meta
        int delAtlas = 0, delAtlasMeta = 0, delAnyMeta = 0, delAnyFile = 0;

        foreach (var f in Directory.GetFiles(root, "*.atlas.txt", SearchOption.AllDirectories).ToList())
        {
            File.Delete(f); delAtlas++;
        }
        foreach (var f in Directory.GetFiles(root, "*.atlas.txt.meta", SearchOption.AllDirectories).ToList())
        {
            File.Delete(f); delAtlasMeta++;
        }
        // 删除其他残留的 .meta（防止 Unity 引用已删的 GUID）
        foreach (var f in Directory.GetFiles(root, "*.meta", SearchOption.AllDirectories).ToList())
        {
            File.Delete(f); delAnyMeta++;
        }
        // 删除可能残留的 _SkeletonData.asset
        foreach (var f in Directory.GetFiles(root, "*_SkeletonData.asset", SearchOption.AllDirectories).ToList())
        {
            File.Delete(f); delAnyFile++;
        }
        // 删空子目录
        foreach (var d in Directory.GetDirectories(root, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length).ToList())
        {
            if (Directory.Exists(d) && !Directory.EnumerateFileSystemEntries(d).Any())
            {
                Directory.Delete(d);
            }
        }

        Console.WriteLine($"已删除: {delAtlas} atlas.txt, {delAtlasMeta} atlas.meta, {delAnyMeta} .meta, {delAnyFile} _SkeletonData.asset");
        var remaining = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        Console.WriteLine($"剩余: {remaining.Length} 个文件");
        foreach (var f in remaining) Console.WriteLine($"  {f.Substring(root.Length+1)}");
        return 0;
    }
}
