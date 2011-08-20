using System.Collections.Generic;

namespace Disruptor
{
    internal interface IEventProcessorBuilder<T>
    {
        EventProcessorsGroup<T> CreateEventProcessors(IEnumerable<IEventProcessor> barrierEventProcessors, IEventHandler<T>[] eventHandlers);
    }
}