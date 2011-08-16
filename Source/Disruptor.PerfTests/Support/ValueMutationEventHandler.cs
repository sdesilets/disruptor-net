using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Disruptor.PerfTests.Support
{
    public class ValueMutationEventHandler : IEventHandler<ValueEvent>
    {
        private readonly Operation _operation;
        private long _value;
        private volatile bool _done;
        private readonly long _iterations;
#if DEBUG 
        private readonly string _path;
        private readonly IList<long> _lines = new List<long>(10*1000*1000);
#endif

        public ValueMutationEventHandler(Operation operation, long iterations)
        {
            _operation = operation;
            _iterations = iterations;
#if DEBUG 
            _path = Path.Combine(Environment.CurrentDirectory, operation + ".txt");
            File.Delete(_path);
#endif
        }

        public long Value
        {
            get { return Thread.VolatileRead(ref _value); }
        }

        public bool Done
        {
            get { return _done; }
        }

        public void OnNext(long sequence, ValueEvent data, bool endOfBatch)
        {
            _value = _operation.Op(_value, data.Value);

            if(sequence == _iterations - 1)
            {
                Thread.VolatileWrite(ref _value, _value);
                _done = true;
#if DEBUG 

                var sb = new StringBuilder();
                foreach (var line in _lines)
                {
                    sb.AppendLine(line.ToString());
                }

                File.WriteAllText(_path, sb.ToString());
#endif
            }
#if DEBUG
            _lines.Add(data.Value);
#endif
            }
    }
}
