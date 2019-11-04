using System;

namespace MimiTools.Messenger
{
    public interface IMessenger : IDisposable, IMessageReader, IMessageWriter
    {
        event Func<IMessage> MessageReceived;
    }
}
