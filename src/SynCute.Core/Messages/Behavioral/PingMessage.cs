namespace SynCute.Core.Messages.Behavioral;

public class PingMessage : Message
{
    public const string CommandName = "ping";
    
    public PingMessage() : base(CommandName)
    {
        
    }
}