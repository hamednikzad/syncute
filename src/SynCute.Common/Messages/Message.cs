namespace SynCute.Common.Messages;

public abstract class Message
{
    public string Type { get; }
    
    protected Message(string type)
    {
        Type = type;
    }
}