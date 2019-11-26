namespace Abbotware.Dataflow
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public static class Program
    {
        public static async Task Main()
        {
            var sum = 0;

            var edbo = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 1,
                MaxDegreeOfParallelism = 1,
            };

            var lo = new DataflowLinkOptions
            {
                PropagateCompletion = true,
            };

            var third = new TransformBlock<int, string>(x =>
            {
                return x.ToString();
            }, edbo);


            // ctor requirement is not desirable, but 'works'
            var second = new AggregationBlock<int, int>(x => Interlocked.Add(ref sum, x), () => sum, third);

            var first = new TransformBlock<int, int>(x => x + 2, edbo);

            first.LinkTo(second, lo);

            var last = new ActionBlock<string>(x => Console.WriteLine(x), edbo);

            // this is preferred:
            //second.LinkTo(third);

            third.LinkTo(last, lo);

            await first.SendAsync(5);
            await first.SendAsync(1);

            first.Complete();

            await last.Completion;
        }
    }
}