using System;
using System.Threading.Tasks;

namespace MimiTools.Messenger
{
    public interface IMessageReader : IDisposable
    {
        IMessage GetMessage();

        Task<IMessage> GetMessageAsync();

        bool TryGetMessage(out IMessage message);
    }
}
