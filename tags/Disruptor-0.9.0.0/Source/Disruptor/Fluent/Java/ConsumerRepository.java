package net.symphonious.disrupter.dsl;

import com.lmax.disruptor.AbstractEntry;
import com.lmax.disruptor.BatchHandler;
import com.lmax.disruptor.Consumer;

import java.util.*;

class ConsumerRepository<T extends AbstractEntry> implements Iterable<ConsumerInfo<T>>
{
    private final Map<BatchHandler, ConsumerInfo<T>> consumerInfoByHandler = new IdentityHashMap<BatchHandler, ConsumerInfo<T>>();
    private final Map<Consumer, ConsumerInfo<T>> consumerInfoByConsumer = new IdentityHashMap<Consumer, ConsumerInfo<T>>();

    public void add(Consumer consumer, BatchHandler<T> handler)
    {
        final ConsumerInfo<T> consumerInfo = new ConsumerInfo<T>(consumer, handler);
        consumerInfoByHandler.put(handler, consumerInfo);
        consumerInfoByConsumer.put(consumer, consumerInfo);
    }

    public Consumer[] getLastConsumersInChain()
    {
        List<Consumer> lastConsumers = new ArrayList<Consumer>();
        for (ConsumerInfo<T> consumerInfo : consumerInfoByHandler.values())
        {
            if (consumerInfo.isEndOfChain())
            {
                lastConsumers.add(consumerInfo.getConsumer());
            }
        }
        return lastConsumers.toArray(new Consumer[lastConsumers.size()]);
    }

    public Consumer getConsumerFor(final BatchHandler<T> handler)
    {
        final ConsumerInfo consumerInfo = consumerInfoByHandler.get(handler);
        return consumerInfo != null ? consumerInfo.getConsumer() : null;
    }

    public void unmarkConsumersAsEndOfChain(final Consumer... barrierConsumers)
    {
        for (Consumer barrierConsumer : barrierConsumers)
        {
            consumerInfoByConsumer.get(barrierConsumer).usedInBarrier();
        }
    }

    public Iterator<ConsumerInfo<T>> iterator()
    {
        return consumerInfoByHandler.values().iterator();
    }

}
