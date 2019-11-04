using System.Text;

namespace MimiTools.Messenger.Messages
{
    public class StringMessage : IMessage
    {
        private readonly string _content;

        public StringMessage(byte code, string content)
        {
            Code = code;
            _content = content.Clone() as string;
        }

        public byte Code { get; }

        public byte[] AsByteArray()
            => Encoding.Unicode.GetBytes(_content);

        public string AsString()
            => _content.Clone() as string;
    }
}
