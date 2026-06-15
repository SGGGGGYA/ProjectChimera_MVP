using System;
using System.IO;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: BankExtractor <bankFileOrFolder> <outputFolder>");
            return;
        }

        string inputPath = args[0];
        string outputFolder = args[1];
        Directory.CreateDirectory(outputFolder);

        if (File.Exists(inputPath))
        {
            ExtractBank(inputPath, outputFolder);
        }
        else if (Directory.Exists(inputPath))
        {
            foreach (var bankFile in Directory.GetFiles(inputPath, "*.bank"))
            {
                ExtractBank(bankFile, outputFolder);
            }
        }
        else
        {
            Console.WriteLine($"Path not found: {inputPath}");
        }
    }

    static void ExtractBank(string bankPath, string outputFolder)
    {
        string bankName = Path.GetFileNameWithoutExtension(bankPath);
        Console.WriteLine($"Processing: {bankName}");

        byte[] bankData = File.ReadAllBytes(bankPath);

        // Search for all FSB5 magic bytes in the bank file
        int searchStart = 0;
        int fsbCount = 0;

        while (true)
        {
            int fsbOffset = FindFsb5Offset(bankData, searchStart);
            if (fsbOffset < 0) break;

            fsbCount++;

            // Read FSB5 header sizes for precise extraction
            // FSB5 header: 64 bytes
            // offset 12-15: sampleHeadersSize
            // offset 16-19: nameTableSize
            // offset 20-23: dataSize
            uint sampleHeadersSize = BitConverter.ToUInt32(bankData, fsbOffset + 12);
            uint nameTableSize = BitConverter.ToUInt32(bankData, fsbOffset + 16);
            uint dataSize = BitConverter.ToUInt32(bankData, fsbOffset + 20);
            uint fsbLength = 64 + sampleHeadersSize + nameTableSize + dataSize;

            // Clamp to available data
            int maxLength = bankData.Length - fsbOffset;
            if (fsbLength > maxLength)
                fsbLength = (uint)maxLength;

            byte[] fsbData = new byte[fsbLength];
            Buffer.BlockCopy(bankData, fsbOffset, fsbData, 0, (int)fsbLength);

            string subFolder = fsbCount > 1
                ? Path.Combine(outputFolder, $"{bankName}_{fsbCount}")
                : Path.Combine(outputFolder, bankName);
            Directory.CreateDirectory(subFolder);

            if (FsbLoader.TryLoadFsbFromByteArray(fsbData, out FmodSoundBank? bank) && bank != null)
            {
                Console.WriteLine($"  Found FSB5 at offset {fsbOffset}, samples: {bank.Samples.Count}, format: {bank.Header.AudioType}");

                int sampleIndex = 0;
                foreach (var sample in bank.Samples)
                {
                    string sampleName = !string.IsNullOrEmpty(sample.Name)
                        ? SanitizeFileName(sample.Name)
                        : $"sample_{sampleIndex:D3}";

                    bool success = sample.RebuildAsStandardFileFormat(out byte[]? audioData, out string? extension);
                    if (success && audioData != null && extension != null)
                    {
                        string outPath = Path.Combine(subFolder, $"{sampleName}.{extension}");
                        File.WriteAllBytes(outPath, audioData);
                        Console.WriteLine($"    -> {sampleName}.{extension}");
                    }
                    else
                    {
                        Console.WriteLine($"    -> {sampleName} (unsupported format: {bank.Header.AudioType})");
                    }
                    sampleIndex++;
                }
            }
            else
            {
                Console.WriteLine($"  Found FSB5 at offset {fsbOffset} but failed to load.");
            }

            searchStart = fsbOffset + 4;
        }

        if (fsbCount == 0)
        {
            Console.WriteLine($"  No FSB5 data found in {bankName}");
        }
    }

    static int FindFsb5Offset(byte[] data, int start)
    {
        for (int i = start; i < data.Length - 4; i++)
        {
            if (data[i] == 0x46 && data[i + 1] == 0x53 && data[i + 2] == 0x42 && data[i + 3] == 0x35)
                return i;
        }
        return -1;
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
