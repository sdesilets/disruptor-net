using System;

namespace Disruptor
{
    /// <summary>
    /// Used to alert consumers waiting at a <see cref="IConsumerBarrier{T}"/> of status changes.
    /// TODO It does not fill in a stack trace for performance reasons.
    /// </summary>
    [Serializable]
    public class AlertException : Exception
    {
        /// <summary>
        /// Pre-allocated exception to avoid garbage generation
        /// </summary>
        public static readonly AlertException Instance = new AlertException();

        /// <summary>
        /// Private constructor so only a single instance exists.
        /// </summary>
        private AlertException()
        {
        }

        //TODO
        /**
     * Overridden so the stack trace is not filled in for this exception for performance reasons.
     *
     * @return this instance.
     */
        /*@Override
        public Throwable fillInStackTrace()
        {
            return this;
        }
        }*/
    }
}