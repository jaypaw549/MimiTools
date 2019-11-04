using System;
using System.Threading.Tasks;

namespace MimiTools.Messenger
{
    public interface IMessageWriter : IDisposable
    {
        void EnqueueMessage(IMessage message);

        void SendMessage(IMessage message);

        Task SendMessageAsync(IMessage message);
    }
}
