## Differences between the Java and .NET version of the Disruptor ##

The main differences comes from the fact that .NET supports structs (value types), Java doesn't.

In Java, every entry (or message) exchanged by the disruptor needs to inherit from a base class called `AbstractEntry`. This class exposes the Sequence number required by the disruptor to process the messages, it is basically a header for the message.

In .NET we have replaced `AbstractEntry` by a generic struct: `Entry<T>`. An entry contains 2 fields: the sequence number and a field called data, used to store the message (of type T). Using this struct has several advantages:
  * the array in the `RingBuffer` is of type `Entry<T>`, when we need to access the Sequence number we don't have to dereference (`Entry<T>` is a struct so instances are directly nested in the array) and this improves [cache spatial locality](http://en.wikipedia.org/wiki/Locality_of_reference).
  * your message types do not need to implement or inherit from a base class, you can use [POCOs](http://en.wikipedia.org/wiki/Plain_Old_CLR_Object).