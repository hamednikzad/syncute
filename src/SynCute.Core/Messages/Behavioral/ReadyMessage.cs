namespace SynCute.Core.Messages.Behavioral;

public class ReadyMessage : Message
{
    public const string CommandName = "ready";
    
    public ReadyMessage() : base(CommandName)
    {
        
    }
}