namespace YBot.Command.Abstracts;

public interface IMessage
{
    string   Room    { get; }
    string   Sender  { get; }
    string   Message { get; }
    string   UserId  { get; }
    string[] Tag     { get; }
    DateTime Time    { get; }
    string   Role    { get; }
    string   Color   { get; }
    string?  Prefix  { get; set; }

    /// <summary>/cmd a b c => "a b c"</summary>
    string RawArgument { get; }

    /// <summary>/cmd a b c => "/cmd"</summary>
    string Command { get; }

    string PureCommand { get; }

    /// <summary>/cmd a b c => [a, b, c]</summary>
    string[] Arguments { get; }
}