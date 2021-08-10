namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class AsyncSemaphoreTest
    {
        [Test]
        public void NegativeInitialCount()
        {
            Assert.That(() => {
                _ = new AsyncSemaphore(-1);
            }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ZeroInitialCountWait()
        {
            AsyncSemaphore sema = new AsyncSemaphore(0);
            Task t = sema.WaitAsync();
            Assert.That(t.IsCompleted, Is.False);
            sema.Release();
            Assert.That(t.IsCompleted, Is.True);
        }

        [Test]
        public void OneInitialCountWait()
        {
            AsyncSemaphore sema = new AsyncSemaphore(1);
            Task t1 = sema.WaitAsync();
            Assert.That(t1.IsCompleted, Is.True);
            sema.Release();
            Assert.That(t1.IsCompleted, Is.True);

            Task t2 = sema.WaitAsync();
            Assert.That(t2.IsCompleted, Is.True);

            Task t3 = sema.WaitAsync();
            Assert.That(t3.IsCompleted, Is.False);

            sema.Release();
            Assert.That(t3.IsCompleted, Is.True);
        }

        [Test]
        public void MultipleInitialCountWait()
        {
            List<Task> tasks = new List<Task>();
            AsyncSemaphore sema = new AsyncSemaphore(10);

            for (int i = 0; i < 11; i++) {
                tasks.Add(sema.WaitAsync());
            }

            for (int i = 0; i < 10; i++) {
                Assert.That(tasks[i].IsCompleted, Is.True);
            }
            Assert.That(tasks[10].IsCompleted, Is.False);

            sema.Release();
            Assert.That(tasks[10].IsCompleted, Is.True);
        }

        [Test]
        public void MultipleWaitOrder()
        {
            AsyncSemaphore sema = new AsyncSemaphore(0);
            Task t1 = sema.WaitAsync();
            Assert.That(t1.IsCompleted, Is.False);

            Task t2 = sema.WaitAsync();
            Assert.That(t2.IsCompleted, Is.False);

            Task t3 = sema.WaitAsync();
            Assert.That(t3.IsCompleted, Is.False);

            sema.Release();
            Assert.That(t1.IsCompleted, Is.True);
            Assert.That(t2.IsCompleted, Is.False);
            Assert.That(t3.IsCompleted, Is.False);

            sema.Release();
            Assert.That(t1.IsCompleted, Is.True);
            Assert.That(t2.IsCompleted, Is.True);
            Assert.That(t3.IsCompleted, Is.False);

            sema.Release();
            Assert.That(t1.IsCompleted, Is.True);
            Assert.That(t2.IsCompleted, Is.True);
            Assert.That(t3.IsCompleted, Is.True);
        }
    }
}
