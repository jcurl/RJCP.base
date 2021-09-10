namespace RJCP.MSBuildTasks.Infrastructure.Threading.Tasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class AsyncValueTest
    {
        [Test]
        public void InitialValue()
        {
            AsyncValue<string> value = new AsyncValue<string>();
            Assert.That(value.Complete.WaitAsync().IsCompleted, Is.False);
        }

        [Test]
        public void GetSetValue()
        {
            AsyncValue<string> value = new AsyncValue<string>();
            Task<string> result = value.GetSetAsync(() => { return Task.FromResult("foo"); });
            Assert.That(result.IsCompleted, Is.True);
            Assert.That(result.Result, Is.EqualTo("foo"));
        }

        [Test]
        public async Task GetSetValueWait()
        {
            int funcCount = 0;
            AsyncValue<string> value = new AsyncValue<string>();
            Task<string> result1 = value.GetSetAsync(() => {
                funcCount++;
                return Task.Delay(50).ContinueWith((t) => { return "bar"; });
            });

            Assert.That(result1.IsCompleted, Is.False);

            Task<string> result2 = value.GetSetAsync(() => {
                funcCount++;
                return Task.Delay(50).ContinueWith((t) => { return "baz"; });
            });

            Assert.That(result2.IsCompleted, Is.False);

            // result1 and result2 should be running simultaneously. result1 is running, result2 sees this and waits
            // for result1. Only one of the functions are called (they're usually identical) and it should be result1.
            await Task.WhenAll(result1, result2);
            Assert.That(result1.Result, Is.EqualTo("bar"));
            Assert.That(result2.Result, Is.EqualTo("bar"));
            Assert.That(funcCount, Is.EqualTo(1));
        }

        [Test]
        [Timeout(10000)]
        public void GetSetAsyncException()
        {
            AsyncValue<string> value = new AsyncValue<string>();

            // Setting the value the first time should raise an exception.
            Assert.That(async () => {
                await value.GetSetAsync(async () => {
                    await Task.Delay(50);
                    throw new InvalidOperationException();
                });
            }, Throws.TypeOf<InvalidOperationException>());

            // Setting the value the second time, should raise an exception from the first result, regardless.
            Assert.That(async () => {
                await value.GetSetAsync(async () => {
                    await Task.Delay(50);
                    return string.Empty;
                });
            }, Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task GetThenSet()
        {
            AsyncValue<string> value = new AsyncValue<string>();
            Task<string> result1 = value.GetAsync();

            // THere are two ways to wait for the result, through GetAsync(), and getting the value there, or by
            // waiting on the Complete reset event and then reading the Value direct. Note, if the Value is set more
            // than once, there is no guarantee which value is returned by either GetAsync() or Value.
            Assert.That(result1.IsCompleted, Is.False);
            Assert.That(value.Complete.WaitAsync().IsCompleted, Is.False);

            value.Set("foo");
            Assert.That(result1.IsCompleted, Is.True);
            Assert.That(value.Complete.WaitAsync().IsCompleted, Is.True);
            Assert.That(result1.Result, Is.EqualTo("foo"));

            string result2 = await value.GetAsync();
            Assert.That(result2, Is.EqualTo("foo"));
        }

        [Test]
        [Timeout(2000)]
        public void GetSynchronousNotSet()
        {
            AsyncValue<string> value = new AsyncValue<string>();
            Task.Run(() => {
                Thread.Sleep(250);
                value.Set("Value");
            });

            string result = value.Get();
            Assert.That(result, Is.EqualTo("Value"));
        }

        [Test]
        [Timeout(2000)]
        public void GetSynchronousSetPrior()
        {
            AsyncValue<string> value = new AsyncValue<string>();
            value.Set("Value");

            string result = value.Get();
            Assert.That(result, Is.EqualTo("Value"));
        }

        [Test]
        [Timeout(2000)]
        public void GetSetSynchronous()
        {
            AsyncValue<string> value = new AsyncValue<string>();

            string result1 = null;
            string result2;

            int start = Environment.TickCount;
            Task bgTask = Task.Run(() => {
                result1 = value.GetSet(() => {
                    Thread.Sleep(400);
                    return "Value";
                });
            });

            Thread.Sleep(100);
            result2 = value.GetSet(() => {
                Thread.Sleep(1500);
                return "Value2";
            });

            // Measure the time. The first GetSet runs which is 500ms, so it should be about this amount of time. The
            // second GetSet sees that we're already executing the function, so will just wait and the lambda of 1000ms
            // shouldn't be executed. So we should be more than 400ms and less than 800ms.
            bgTask.Wait();
            int timeDiff = unchecked(Environment.TickCount - start);
            Assert.That(timeDiff, Is.GreaterThan(300).And.LessThan(700));

            // Because the first GetSet is run, this value wins.
            Assert.That(result1, Is.EqualTo("Value"));
            Assert.That(result2, Is.EqualTo("Value"));
        }
    }
}
