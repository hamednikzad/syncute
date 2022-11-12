namespace SynCute.Core.Messages;

public class Message
{
    public string Type { get; protected set; }
    public List<string> Meta { get; protected set; }
    public byte[] Content { get; protected set; }

    public string GetJson()
    {
        throw new NotImplementedException();
    }
}