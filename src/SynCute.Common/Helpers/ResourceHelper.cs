using System.Text;
using Serilog;
using SynCute.Common.Models;

namespace SynCute.Common.Helpers;

public interface IResourceHelper
{
    void CheckRepository();
    List<Resource> GetAllFilesWithChecksum();
    Resource GetResourceByFullPath(string file);
    string RemoveExtraFromPath(string file);
    Task<Resource> WriteResource(MemoryStream ms);
    List<Resource> GetResourcesWithRelativePath(string[] relativePaths);
    string GetRepositoryPath();
}

public class ResourceHelper : IResourceHelper
{
    private string _repositoryPath;

    public ResourceHelper(string repositoryPath)
    {
        _repositoryPath = repositoryPath;
    }

    public void CheckRepository()
    {
        if (string.IsNullOrEmpty(_repositoryPath))
        {
            _repositoryPath =
                Path.Combine(
                    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                    "Repository");
        }
        
        if (!Directory.Exists(_repositoryPath))
        {
            Directory.CreateDirectory(_repositoryPath);
        }
    }

    private string[] GetAllFiles()
    {
        var allFiles = Directory.GetFiles(_repositoryPath, "*.*", SearchOption.AllDirectories);
        
        return allFiles;
    }

    public List<Resource> GetAllFilesWithChecksum()
    {
        var resources = new List<Resource>();

        var files = GetAllFiles();
        var count = files.Length;
        for (var i = 0; i < count; i++)
        {
            var file = files[i];
            
            resources.Add(GetResourceByFullPath(file));
        }
        
        return resources;
    }

    public Resource GetResourceByFullPath(string file)
    {
        var fileInfo = new FileInfo(file);
        var checksum = GetChecksum(file);
        var relativePath = RemoveExtraFromPath(file);

        return new Resource()
        {
            Checksum = checksum,
            ResourceName = fileInfo.Name,
            FullPath = file,
            RelativePath = relativePath
        };
    }
    
    public string RemoveExtraFromPath(string file)
    {
        return file[_repositoryPath.Length..].Replace("\\", "/");
    }

    private static string GetChecksum(string filename)
    {
        using var hasher = System.Security.Cryptography.HashAlgorithm.Create("MD5");
        using var stream = File.OpenRead(filename);
        var hash = hasher!.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "");
    }

    public async Task<Resource> WriteResource(MemoryStream ms)
    {
        var byteArray = ms.ToArray();
        ms.Close();
        var fileNameLen = BitConverter.ToInt32(byteArray, 0);
        
        var path = Encoding.UTF8.GetString(byteArray, 4, fileNameLen);
        var skipLength = 4 + fileNameLen;

        var fullPath = _repositoryPath + path;
        var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await file.WriteAsync(byteArray.AsMemory(skipLength, byteArray.Length - skipLength));
        Log.Information("File {File} with size {Size} received", path, file.Length);
        file.Close();
        
        return GetResourceByFullPath(fullPath);
    }

    public List<Resource> GetResourcesWithRelativePath(string[] relativePaths)
    {
        var resources = GetAllFilesWithChecksum();
        return resources.Where(r => relativePaths.Contains(r.RelativePath)).ToList();
    }

    public string GetRepositoryPath()
    {
        return _repositoryPath;
    }
}