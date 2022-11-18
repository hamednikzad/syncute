using System.Text;

namespace SynCute.Common.Helpers;

public static class ArrayHelper
{
    public static byte[] GetByteArray(int input)
    {
        var result = BitConverter.GetBytes(input);

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(result);
        }
        
        return result;
    }
    
    public static byte[] GetByteArray(string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }
}