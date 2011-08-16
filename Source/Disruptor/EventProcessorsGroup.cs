namespace Disruptor
{
    ///<summary>
    ///  A group of <see cref="IEventProcessor"/> set up via the <see cref="RingBuffer{T}"/>
    ///</summary>
    ///<typeparam name="T">the type of event used by the eventProcessors.</typeparam>
    internal class EventProcessorsGroup<T> : IEventProcessorsGroup<T>
    {
        private readonly IEventProcessorBuilder<T> _ringBuffer;
        private readonly IEventProcessor[] _eventProcessors;

        internal EventProcessorsGroup(IEventProcessorBuilder<T> ringBuffer, IEventProcessor[] eventProcessors)
        {
            _ringBuffer = ringBuffer;
            _eventProcessors = eventProcessors;
        }
        
        public IEventProcessorsGroup<T> Then(params IEventHandler<T>[] eventHandlers)
        {
            return ConsumeWith(eventHandlers);
        }

        public IEventProcessorsGroup<T> ConsumeWith(IEventHandler<T>[] eventHandlers)
        {
            return _ringBuffer.CreateEventProcessors(_eventProcessors, eventHandlers);
        }
    }
}
