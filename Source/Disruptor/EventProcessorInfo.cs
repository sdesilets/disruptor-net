namespace Disruptor
{
    internal class EventProcessorInfo<T>
    {
        public EventProcessorInfo(IEventProcessor eventProcessor, IEventHandler<T> eventHandler)
        {
            EventProcessor = eventProcessor;
            EventHandler = eventHandler;
            IsEndOfChain = true;
        }

        public bool IsEndOfChain { get; private set; }
        public IEventProcessor EventProcessor { get; private set; }
        public IEventHandler<T> EventHandler { get; private set; }

        public void UsedInBarrier()
        {
            IsEndOfChain = false;
        }
    }
}
