using System;

namespace MimiTools.Messenger.Messages
{
    public sealed class DataMessage : IMessage
    {
        private readonly byte[] _data;

        public DataMessage(byte code, byte[] data)
        {
            Code = code;
            _data = data.Clone() as byte[];
        }

        public byte Code { get; }

        public byte[] AsByteArray()
            => _data.Clone() as byte[];

        public string AsString()
            => Convert.ToBase64String(_data);
    }
}
