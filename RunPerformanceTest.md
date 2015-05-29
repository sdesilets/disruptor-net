To run the performance test on your hardware:
  * you will need the [.NET Framework 4.0](http://www.microsoft.com/net/download.aspx) to be installed first,
  * then download the latest version from the download section (Disruptor-perfRunner-X.Y.Z.A.zip)
  * extract the zip
  * open a cmd prompt and navigate to the folder where you extracted the package: `cd ExtractedFolder\Disruptor-perfRunner-1.0.0.0`
  * type `Disruptor.PerfTests 0 0` to run all the performance tests.

```
Disruptor.PerfTests.exe usage:

Usage: Disruptor.PerfTests Scenario Implementation Runs

ScenarioType options:
 - 0 (All)
 - 1 (UniCast1P1C)
 - 2 (MultiCast1P3C)
 - 3 (Pipeline3Step)
 - 4 (Sequencer3P1C)
 - 5 (DiamondPath1P3C)
 - 6 (Pipeline3StepLatency)
 - 7 (UniCast1P1CBatch)

ImplementationType options:
 - 0 (All)
 - 1 (Disruptor)
 - 2 (BlockingCollection)
 - 3 (Dataflow)

Runs: number of test run to do for each scenario and implementation

Example: Disruptor.PerfTests 1 1
will run UniCast1P1C performance test with the disruptor only.
```