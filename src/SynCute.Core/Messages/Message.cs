namespace SynCute.Core.Messages;

public abstract class Message
{
    public string Type { get; }
    
    protected Message(string type)
    {
        Type = type;
    }
}