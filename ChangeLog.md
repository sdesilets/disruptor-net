### v1.0 ###
  * New API to configure the disruptor (look at the performance tests to see some samples)
  * Removed value type ring buffer
  * Improved significantly the perf test runner (better reporting, adjusted duration of tests, print additional system informations, print warnings if insuficient cores or HT is turned on)

### 10th July - v0.7 ###
  * Add batching support on producer barrier
  * Setup continuous build and new build targets to generate packages

### 8th July ###
  * completly restructured performance test projects
  * command line options to select test scenarios, implementation, number of runs and number of iterations (num of iterations does not
work yet)
  * generate a html report at the end of the test run

### 5th July 2011 ###
  * Added Microsoft TPL Dataflow comparison to Uni and Pipe perf tests
  * Ported all remaining performance tests. All Java code has now been ported.
  * Added a new constructor to `BatchConsumer` taking 2 actions: onAvailable and onEndOfBatch -> we no longer need to implement IBatchHandler for every use case.

### 4th July 2011 ###
  * ported latency test
  * ported latest disruptor (Java) changes
  * fixed bugs
  * performance (throughput) is now equivalent to the Java version

### 3rd July 2011 ###
  * removed `IEntry` and replaced it with struct `Entry<T>`: data processed by the disruptor no longer need to implement a specific interface
  * split the implementation in 2: `RingBuffer<T>` for reference type and `ValueTypeRingBuffer<T>` for value types.

### 28th June 2011 ###
  * automated build process
  * imported several .net performance test tools ([ClrProfiler](http://www.microsoft.com/download/en/details.aspx?id=16273), [Perf Monitor](http://bcl.codeplex.com/wikipage?title=PerfMonitor&referringTitle=Home)) and found one source of allocation. This is now fixed, the disruptor does not create any garbage once started.
  * first perf test ported (1 producer, 1 consumer): first results show about 9-10 million msg/sec on my dual core i7 @ 2.67GHz
  * perf test does not stop properly all the time, to investigate...

### 27th June 2011 ###
  * code base: all classes have been ported except Histogram.java
  * unit tests: all unit tests have been ported but 2 are failing and need to be fixed.
  * perf tests: TODO