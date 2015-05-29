## What is the disruptor? ##
Disruptor is a data construct used to pass messages between threads in an ordered maner (FIFO).
It is composed by:
**a ring buffer (array backed)** a set of barriers used to synchronise concurrent acces to the ring buffer. A producer barrier is used by one or more producer(s) to publish messages to the ring buffer and a consumer barrier notifies consumers when new data is available

## Is it performant? ##
The disruptor benefits of the following optimisations:
**no allocation during execution(message instances are pre-allocated when the ring buffer is initialised and recycled during execution)** CPU cache friendly: no false sharing, no contended writes and messages appears in sequencial order in memory, which improves pre-fetching
**performance tets, included in source code, show significant improvements compared to dtata structures available in .NET framework**

## How does it perform compared to .NET 4 concurrent collections? ##
The disruptor solution contains several performance tests focusing on throughput and latency.
Below are the results for throughput and latency tests run comparing [BlockingCollection](http://msdn.microsoft.com/en-us/library/dd267312.aspx) (.NET 4) and the disruptor

Throughput (best of 3 runs):
| |Blocking Collection (msg/sec)|Disruptor (msg/sec)|Ratio|
|:|:----------------------------|:------------------|:----|
|1 producer 1 consumer|2,340,823                    |26,737,967         |1100%|
|p -> c1 -> c2 -> c3|460,956                      |19,120,458         |4100%|

Latency (best of 3 runs):
| |Blocking Collection (nano sec)|Disruptor (nano sec)|Ratio|
|:|:-----------------------------|:-------------------|:----|
|Min Latency|533                           |107                 |498% |
|Mean Latency|1,409,187.85                  |404.28              |348,567%|
|99 percentile|16,777,216                    |512                 |3,276,800%|
|99.99 percentile|16,777,216                    |32,768              |51,200%|
|Max Latency|15,132,263                    |94,064              |16,087%|