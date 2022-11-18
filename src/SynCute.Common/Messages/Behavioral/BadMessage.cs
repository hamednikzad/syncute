namespace SynCute.Common.Messages.Behavioral;

public class BadMessage : Message
{
    public const string CommandName = "bad_message";
    public string Message { get; }
    
    public BadMessage(string message) : base(CommandName)
    {
        Message = message;
    }
}