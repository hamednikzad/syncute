namespace SynCute.Common.Messages.Behavioral;

public class ReadyMessage : Message
{
    public const string CommandName = "ready";
    
    public ReadyMessage() : base(CommandName)
    {
        
    }
}