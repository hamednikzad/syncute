namespace SynCute.Core.Messages;

public class Message
{
    public string Command { get; init; }
    public List<string> Meta { get; init; }
    public string Content { get; init; }

    public Message()
    {
    }
    
    protected Message(string command)
    {
        Command = command;
    }
    
    
    public string GetJson()
    {
        throw new NotImplementedException();
    }
}