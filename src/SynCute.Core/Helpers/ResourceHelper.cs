namespace SynCute.Core.Helpers;

public static class ResourceHelper
{
    private static string _repositoryPath = "";

    private static void CheckRepository()
    {
        _repositoryPath =
            Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "Repository");
        
        if (!Directory.Exists(_repositoryPath))
        {
            Directory.CreateDirectory(_repositoryPath);
        }
    }
    
    static ResourceHelper()
    {
        CheckRepository();
    }

    public static string[] GetAllFiles()
    {
        var allFiles = Directory.GetFiles(_repositoryPath, "*.*", SearchOption.AllDirectories);
        
        //Get Empty Directories also
        //var directories = Directory.GetDirectories(_repositoryPath, "*.*", SearchOption.AllDirectories);

        return allFiles;
    }
    
}