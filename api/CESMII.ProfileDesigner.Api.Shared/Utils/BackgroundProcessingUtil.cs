using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

namespace CESMII.ProfileDesigner.Api.Shared.Utils
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Reference: https://blog.elmah.io/async-processing-of-long-running-tasks-in-asp-net-core/
    /// </remarks>
    public class BackgroundWorkerQueue
    {
        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Reference: https://blog.elmah.io/async-processing-of-long-running-tasks-in-asp-net-core/
    /// </remarks>
    public class LongRunningService : BackgroundService
    {
        private readonly BackgroundWorkerQueue queue;

        public LongRunningService(BackgroundWorkerQueue queue)
        {
            this.queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await queue.DequeueAsync(stoppingToken);
                //await workItem(stoppingToken);
                _ = workItem(stoppingToken);
            }
        }
    }
}
