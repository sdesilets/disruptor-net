# Update 24/12/2011 #
Disruptor-net moved to Github, you will find the latest versions of the code here: https://github.com/odeheurles/Disruptor-net

The Github repository contains:
  * equivalent of version 2.7.1 of the Java codebase (a few minor things missings but should be available soon)
  * a new task scheduler that can be used to run the Disruptor with processor affinity
  * a new Atomic assembly containing an equivalent of the [Java Atomic package](http://docs.oracle.com/javase/1.5.0/docs/api/java/util/concurrent/atomic/package-summary.html): (under development)

## The SVN repository available on this site will no longer be maintained. ##

# Disruptor-net #

The disruptor is a concurrency component used to exchange messages between threads ([producer consumer](http://en.wikipedia.org/wiki/Producer-consumer_problem))
It is optimised for high throughput and low latency scenarios.

This component was initially developed in Java by the LMAX team and we ported it to .NET.

Before going any further, we recommand to read their [technical paper](http://disruptor.googlecode.com/files/Disruptor-1.0.pdf), which explains why they decided to develop this component and why it is so fast. This [video](http://www.infoq.com/presentations/LMAX) is as well a very good introduction to the disruptor.

### Disruptor internals ###
If you want to understand how the disruptor is designed, the LMAX team wrote several [articles and blogs](http://code.google.com/p/disruptor/wiki/BlogsAndArticles) which are perfectly valid for the .NET version as well.

Differences between Java and .NET implementations are highlighted in the wiki, [here](http://code.google.com/p/disruptor-net/wiki/DifferencesJavaDotNet).

### Using the disruptor ###
Beta versions of the .NET Disruptor are available in the [Download section](http://code.google.com/p/disruptor-net/downloads/list) and you can see the major list of changes [here](http://code.google.com/p/disruptor-net/wiki/ChangeLog).

To start you can have a look to the [code examples](http://code.google.com/p/disruptor-net/wiki/CodeExamples) or download the source code and look at the performance tests implementation for more advanced scenarios (all the scenarios available in the Java version have been ported as well).

### Questions? Ideas? Want to contribute to the project? ###
Have a look to our [discussion group](http://groups.google.com/group/disruptor-net).