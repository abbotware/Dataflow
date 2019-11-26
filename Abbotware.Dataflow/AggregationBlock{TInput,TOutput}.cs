namespace Abbotware.Dataflow
{
    using System;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    public class AggregationBlock<TInput, TOutput> : ITargetBlock<TInput>
    {
        private readonly ITargetBlock<TInput> consumer;

        private readonly ITargetBlock<TOutput> target;

        public AggregationBlock(Action<TInput> input, Func<TOutput> output, ITargetBlock<TOutput> target)
            : this((x) => { input(x); return Task.CompletedTask; }, () => { return Task.FromResult(output()); }, target)
        {
        }

        public AggregationBlock(Func<TInput, Task> input, Func<Task<TOutput>> output, ITargetBlock<TOutput> target)
        {
            this.target = target;

            var edbo = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = 1,
                MaxDegreeOfParallelism = 1,
            };

            consumer = new ActionBlock<TInput>(input, edbo);

            var t = consumer.Completion.ContinueWith(async (x) =>
            {
                await target.SendAsync(await output());
                target.Complete();
            });
        }

        public Task Completion => consumer.Completion;

        public void Complete() => consumer.Complete();

        void IDataflowBlock.Fault(Exception exception) => consumer.Fault(exception);

        DataflowMessageStatus ITargetBlock<TInput>.OfferMessage(DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            return consumer.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }
    }
}