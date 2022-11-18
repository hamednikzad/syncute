namespace SynCute.Common.Messages.Behavioral;

public class PingMessage : Message
{
    public const string CommandName = "ping";
    
    public PingMessage() : base(CommandName)
    {
        
    }
}