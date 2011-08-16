namespace Disruptor
{
    internal interface IEventProcessorBuilder<T>
    {
        EventProcessorsGroup<T> CreateEventProcessors(IEventProcessor[] barrierEventProcessors, IEventHandler<T>[] eventHandlers);
    }
}