namespace SynCute.Core.Messages;

public class Message
{
    public string Command { get; }
    public List<string> Meta { get; protected set; }
    public string Content { get; protected set; }

    protected Message(string command)
    {
        Command = command;
    }
    
    public string GetJson()
    {
        throw new NotImplementedException();
    }
}