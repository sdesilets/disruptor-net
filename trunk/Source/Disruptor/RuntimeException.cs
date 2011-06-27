using System;
using System.Runtime.Serialization;

namespace Disruptor
{
    [Serializable]
    public class RuntimeException : Exception
    {
        public RuntimeException()
        {
        }

        public RuntimeException(string message) : base(message)
        {
        }

        public RuntimeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RuntimeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}