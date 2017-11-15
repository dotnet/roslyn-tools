// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace System.Threading.Tasks
{
    public static class SingleThreadExecutor
    {
        public static void ExecuteTask(Task task)
        {
            var previousSynchronizationContext = SynchronizationContext.Current;
            try
            {
                using (var singleThreadedSynchronizationContext = new SingleThreadSynchronizationContext())
                {
                    SynchronizationContext.SetSynchronizationContext(singleThreadedSynchronizationContext);
                    task = task.ContinueWith(delegate { singleThreadedSynchronizationContext.Complete(); }, TaskScheduler.Default);
                    singleThreadedSynchronizationContext.RunOnCurrentThread();
                    task.GetAwaiter().GetResult();
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }

        public static async Task ExecuteTaskAsync(Task task)
        {
            var previousSynchronizationContext = SynchronizationContext.Current;
            try
            {
                using (var singleThreadedSynchronizationContext = new SingleThreadSynchronizationContext())
                {
                    SynchronizationContext.SetSynchronizationContext(singleThreadedSynchronizationContext);
                    task = task.ContinueWith(delegate { singleThreadedSynchronizationContext.Complete(); }, TaskScheduler.Default);
                    singleThreadedSynchronizationContext.RunOnCurrentThread();
                    await task.ConfigureAwait(false);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }
    }
}
