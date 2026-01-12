using System;
using System.Diagnostics;
using NUnit.Framework;

namespace PragmaSignalBus.Tests
{
    public class TestsPerformance
    {
        [Test]
        public void NaiveSendPerformanceTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestMethodHandler);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                signalBus.Send(new TestSignal {Name = "Custom Signal", Identifier = 1});
            }

            sw.Stop();

            UnityEngine.Debug.Log($"Finished default in {sw.ElapsedMilliseconds}ms");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1),
                $"NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"[DEBUG] NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        public void NaiveSendEmptyPerformanceTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestMethodHandlerEmpty);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                signalBus.Send<TestSignal>();
            }

            sw.Stop();

            UnityEngine.Debug.Log($"Finished empty in {sw.ElapsedMilliseconds}ms");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1),
                $"NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"[DEBUG] NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        public void NaiveSendUnsafeEmptyPerformanceTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestMethodHandlerEmpty);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                signalBus.SendUnsafe<TestSignal>();
            }

            sw.Stop();

            UnityEngine.Debug.Log($"Finished Unsafe in {sw.ElapsedMilliseconds}ms");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1),
                $"NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"[DEBUG] NaiveSendPerformanceTest took {sw.ElapsedMilliseconds}ms");
        }

        private void CustomTestMethodHandler(TestSignal testSignal)
        {
            // Do nothing
        }

        private void CustomTestMethodHandlerEmpty()
        {
            // Do nothing
        }
    }
}