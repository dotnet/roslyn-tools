// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Threading.Tasks
{
    public sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> queue = new();

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="callback">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            queue.Add(new KeyValuePair<SendOrPostCallback, object>(callback, state));
        }

        /// <summary>Not supported.</summary>
        /// <param name="callback">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Send(SendOrPostCallback callback, object state)
        {
            throw new NotSupportedException("Synchronously sending is not supported.");
        }

        /// <summary>return new instance.</summary>
        /// <returns>new SynchronizationContext</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new SingleThreadSynchronizationContext();
        }

        /// <summary>Runs an loop to process all queued work items.</summary>
        public void RunOnCurrentThread()
        {
            foreach (var workItem in queue.GetConsumingEnumerable())
            {
                workItem.Key(workItem.Value);
            }
        }

        /// <summary>Notifies the context that no more work will arrive.</summary>
        public void Complete()
        {
            queue.CompleteAdding();
        }

        public void Dispose()
        {
            queue.Dispose();
        }
    }
}
