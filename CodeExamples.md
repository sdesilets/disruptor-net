## Code example ##

The code below is an example of a single producer and single consumer using the convenience interface `IBatchHandler<T>` for implementing a consumer.
The consumer runs on a separate thread receiving entries as they become available.

Consumers implement this interface for convenience.
```
public class ValueAdditionHandler:IBatchHandler<long>
{
    public long Sum { get; private set; }

    public void OnAvailable(long sequence, long value)
    {
        // process a new entry as it becomes available.
        _value += value;
    }

    public void OnEndOfBatch()
    {
        // useful for flushing results to an IO device if necessary.
    }
}
```

Define the type of message exchanged
```
public sealed class ValueEntry
{
    public long Value { get; set; }
}
```

Setup the `RingBuffer` and associated barriers.
```
ringBuffer = new RingBuffer<ValueEntry>(()=>new ValueEntry(), // ValueEntry factory
                                        1000, // Size of the RingBuffer (will be rounded to the next power of 2
                                        ClaimStrategyFactory.ClaimStrategyOption.SingleThreaded, // Single producer
                                        WaitStrategyFactory.WaitStrategyOption.Yielding);


handler = new ValueAdditionHandler(Iterations);
ringBuffer.ConsumeWith(_handler);

producerBarrier = _ringBuffer.CreateProducerBarrier();
	
//run the consumer in a new Thread
ringBuffer.StartConsumers();
```

Publish messages to the disruptor
```
ValueEntry data;
long sequence = _producerBarrier.NextEntry(out data);

data.Value = 123L;

producerBarrier.Commit(sequence);
```

To tear down the `RingBuffer` and stop consumer(s) thread(s):
```
ringBuffer.Halt();
```