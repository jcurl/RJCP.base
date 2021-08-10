namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class AsyncManualResetEventTest
    {
        [Test]
        public void DefaultResetState()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            Task t = mre.WaitAsync();
            Assert.That(t.IsCompleted, Is.False);
        }

        [Test]
        public void InitialResetStateNotSet()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(false);
            Task t = mre.WaitAsync();
            Assert.That(t.IsCompleted, Is.False);
        }

        [Test]
        public void InitialResetStatetSet()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);
            Task t = mre.WaitAsync();
            Assert.That(t.IsCompleted, Is.True);
        }

        [Test]
        public void SetManualResetEvent()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            Task t = mre.WaitAsync();
            Assert.That(t.IsCompleted, Is.False);
            mre.Set();
            Assert.That(t.IsCompleted, Is.True);
        }

        [Test]
        public void ResetManualResetEvent()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(true);
            Task t = mre.WaitAsync();
            Assert.That(t.IsCompleted, Is.True);

            // When resetting, already obtained tasks remain set. Only the next WaitAsync() will be not set.
            mre.Reset();
            Assert.That(t.IsCompleted, Is.True);

            Task t2 = mre.WaitAsync();
            Assert.That(t2.IsCompleted, Is.False);
        }

        [Test]
        public void MultipleWaiters()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent();
            Task t1 = mre.WaitAsync();
            Task t2 = mre.WaitAsync();
            Task t3 = mre.WaitAsync();

            Assert.That(t1.IsCompleted, Is.False);
            Assert.That(t2.IsCompleted, Is.False);
            Assert.That(t3.IsCompleted, Is.False);

            mre.Set();
            Assert.That(t1.IsCompleted, Is.True);
            Assert.That(t2.IsCompleted, Is.True);
            Assert.That(t3.IsCompleted, Is.True);
        }

        [Test]
        [Timeout(1000)]
        public void SynchronousWait()
        {
            AsyncManualResetEvent mre = new AsyncManualResetEvent(false);
            Task.Run(() => {
                Thread.Sleep(400);
                mre.Set();
            });
            mre.Wait();
        }
    }
}
