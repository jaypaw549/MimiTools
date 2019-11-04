namespace MimiTools.Messenger
{
    public interface IMessage
    {
        byte Code { get; }

        byte[] AsByteArray();

        string AsString();
    }
}
