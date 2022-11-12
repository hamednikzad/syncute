namespace SynCute.Core.Messages;

public class Message
{
    public string Type { get; }
    public List<string> Meta { get; protected set; }
    public string Content { get; protected set; }

    protected Message(string type)
    {
        Type = type;
    }
    
    public string GetJson()
    {
        throw new NotImplementedException();
    }
}