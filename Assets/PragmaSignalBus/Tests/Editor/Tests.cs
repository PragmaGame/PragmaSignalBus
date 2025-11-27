using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace PragmaSignalBus.Tests
{
    public class Tests
    {
        private bool _methodHandlerHit;
        private bool _actionHandlerHit;

        [SetUp]
        public void Initialize()
        {
            _methodHandlerHit = false;
            _actionHandlerHit = false;
        }

        [Test]
        public void RegisterAndSendCustomSignalMethodTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestSignalMethodHandler);

            Assert.IsFalse(_methodHandlerHit);
            signalBus.Send(new TestSignal {Name = "Custom Signal", Identifier = 1});
            Assert.IsTrue(_methodHandlerHit);
        }

        [Test]
        public void RegisterAndSendCustomSignalActionTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(s =>
            {
                Assert.AreEqual("Custom signal 2", s.Name);
                Assert.AreEqual(2, s.Identifier);

                _actionHandlerHit = true;
            });

            Assert.IsFalse(_actionHandlerHit);
            signalBus.Send(new TestSignal {Name = "Custom signal 2", Identifier = 2});
            Assert.IsTrue(_actionHandlerHit);
        }

        [Test]
        public void RegisterAndSendLambdaSignalTest()
        {
            var signalBus = new SignalBus();
            signalBus.Register<PayloadSignal<int>>(s =>
            {
                Assert.AreEqual(999, s.Payload);
                _actionHandlerHit = true;
            });

            Assert.IsFalse(_actionHandlerHit);
            signalBus.Send(new PayloadSignal<int>(999));
            Assert.IsTrue(_actionHandlerHit);
        }

        [Test]
        public void SendInCorrectOrderTest()
        {
            var signalBus = new SignalBus();

            List<TestSignal> customTestSignalResults = new List<TestSignal>();
            signalBus.Register<TestSignal>(s => { customTestSignalResults.Add(s); });

            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 1});
            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 2});
            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 3});
            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 4});
            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 5});
            signalBus.Send(new TestSignal {Name = "Custom Test Signal", Identifier = 6});

            Assert.AreEqual(6, customTestSignalResults.Count);
            Assert.AreEqual(1, customTestSignalResults[0].Identifier);
            Assert.AreEqual(2, customTestSignalResults[1].Identifier);
            Assert.AreEqual(3, customTestSignalResults[2].Identifier);
            Assert.AreEqual(4, customTestSignalResults[3].Identifier);
            Assert.AreEqual(5, customTestSignalResults[4].Identifier);
            Assert.AreEqual(6, customTestSignalResults[5].Identifier);
        }

        [Test]
        public void MultipleRegistersForSameSignalTest()
        {
            var signalBus = new SignalBus();
            int handler1Count = 0;
            int handler2Count = 0;

            signalBus.Register<TestSignal>(s => handler1Count++);
            signalBus.Register<TestSignal>(s => handler2Count++);

            signalBus.Send(new TestSignal());

            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);
        }
        
        [Test]
        public void DeregisterTestGeneralToken()
        {
            var signalBus = new SignalBus();
            var token = 1;

            signalBus.Register<TestSignal>(TestFailSignalMethodHandler, token: token);
            signalBus.Register<TestSignalWithSignalBusField>(Test2SignalMethodHandler, token: token);

            signalBus.Deregister(token);

            signalBus.Send(new TestSignalWithSignalBusField {IsFail = true});
        }
        
        [Test]
        public void DeregisterTestOutside()
        {
            var signalBus = new SignalBus();

            signalBus.Register<TestSignal>(TestFailSignalMethodHandler);
            signalBus.Deregister<TestSignal>(TestFailSignalMethodHandler);

            signalBus.Send(new TestSignal {Name = "Custom Signal 3", Identifier = 3});
        }
        
        [Test]
        public void DeregisterTestInside()
        {
            var signalBus = new SignalBus();

            signalBus.Register<TestSignalWithSignalBusField>(Test3SignalMethodHandler);

            signalBus.Send(new TestSignalWithSignalBusField{SignalBus = signalBus, IsFail = false, Token = null});
            signalBus.Send(new TestSignalWithSignalBusField{SignalBus = null, IsFail = true, Token = null});
        }
        
        [Test]
        public void DeregisterTokenTestOutside()
        {
            var signalBus = new SignalBus();
            var token = 1;

            signalBus.Register<TestSignal>(TestFailSignalMethodHandler, token: token);
            signalBus.Deregister(token);

            signalBus.Send(new TestSignal());
        }
        
        [Test]
        public void DeregisterTokenTestInside()
        {
            var signalBus = new SignalBus();

            var token = signalBus.Register<TestSignalWithSignalBusField>(Test2SignalMethodHandler);

            signalBus.Send(new TestSignalWithSignalBusField() {SignalBus = signalBus, IsFail = false, Token = token});
            signalBus.Send(new TestSignalWithSignalBusField() {SignalBus = null, IsFail = true, Token = null});
        }
        
        [Test]
        public void DeregisterLambdaTest()
        {
            var signalBus = new SignalBus();
            var token = 1;

            signalBus.Register<TestSignal>(s =>
            {
                Assert.Fail();
            }, token: token);
            
            signalBus.Deregister(token);
            
            signalBus.Send(new TestSignal());
        }

        [Test]
        public void DeregisterDontThrowIfDoesntExistTest()
        {
            var eventBus = new SignalBus();
            eventBus.Register<TestSignal>(CustomTestThrowSignalMethodHandler);

            eventBus.Deregister<TestSignal>(CustomTestThrowSignalMethodHandler);
            eventBus.Deregister<TestSignal>(CustomTestThrowSignalMethodHandler);
            eventBus.Deregister<TestSignal>(CustomTestThrowSignalMethodHandler);
            eventBus.Deregister<TestSignal>(CustomTestThrowSignalMethodHandler);

            eventBus.Send(new TestSignal {Name = "Custom Signal 3", Identifier = 3});
        }

        private void CustomTestSignalMethodHandler(TestSignal testSignal)
        {
            Assert.AreEqual("Custom Signal", testSignal.Name);
            Assert.AreEqual(1, testSignal.Identifier);
            _methodHandlerHit = true;
        }

        private void CustomTestThrowSignalMethodHandler(TestSignal testSignal)
        {
            Assert.AreEqual("Custom Signal 3", testSignal.Name);
            Assert.AreEqual(3, testSignal.Identifier);

            throw new Exception("This should not be executed due to deregister.");
        }
        
        [Test]
        public async Task RegisterAndSendWithSignalDataShouldInvokeHandler()
        {
            var signalBus = new SignalBus();
            var eventHandled = false;
            var testSignal = new TestSignal { Name = "Test" };

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                Assert.AreEqual("Test", e.Name);
                eventHandled = true;
                await Task.CompletedTask;
            });
            
            await signalBus.SendAsync(testSignal);
            
            Assert.IsTrue(eventHandled);
        }

        [Test]
        public async Task RegisterAndSendWithoutSignalDataShouldInvokeHandler()
        {
            var signalBus = new SignalBus();
            var eventHandled = false;

            signalBus.Register<TestSignal>(async ct =>
            {
                eventHandled = true;
                await Task.CompletedTask;
            });
            
            await signalBus.SendAsync<TestSignal>();
            
            Assert.IsTrue(eventHandled);
        }
        
        [Test]
        public async Task SendSequenceInvocationShouldExecuteInOrder()
        {
            var signalBus = new SignalBus();
            var executionOrder = new List<int>();
            var test = new TestSignal();
            var tokenSource = new CancellationTokenSource();

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await UniTask.Delay(50, cancellationToken : ct);
                executionOrder.Add(1);
            }, new SortOptions(typeof(SortTest1)));
            
            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await UniTask.Delay(10, cancellationToken : ct);
                executionOrder.Add(2);
            }, new SortOptions(typeof(SortTest2), afterOrder: new []{typeof(SortTest1)}));

            await signalBus.SendAsync(test, asyncSendInvocationType: AsyncSendInvocationType.Sequence, token: tokenSource.Token);
            
            Assert.AreEqual(new List<int> { 1, 2 }, executionOrder);
        }
        
        [Test]
        public void OrderTest()
        {
            var signalBus = new SignalBus();
            var callOrder = new List<int>();
            
            signalBus.Register<TestSignal>(() => callOrder.Add(1), new SortOptions(typeof(SortTest1)));
            signalBus.Register<TestSignal>(() => callOrder.Add(2), new SortOptions(typeof(SortTest2), afterOrder: new[] { typeof(SortTest1) }));
            signalBus.Register<TestSignal>(() => callOrder.Add(3), new SortOptions(typeof(SortTest3), beforeOrder: new[] { typeof(SortTest2) }));

            signalBus.Send<TestSignal>();

            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, callOrder);
        }
        
        [Test]
        public void CyclicDependencyThrowsImmediatelyTest()
        {
            var signalBus = new SignalBus();
    
            signalBus.Register<TestSignal>(() => { }, 
                new SortOptions(typeof(SortTest1), 
                beforeOrder: new[] { typeof(SortTest2) }));
            
            Assert.Throws<InvalidOperationException>(() => 
            {
                signalBus.Register<TestSignal>(() => { }, 
                    new SortOptions(typeof(SortTest2), 
                    beforeOrder: new[] { typeof(SortTest1) }));
            });
        }
        
        [Test]
        public void ComplexCyclicDependencyThrowsTest()
        {
            var signalBus = new SignalBus();

            // Cycle: Handler1 → Handler3 → Handler2 → Handler1
            Assert.Throws<InvalidOperationException>(() =>
            {
                signalBus.Register<TestSignal>(() => { }, 
                    new SortOptions(typeof(SortTest1), 
                    beforeOrder: new[] { typeof(SortTest3) }));
    
                signalBus.Register<TestSignal>(() => { }, 
                    new SortOptions(typeof(SortTest2), 
                    beforeOrder: new[] { typeof(SortTest1) }));
    
                signalBus.Register<TestSignal>(() => { }, 
                    new SortOptions(typeof(SortTest3), 
                    beforeOrder: new[] { typeof(SortTest2) }));
            });
        }
        
        [Test]
        public async Task SendParallelInvocationShouldExecuteConcurrently()
        {
            var signalBus = new SignalBus();
            var signal = new TestSignal();
            var results = new List<int>();

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await UniTask.Delay(15, cancellationToken: ct);
                results.Add(2);
                await UniTask.Delay(15, cancellationToken: ct);
                results.Add(4);
            });

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await UniTask.Delay(10, cancellationToken: ct);
                results.Add(1);
                await UniTask.Delay(10, cancellationToken: ct);
                results.Add(3);
            });
            
            await signalBus.SendAsync(signal, asyncSendInvocationType: AsyncSendInvocationType.Parallel);
            
            Assert.AreEqual(new List<int>() {1, 2, 3, 4}, results);
        }

        [Test]
        public async Task DeregisterDuringSendShouldNotAffectCurrentSignal()
        {
            var signalBus = new SignalBus();
            var callCount = 0;
            object token = null;

            Func<TestSignal, CancellationToken, UniTask> handler = async (e, ct) =>
            {
                callCount++;
                signalBus.Deregister(token);
                await Task.CompletedTask;
            };

            token = signalBus.Register(handler);
            
            await signalBus.SendAsync(new TestSignal());
            await signalBus.SendAsync(new TestSignal());
            
            Assert.AreEqual(1, callCount, "Handler should be called once before deregister");
        }
        
        [Test]
        public async Task RegisterDuringSendShouldNotAffectCurrentSignal()
        {
            var signalBus = new SignalBus();
            var originalCallCount = 0;
            var newHandlerCallCount = 0;
            var signal = new TestSignal();

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                originalCallCount++;
                
                signalBus.Register<TestSignal>(async (e2, ct2) =>
                {
                    newHandlerCallCount++;
                    await Task.CompletedTask;
                });
                await Task.CompletedTask;
            });
            
            await signalBus.SendAsync(signal);
            
            Assert.AreEqual(1, originalCallCount);
            Assert.AreEqual(0, newHandlerCallCount, "New handler should not be called during current signal");
            
            await signalBus.SendAsync(signal);
            
            Assert.AreEqual(2, originalCallCount);
            Assert.AreEqual(1, newHandlerCallCount);
        }
        
        [Test]
        public async Task MultipleSignalTypesShouldNotInterfere()
        {
            var signalBus = new SignalBus();
            var testSignal1Count = 0;
            var testSignal2Count = 0;

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await UniTask.Delay(50, cancellationToken: ct);
                testSignal1Count++;
            });

            signalBus.Register<PayloadSignal<int>>(async (e, ct) =>
            {
                await UniTask.Delay(50, cancellationToken: ct);
                testSignal2Count++;
            });

            var task1 = signalBus.SendAsync(new TestSignal());
            var task2 = signalBus.SendAsync(new PayloadSignal<int>(2));

            await UniTask.WhenAll(task1, task2);
            
            Assert.AreEqual(1, testSignal1Count);
            Assert.AreEqual(1, testSignal2Count);
        }
        
        [Test]
        public void HandlerThrowsExceptionShouldPropagateException()
        {
            var signalBus = new SignalBus();
            
            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException();
            });
            
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await signalBus.SendAsync(new TestSignal()));
        }
        
        [Test]
        public async Task DeregisterWhenCancelToken()
        {
            var signalBus = new SignalBus();
            var cts = new CancellationTokenSource();
            var callCount = 0;

            Func<TestSignal, CancellationToken, UniTask> handler1 = (e, ct) =>
            {
                callCount++;
                return UniTask.CompletedTask;
            };
            
            Func<TestSignal, CancellationToken, UniTask> handler2 = async (e, ct) =>
            {
                signalBus.Deregister(handler1);
                await UniTask.Delay(500, cancellationToken: ct);
            };

            signalBus.Register(handler1);
            signalBus.Register(handler2);
            
            cts.CancelAfter(250);

            await signalBus.SendAsync(new TestSignal(), cts.Token).SuppressCancellationThrow();
            
            cts = new CancellationTokenSource();
            
            await signalBus.SendAsync(new TestSignal(), cts.Token).SuppressCancellationThrow();
            
            Assert.AreEqual(1, callCount, "Handler1 should be called once");
        }
        
        [Test]
        public async Task RegisterAndSendWithCancellationTokenShouldRespectCancellation()
        {
            var signalBus = new SignalBus();
            var cts = new CancellationTokenSource();

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                await Task.Delay(500, ct);
            });

            var task = signalBus.SendAsync(new TestSignal(), cts.Token);
            
            cts.CancelAfter(250);
            
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            Assert.Fail();
        }

        [Test]
        public void RejectDoubleSameInvocation()
        {
            var signalBus = new SignalBus();
            var signal = new TestSignal();

            signalBus.Register<TestSignal>(AsyncMethodHandler);

            Assert.DoesNotThrowAsync(() => Task.FromResult(signalBus.SendAsync(signal)));
            Assert.DoesNotThrowAsync(() => Task.FromResult(signalBus.SendAsync(signal)));
        }
        
        [Test]
        public async Task MultipleInvocation()
        {
            var signalBus = new SignalBus();
            var signal = new TestSignal();
            var callCount = 0;

            Func<TestSignal, CancellationToken, UniTask> handler1 = async (e, ct) =>
            {
                callCount++;
                await UniTask.Delay(15, cancellationToken: ct);
            };
            
            Func<TestSignal, CancellationToken, UniTask> handler2 = async (e, ct) =>
            {
                callCount++;
                await UniTask.Delay(10, cancellationToken: ct);
            };
            
            signalBus.Register(handler1);
            signalBus.Register(handler2);

            await signalBus.SendAsync(signal, asyncSendInvocationType:AsyncSendInvocationType.Parallel);
            await signalBus.SendAsync(signal, asyncSendInvocationType:AsyncSendInvocationType.Sequence);
            await signalBus.SendAsync(signal, asyncSendInvocationType:AsyncSendInvocationType.Parallel);
            await signalBus.SendAsync(signal, asyncSendInvocationType:AsyncSendInvocationType.Sequence);
            
            Assert.AreEqual(8, callCount);
        }

        private void TestFailSignalMethodHandler(TestSignal testSignal)
        {
            Assert.Fail();
        }
        
        private void Test2SignalMethodHandler(TestSignalWithSignalBusField customTestSignalWithSignalBusField)
        {
            if (customTestSignalWithSignalBusField.IsFail)
            {
                Assert.Fail();
                return;
            }
            
            customTestSignalWithSignalBusField.SignalBus.Deregister(customTestSignalWithSignalBusField.Token);
        }
        
        private void Test3SignalMethodHandler(TestSignalWithSignalBusField customTestSignalWithSignalBusField)
        {
            if (customTestSignalWithSignalBusField.IsFail)
            {
                Assert.Fail();
                return;
            }
            
            customTestSignalWithSignalBusField.SignalBus.Deregister<TestSignalWithSignalBusField>(Test3SignalMethodHandler);
        }
        
        private async UniTask AsyncMethodHandler(TestSignal testSignal, CancellationToken token)
        {
            await Task.Delay(1000, token);
        }
    }

    internal class TestSignal
    {
        public string Name { get; set; }
        public int Identifier { get; set; }
    }

    internal class PayloadSignal<TPayload>
    {
        public TPayload Payload { get; protected set; }

        public PayloadSignal(TPayload payload)
        {
            Payload = payload;
        }
    }

    internal class TestSignalWithSignalBusField
    {
        public SignalBus SignalBus { get; set; }
        public bool IsFail { get; set; }
        public object Token { get; set; }
    }
    
    internal class SortTest1
    {
    }

    internal class SortTest2
    {
    }

    internal class SortTest3
    {
    }
}