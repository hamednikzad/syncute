using System.Text;
using SynCute.Core.Models;
using System;
using Serilog;

namespace SynCute.Core.Helpers;

public static class ResourceHelper
{
    private static readonly string RepositoryPath;

    static ResourceHelper()
    {
        RepositoryPath =
            Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "Repository");
    }
    public static void CheckRepository()
    {
        if (!Directory.Exists(RepositoryPath))
        {
            Directory.CreateDirectory(RepositoryPath);
        }
    }

    private static string[] GetAllFiles()
    {
        var allFiles = Directory.GetFiles(RepositoryPath, "*.*", SearchOption.AllDirectories);
        
        //Get Empty Directories also
        //var directories = Directory.GetDirectories(_repositoryPath, "*.*", SearchOption.AllDirectories);

        return allFiles;
    }

    public static List<Resource> GetAllFilesWithChecksum()
    {
        var resources = new List<Resource>();

        var files = GetAllFiles();
        var count = files.Length;
        for (var i = 0; i < count; i++)
        {
            var file = files[i];
            var fileInfo = new FileInfo(file);
            var checksum = GetChecksum(HashingAlgoTypes.MD5, file);
            var relativePath = RemoveExtraFromPath(file);
            
            resources.Add(new Resource()
            {
                Checksum = checksum,
                ResourceName = fileInfo.Name,
                FullPath = file,
                RelativePath = relativePath
            });
        }
        
        return resources;
    }

    public static string[,] GetAllFilesWithChecksumAsArray()
    {
        var files = GetAllFilesWithChecksum();
        var count = files.Count;
        var result = new string[count,2];

        for (var i = 0; i < count; i++)
        {
            var file = files[i];
            var checksum = GetChecksum(HashingAlgoTypes.MD5, file.Checksum);
            var relativePath = RemoveExtraFromPath(file.RelativePath);
            result[i, 0] = relativePath;
            result[i, 1] = checksum;
        }

        return result;
    }

    private static string RemoveExtraFromPath(string file)
    {
        return file.Substring(RepositoryPath.Length).Replace("\\", "/");
    }
    
    private static string GetChecksum(HashingAlgoTypes hashingAlgoType, string filename)
    {
        using var hasher = System.Security.Cryptography.HashAlgorithm.Create(hashingAlgoType.ToString());
        using var stream = File.OpenRead(filename);
        var hash = hasher!.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }
    
    private enum HashingAlgoTypes
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512
    }

    public static async Task Write(MemoryStream ms)
    {
        var byteArray = ms.ToArray();
        ms.Close();
        var fileNameLen = BitConverter.ToInt32(byteArray, 0);
        
        var path = Encoding.UTF8.GetString(byteArray, 4, fileNameLen);
        var skipLength = 4 + fileNameLen;
        
        var file = new FileStream(RepositoryPath + path, FileMode.Create, FileAccess.Write);
        await file.WriteAsync(byteArray.AsMemory(skipLength, byteArray.Length - skipLength));
        file.Close();
        Log.Information("File {File} received", path);
    }

    public static List<Resource> GetResourcesWithRelativePath(string[] relativePaths)
    {
        var resources = GetAllFilesWithChecksum();
        return resources.Where(r => relativePaths.Contains(r.RelativePath)).ToList();
    }
}