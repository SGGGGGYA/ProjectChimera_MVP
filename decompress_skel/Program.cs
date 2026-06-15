using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args)
    {
        // 检查根目录
        var root = @"E:\游\ProjectChimera_MVP\ProjectChimera_MVP\Assets\SpineCharacters\DD1";
        if (!Directory.Exists(root))
        {
            Console.WriteLine($"找不到目录: {root}");
            return 1;
        }

        var files = Directory.GetFiles(root, "*.skel.bytes", SearchOption.AllDirectories);
        Console.WriteLine($"找到 {files.Length} 个 .skel.bytes 文件");

        int ok = 0, fail = 0, noCompress = 0, alreadyDecompressed = 0;
        foreach (var f in files)
        {
            try
            {
                var bytes = File.ReadAllBytes(f);
                // zlib magic: 0x78 0x01 / 0x78 0x9C / 0x78 0xDA
                bool isZlib = bytes.Length > 2 && bytes[0] == 0x78 && (bytes[1] == 0x01 || bytes[1] == 0x9C || bytes[1] == 0xDA);
                if (!isZlib)
                {
                    // 检查是否已经解压：Spine skel 文件头通常是 ASCII "skel" 后跟版本号
                    // 但新版可能直接是数字版本号
                    if (bytes[0] == 0x73 && bytes[1] == 0x6B && bytes[2] == 0x65 && bytes[3] == 0x6C)
                    {
                        // "skel" ascii header
                        alreadyDecompressed++;
                        continue;
                    }
                    noCompress++;
                    continue;
                }

                // zlib 解压（跳过前 2 字节 zlib header，最后 4 字节是 adler32 校验和）
                using (var input = new MemoryStream(bytes, 2, bytes.Length - 6))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    deflate.CopyTo(output);
                    var decompressed = output.ToArray();
                    // 备份原文件
                    var backup = f + ".compressed.bak";
                    if (!File.Exists(backup))
                        File.Copy(f, backup, true);
                    File.WriteAllBytes(f, decompressed);
                    ok++;
                    Console.WriteLine($"  解压: {Path.GetFileName(f)} ({bytes.Length} -> {decompressed.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                fail++;
                Console.WriteLine($"  失败: {Path.GetFileName(f)} - {ex.Message}");
            }
        }

        Console.WriteLine($"\n完成: 解压={ok} 失败={fail} 未压缩={noCompress} 已是明文={alreadyDecompressed}");
        return fail > 0 ? 1 : 0;
    }
}
