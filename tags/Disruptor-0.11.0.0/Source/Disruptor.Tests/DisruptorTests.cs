using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class DisruptorTests
    {
        [Test]
        public void Test()
        {
            //IClaimStrategyConfigurator claimStrategyConfigurator =
            //    DisruptorBuilder.Create<long>(Size);


        }
    }

    public static class DisruptorBuilder
    {

    }

    public interface IClaimStrategyConfigurator
    {

    }
}

/*
            const string FizzStep = "Fizz-C1";
            const string BuzzStep = "Buzz-C2";
            const string FizzBuzzStep = "FizzBuzz-C3";

            var disruptor = DisruptorBuilder
                        .Create<FizzBuzzEntry>(() => new FizzBuzzEntry(), Size)
                              .WithMultipleProducers()    //claim strategy
                              .WithYieldStrategy()        //wait strategy
                              .WithNewThreadPerConsumer() //different threading strategy
                        .ConsumeWith(FizzStep,
                                              (s,d) => { ....  }, // on available (s=sequence, d=data of type T)
                                              () => { ... }) // on end of batch

                        .InParallelWith(BuzzStep,
                                              (s,d) => { ....  }, // on available (s=sequence, d=data of type T)
                                              () => { ... }) // on end of batch

                               .After(FizzStep, BuzzStep)
                               .ConsumeWith(FizzBuzzStep, fizzBuzzBatchHandler) //we could as well provide a handler (IBatchHandler<T>) instead of lambdas (different overloads)

                        .HandleErrorsWith((ex, seq, data) => {});

            disruptor.StartConsumers(); // start consumer threads (threads named based on step name, to improve debugging experience)

            var producerBarrier = disruptor.ProducerBarrier; // automatically retrieves the last consumer of the graph (producer needs to track only the last consumer to prevent ring buffer wrapping)

            // use the producerBarrier to publish some stuff

            disruptor.Stop(); // halt all consumers

            var buzzBarrier = disruptor.GetConsumerBarrier(BuzzStep); // retrieve a consumer barrier, to change it state, etc 

*/