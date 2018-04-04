using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CouchbaseDocumentExpirySetter
{
    public static class TaskHelper
    {
        public static async Task WaitAllThrottledAsync(IEnumerable<Task> tasksToRun, int maxTasksToRunInParallel, int timeoutInMilliseconds = -1, CancellationToken cancellationToken = new CancellationToken())
        {
            var tasks = tasksToRun.ToList();

            using (var throttler = new SemaphoreSlim(maxTasksToRunInParallel))
            {
                var postTaskTasks = new List<Task>();

                tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => throttler.Release())));

                foreach (var task in tasks)
                {
                    await throttler.WaitAsync(timeoutInMilliseconds, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!task.IsCompleted) task.Start();
                }

                await Task.WhenAll(postTaskTasks.ToArray());
            }
        }
    }
}
