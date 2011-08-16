using System.Collections.Generic;
using System.Linq;

namespace Disruptor
{
    internal class EventProcessorRepository<T>
    {
        private readonly IDictionary<IEventHandler<T>, EventProcessorInfo<T>> _eventProcessorInfoByHandler = new Dictionary<IEventHandler<T>, EventProcessorInfo<T>>();
        private readonly IDictionary<IEventProcessor, EventProcessorInfo<T>> _eventProcessorInfoByEventProcessor = new Dictionary<IEventProcessor, EventProcessorInfo<T>>();

        public void Add(IEventProcessor eventProcessor, IEventHandler<T> eventHandler)
        {
            var eventProcessorInfo = new EventProcessorInfo<T>(eventProcessor, eventHandler);
            _eventProcessorInfoByHandler[eventHandler] = eventProcessorInfo;
            _eventProcessorInfoByEventProcessor[eventProcessor] = eventProcessorInfo;
        }

        public IEventProcessor[] LastEventProcessorsInChain
        {
            get
            {
                return (from eventProcessorInfo in _eventProcessorInfoByHandler.Values
                        where eventProcessorInfo.IsEndOfChain
                        select eventProcessorInfo.EventProcessor).ToArray();
            }
        }
        
        public void UnmarkEventProcessorsAsEndOfChain(IEnumerable<IEventProcessor> eventProcessors)
        {
            foreach (var eventProcessor in eventProcessors)
            {
                _eventProcessorInfoByEventProcessor[eventProcessor].UsedInBarrier();
            }
        }

        public IEventProcessor GetEventProcessorFor(IEventHandler<T> eventHandler)
        {
            return _eventProcessorInfoByHandler[eventHandler].EventProcessor;
        }

        public IEnumerable<EventProcessorInfo<T>> EventProcessors
        {
            get { return _eventProcessorInfoByHandler.Values; }
        }
    }
}
