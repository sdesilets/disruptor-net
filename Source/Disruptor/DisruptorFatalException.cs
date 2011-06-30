using System;
using System.Runtime.Serialization;

namespace Disruptor
{
    ///<summary>
    /// Exceptions handled by <see cref="FatalExceptionHandler{T}"/> are wrapped into a <see cref="DisruptorFatalException"/> before beeing thrown
    ///</summary>
    [Serializable]
    public class DisruptorFatalException : Exception
    {
        /// <summary>
        /// Initialize a <see cref="DisruptorFatalException"/>
        /// </summary>
        public DisruptorFatalException()
        {
        }

        ///<summary>
        /// Initialize a <see cref="DisruptorFatalException"/> with a specified error message.
        ///</summary>
        ///<param name="message">The message that describe the error.</param>
        public DisruptorFatalException(string message) : base(message)
        {
        }

        ///<summary>
        /// Initialize a <see cref="DisruptorFatalException"/> with a specified error 
        /// message and a reference to the inner exception that is the cause of this exception.
        ///</summary>
        ///<param name="message">The message that describe the error.</param>
        ///<param name="inner">reference to the inner exception that is the cause of this exception.</param>
        public DisruptorFatalException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DisruptorFatalException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}