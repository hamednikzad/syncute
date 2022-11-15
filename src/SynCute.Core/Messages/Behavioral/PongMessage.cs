namespace SynCute.Core.Messages.Behavioral;

public class PongMessage : Message
{
    public const string CommandName = "pong";
    
    public PongMessage() : base(CommandName)
    {
        
    }
}