using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Pragma.SignalBus;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace UnitTests
{
    public class UnitTest
    {
        private bool _methodHandlerHit;
        private int _countMethodInvoked;
        
        [SetUp]
        public void Initialize()
        {
            _methodHandlerHit = false;
            _countMethodInvoked = 0;
        }

        private void TestMethodHandler(Signal signal)
        {
            Assert.AreEqual("Signal", signal.Name);
            Assert.AreEqual(1, signal.Identifier);
            _methodHandlerHit = true;
        }

        private void TestMethodHandlerFail(Signal signal)
        {
            Assert.Fail();
        }

        [Test]
        public void RegisterAndInvoke()
        {
            var eventBus = new SignalBus();
            eventBus.Register<Signal>(TestMethodHandler);
            
            eventBus.Send(new Signal{Name = "Signal", Identifier = 1});
            Assert.IsTrue(_methodHandlerHit);
        }
        
        [Test]
        public void RegisterAndInvokeLambda()
        {
            var eventBus = new SignalBus();
            
            eventBus.Register<Signal>(s =>
            {
                Assert.AreEqual("Signal 2", s.Name);
                Assert.AreEqual(2, s.Identifier);

                _methodHandlerHit = true;
            });
            
            eventBus.Send(new Signal {Name = "Signal 2", Identifier = 2});
            Assert.IsTrue(_methodHandlerHit);
        }
        
        [Test]
        public void DeregisterTest1()
        {
            var eventBus = new SignalBus();

            eventBus.Register<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);

            eventBus.Send(new Signal());
        }
        
        [Test]
        public void DeregisterTest2()
        {
            var eventBus = new SignalBus();

            eventBus.Register<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);

            eventBus.Send(new Signal());
        }
        
        [Test]
        public void RegisterTest2()
        {
            var eventBus = new SignalBus();

            eventBus.Register<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);
            eventBus.Register<Signal>(TestMethodHandlerFail);
            eventBus.Deregister<Signal>(TestMethodHandlerFail);

            eventBus.Send(new Signal());
        }
        
        [Test]
        public void DeregisterInMethodHandler()
        {
            var eventBus = new SignalBus();
            
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerCounter);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerCounter);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerCounter);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandler);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerHit);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerCounter);
            eventBus.Register<SignalRegister>(TestDeregisterInMethodHandlerCounter);

            eventBus.Send(new SignalRegister {signalBus = eventBus});

            Assert.AreEqual(5, _countMethodInvoked);
        }

        private void TestDeregisterInMethodHandler(SignalRegister signalRegister)
        {
            signalRegister.signalBus.Deregister<SignalRegister>(TestDeregisterInMethodHandlerHit);
        }
        
        private void TestDeregisterInMethodHandlerCounter(SignalRegister signalRegister)
        {
            _countMethodInvoked++;
        }
        
        private void TestDeregisterInMethodHandlerHit(SignalRegister signalRegister)
        {
        }

        [Test]
        public void PerformanceCachedTest()
        {
            var eventBus = new SignalBus();
            
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);

            var signal = new Signal { Name = "Signal", Identifier = 1 }; 

            var allocCounter = new AllocCounter();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000000; i++)
            {
                eventBus.Send(signal);
            }

            sw.Stop();
            var countAlloc = allocCounter.Stop();

            UnityEngine.Debug.Log($"PerformanceCached finished in {sw.ElapsedMilliseconds}ms | Count Alloc in {countAlloc - 1}");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1),
                $"PerformanceCached took {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"[DEBUG] PerformanceCached took {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        public void PerformanceNonCachedTest()
        {
            var eventBus = new SignalBus();
            
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);
            eventBus.Register<Signal>(PerformanceMethodHandler);

            var allocCounter = new AllocCounter();
            var sw = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000000; i++)
            {
                eventBus.Send(new Signal { Name = "Signal", Identifier = 1 });
            }
            
            sw.Stop();
            var countAlloc = allocCounter.Stop();

            UnityEngine.Debug.Log($"PerformanceNonCached finished in {sw.ElapsedMilliseconds}ms | Count Alloc in {countAlloc - 1}");
            Assert.IsTrue(sw.Elapsed < TimeSpan.FromSeconds(1),
                $"PerformanceNonCached took {sw.ElapsedMilliseconds}ms");

            Console.WriteLine($"[DEBUG] PerformanceNonCached took {sw.ElapsedMilliseconds}ms");
        }
        
        private void PerformanceMethodHandler()
        {
        }

        [Test]
        public void OrderTest()
        {
            var eventBus = new SignalBus();
            var callOrder = new List<int>();

            // Подписчики с зависимостями
            eventBus.Register<Signal>(() => callOrder.Add(1), owner: typeof(Handler1));
            eventBus.Register<Signal>(() => callOrder.Add(2), owner: typeof(Handler2),
                afterOrder: new[] { typeof(Handler1) });
            eventBus.Register<Signal>(() => callOrder.Add(3), owner: typeof(Handler3),
                beforeOrder: new[] { typeof(Handler2) });

            eventBus.Send<Signal>();

            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, callOrder);
        }

        [Test]
        public void LazySortingTest()
        {
            var eventBus = new SignalBus();
            var callOrder = new List<int>();
            
            eventBus.Register<Signal>(() => callOrder.Add(1), owner: typeof(Handler1), isLazySorted: true);
            eventBus.Register<Signal>(() => callOrder.Add(2), owner: typeof(Handler2),
                afterOrder: new[] { typeof(Handler1) }, isLazySorted: true);
            
            Assert.IsTrue(GetSubscriptionsForTest<Signal>(eventBus).Any(s => s.owner == typeof(Handler2)));

            eventBus.Send<Signal>();
            CollectionAssert.AreEqual(new[] { 1, 2 }, callOrder);
        }

        [Test]
        public void CyclicDependencyThrowsOnSendTest()
        {
            var eventBus = new SignalBus();
            
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler1), 
                beforeOrder: new[] { typeof(Handler2) },
                isLazySorted: true);
    
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler2), 
                beforeOrder: new[] { typeof(Handler1) },
                isLazySorted: true);
            
            Assert.Throws<InvalidOperationException>(() => eventBus.Send<Signal>());
        }

        [Test]
        public void CyclicDependencyThrowsImmediatelyTest()
        {
            var eventBus = new SignalBus();
    
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler1), 
                beforeOrder: new[] { typeof(Handler2) });
            
            Assert.Throws<InvalidOperationException>(() => 
            {
                eventBus.Register<Signal>(() => { }, 
                    owner: typeof(Handler2), 
                    beforeOrder: new[] { typeof(Handler1) });
            });
        }
        
        [Test]
        public void ComplexCyclicDependencyThrowsTest()
        {
            var eventBus = new SignalBus();
    
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler1), 
                beforeOrder: new[] { typeof(Handler3) },
                isLazySorted: true);
    
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler2), 
                beforeOrder: new[] { typeof(Handler1) },
                isLazySorted: true);
    
            eventBus.Register<Signal>(() => { }, 
                owner: typeof(Handler3), 
                beforeOrder: new[] { typeof(Handler2) },
                isLazySorted: true);
    
            // Cycle: Handler1 → Handler3 → Handler2 → Handler1
            Assert.Throws<InvalidOperationException>(() => eventBus.Send<Signal>());
        }

        [Test]
        public void RegisterDuringSendTest()
        {
            var eventBus = new SignalBus();
            var lateHandlerCalled = false;

            eventBus.Register<Signal>(() => { eventBus.Register<Signal>(() => lateHandlerCalled = true); });

            eventBus.Send<Signal>();
            eventBus.Send<Signal>();

            Assert.IsTrue(lateHandlerCalled);
        }

        [Test]
        public void SendWithoutParametersTest()
        {
            var eventBus = new SignalBus();
            var handlerCalled = false;

            eventBus.Register<Signal>(() => handlerCalled = true);
            eventBus.Send<Signal>();

            Assert.IsTrue(handlerCalled);
        }
        
        [Test]
        public void SendFromPool()
        {
            var eventBus = new SignalBus();

            eventBus.Register<Signal>(TestMethodHandler);
            eventBus.FromPool<Signal>().Set("Signal", 1).Send();

            Assert.IsTrue(_methodHandlerHit);
        }

        private List<Subscription> GetSubscriptionsForTest<TSignal>(SignalBus bus)
        {
            var field = typeof(SignalBus).GetField("_subscriptions", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field == null)
                throw new InvalidOperationException("Field '_subscriptions' not found");
            
            var subscriptionsDict = (Dictionary<Type, List<Subscription>>)field.GetValue(bus);
            
            if (subscriptionsDict.TryGetValue(typeof(TSignal), out var subscriptions))
            {
                return subscriptions;
            }
            
            return new List<Subscription>();
        }
    }

    public static class SignalExtension
    {
        public static SignalWrapper<Signal> Set(this SignalWrapper<Signal> signalWrapper, string name, int identifier)
        {
            signalWrapper.Signal.Name = name;
            signalWrapper.Signal.Identifier = identifier;

            return signalWrapper;
        }
    }
    
    public class Signal
    {
        public string Name { get; set; }
        public int Identifier { get; set; }
    }
        
    public class SignalRegister
    {
        public SignalBus signalBus;
    }
    
    public class Handler1
    {
    }

    public class Handler2
    {
    }

    public class Handler3
    {
    }

    public class AllocCounter {
        private Recorder _rec;

        public AllocCounter() {
            _rec = Recorder.Get("GC.Alloc");

            // The recorder was created enabled, which means it captured the creation of the
            // Recorder object itself, etc. Disabling it flushes its data, so that we can retrieve
            // the sample block count and have it correctly account for these initial allocations.
            _rec.enabled = false;

#if !UNITY_WEBGL
            _rec.FilterToCurrentThread();
#endif

            _rec.enabled = true;
        }

        public int Stop() {
            if (_rec == null) {
                throw new Exception("AllocCounter already stopped");
            }

            _rec.enabled = false;

#if !UNITY_WEBGL
            _rec.CollectFromAllThreads();
#endif

            var res = _rec.sampleBlockCount;
            _rec = null;
            return res;
        }

        public static int Instrument(Action action) {
            var counter = new AllocCounter();
            int allocs;
            try {
                action();
            } finally {
                allocs = counter.Stop();
            }

            return allocs;
        }
    }
}
