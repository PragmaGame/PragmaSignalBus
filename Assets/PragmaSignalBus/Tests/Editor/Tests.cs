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

        // ─────────────────────────────────────────────────────────────────────
        // Register / Send (sync)
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RegisterAndSend_MethodHandler_InvokesHandler()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestSignalMethodHandler);

            Assert.IsFalse(_methodHandlerHit);
            signalBus.Send(new TestSignal { Name = "Custom Signal", Identifier = 1 });
            Assert.IsTrue(_methodHandlerHit);
        }

        [Test]
        public void RegisterAndSend_LambdaHandler_InvokesHandler()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(s =>
            {
                Assert.AreEqual("Custom signal 2", s.Name);
                Assert.AreEqual(2, s.Identifier);
                _actionHandlerHit = true;
            });

            Assert.IsFalse(_actionHandlerHit);
            signalBus.Send(new TestSignal { Name = "Custom signal 2", Identifier = 2 });
            Assert.IsTrue(_actionHandlerHit);
        }

        [Test]
        public void RegisterAndSend_GenericPayloadSignal_InvokesHandler()
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
        public void RegisterAndSend_ActionWithoutParam_InvokesHandler()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<TestSignal>(() => hit = true);

            signalBus.Send(new TestSignal());
            Assert.IsTrue(hit);
        }

        [Test]
        public void RegisterAndSend_SendWithoutPayload_InvokesHandler()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<TestSignal>(_ => hit = true);

            signalBus.Send<TestSignal>();
            Assert.IsTrue(hit);
        }

        [Test]
        public void Send_MultipleHandlersSameSignal_AllInvoked()
        {
            var signalBus = new SignalBus();
            var handler1Count = 0;
            var handler2Count = 0;

            signalBus.Register<TestSignal>(_ => handler1Count++);
            signalBus.Register<TestSignal>(_ => handler2Count++);

            signalBus.Send(new TestSignal());

            Assert.AreEqual(1, handler1Count);
            Assert.AreEqual(1, handler2Count);
        }

        [Test]
        public void Send_MultipleTimesInOrder_SignalsArriveInOrder()
        {
            var signalBus = new SignalBus();
            var results = new List<int>();

            signalBus.Register<TestSignal>(s => results.Add(s.Identifier));

            for (int i = 1; i <= 6; i++)
                signalBus.Send(new TestSignal { Identifier = i });

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, results);
        }

        [Test]
        public void Send_NoSubscribers_DoesNotThrow()
        {
            var signalBus = new SignalBus();
            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
            Assert.DoesNotThrow(() => signalBus.Send<TestSignal>());
        }

        [Test]
        public void SendAbstract_InvokesCorrectHandler()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<TestSignal>(_ => hit = true);
            signalBus.SendAbstract(new TestSignal());

            Assert.IsTrue(hit);
        }

        [Test]
        public void SendAbstract_NullSignal_DoesNotThrow()
        {
            var signalBus = new SignalBus();
            Assert.DoesNotThrow(() => signalBus.SendAbstract(null));
        }

        [Test]
        public void SendAbstract_DoesNotInvokeUnrelatedHandlers()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<PayloadSignal<int>>(_ => hit = true);
            signalBus.SendAbstract(new TestSignal());

            Assert.IsFalse(hit);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Deregister (sync)
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Deregister_ByDelegate_StopsInvocation()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(TestFailSignalMethodHandler);
            signalBus.Deregister<TestSignal>(TestFailSignalMethodHandler);

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public void Deregister_ActionWithoutParam_ByDelegate_StopsInvocation()
        {
            var signalBus = new SignalBus();
            Action handler = () => Assert.Fail("Should not be invoked after deregister");

            signalBus.Register<TestSignal>(handler);
            signalBus.Deregister<TestSignal>(handler);

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public void Deregister_ByManualToken_StopsInvocation()
        {
            var signalBus = new SignalBus();
            const int token = 1;

            signalBus.Register<TestSignal>(TestFailSignalMethodHandler, token: token);
            signalBus.Deregister(token);

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public void Deregister_ByAutoToken_StopsInvocation()
        {
            var signalBus = new SignalBus();
            var token = signalBus.Register<TestSignal>(TestFailSignalMethodHandler);
            signalBus.Deregister(token);

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public void Deregister_ByToken_RemovesMultipleSignalTypes()
        {
            var signalBus = new SignalBus();
            const int token = 1;

            signalBus.Register<TestSignal>(TestFailSignalMethodHandler, token: token);
            signalBus.Register<TestSignalWithSignalBusField>(Test2SignalMethodHandler, token: token);

            signalBus.Deregister(token);

            signalBus.Send(new TestSignalWithSignalBusField { IsFail = true });
        }

        [Test]
        public void Deregister_LambdaByToken_StopsInvocation()
        {
            var signalBus = new SignalBus();
            const int token = 1;

            signalBus.Register<TestSignal>(_ => Assert.Fail("Should not be called"), token: token);
            signalBus.Deregister(token);

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public void Deregister_InsideSendHandler_DoesNotAffectCurrentSend()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignalWithSignalBusField>(Test3SignalMethodHandler);

            signalBus.Send(new TestSignalWithSignalBusField { SignalBus = signalBus, IsFail = false });
            signalBus.Send(new TestSignalWithSignalBusField { SignalBus = null, IsFail = true });
        }

        [Test]
        public void Deregister_ByAutoToken_InsideSendHandler_DoesNotAffectCurrentSend()
        {
            var signalBus = new SignalBus();
            var token = signalBus.Register<TestSignalWithSignalBusField>(Test2SignalMethodHandler);

            signalBus.Send(new TestSignalWithSignalBusField { SignalBus = signalBus, IsFail = false, Token = token });
            signalBus.Send(new TestSignalWithSignalBusField { SignalBus = null, IsFail = true });
        }

        [Test]
        public void Deregister_CalledMultipleTimes_DoesNotThrow()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(CustomTestSignalMethodHandler);

            Assert.DoesNotThrow(() =>
            {
                signalBus.Deregister<TestSignal>(CustomTestSignalMethodHandler);
                signalBus.Deregister<TestSignal>(CustomTestSignalMethodHandler);
                signalBus.Deregister<TestSignal>(CustomTestSignalMethodHandler);
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // ClearSubscriptions
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void ClearSubscriptions_RemovesAllSyncHandlers()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(_ => Assert.Fail("Should not be called after clear"));
            signalBus.Register<TestSignal>(_ => Assert.Fail("Should not be called after clear"));

            signalBus.ClearSubscriptions();

            Assert.DoesNotThrow(() => signalBus.Send(new TestSignal()));
        }

        [Test]
        public async Task ClearSubscriptions_RemovesAllAsyncHandlers()
        {
            var signalBus = new SignalBus();
            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                Assert.Fail("Should not be called after clear");
                await Task.CompletedTask;
            });

            signalBus.ClearSubscriptions();

            await signalBus.SendAsync(new TestSignal());
        }

        // ─────────────────────────────────────────────────────────────────────
        // SortOptions (sync)
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public void SortOptions_AfterAndBefore_CorrectOrder()
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
        public void SortOptions_CyclicDependency_ThrowsOnRegister()
        {
            var signalBus = new SignalBus();

            signalBus.Register<TestSignal>(() => { },
                new SortOptions(typeof(SortTest1), beforeOrder: new[] { typeof(SortTest2) }));

            Assert.Throws<InvalidOperationException>(() =>
            {
                signalBus.Register<TestSignal>(() => { },
                    new SortOptions(typeof(SortTest2), beforeOrder: new[] { typeof(SortTest1) }));
            });
        }

        [Test]
        public void SortOptions_ComplexCyclicDependency_ThrowsOnRegister()
        {
            var signalBus = new SignalBus();

            Assert.Throws<InvalidOperationException>(() =>
            {
                signalBus.Register<TestSignal>(() => { },
                    new SortOptions(typeof(SortTest1), beforeOrder: new[] { typeof(SortTest3) }));

                signalBus.Register<TestSignal>(() => { },
                    new SortOptions(typeof(SortTest2), beforeOrder: new[] { typeof(SortTest1) }));

                signalBus.Register<TestSignal>(() => { },
                    new SortOptions(typeof(SortTest3), beforeOrder: new[] { typeof(SortTest2) }));
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Register / Send (async)
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public async Task RegisterAndSendAsync_WithSignalData_InvokesHandler()
        {
            var signalBus = new SignalBus();
            var eventHandled = false;

            signalBus.Register<TestSignal>(async (e, ct) =>
            {
                Assert.AreEqual("Test", e.Name);
                eventHandled = true;
                await Task.CompletedTask;
            });

            await signalBus.SendAsync(new TestSignal { Name = "Test" });

            Assert.IsTrue(eventHandled);
        }

        [Test]
        public async Task RegisterAndSendAsync_WithoutSignalData_InvokesHandler()
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
        public async Task SendAsync_NoSubscribers_DoesNotThrow()
        {
            var signalBus = new SignalBus();
            Assert.DoesNotThrowAsync(async () => await signalBus.SendAsync(new TestSignal()));
            Assert.DoesNotThrowAsync(async () => await signalBus.SendAsync<TestSignal>());
        }

        [Test]
        public async Task SendAbstractAsync_InvokesCorrectHandler()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                hit = true;
                await Task.CompletedTask;
            });

            await signalBus.SendAbstractAsync(new TestSignal());

            Assert.IsTrue(hit);
        }

        [Test]
        public async Task SendAbstractAsync_NullSignal_DoesNotThrow()
        {
            var signalBus = new SignalBus();
            Assert.DoesNotThrowAsync(async () => await signalBus.SendAbstractAsync(null));
        }

        [Test]
        public async Task SendAbstractAsync_DoesNotInvokeUnrelatedHandlers()
        {
            var signalBus = new SignalBus();
            var hit = false;

            signalBus.Register<PayloadSignal<int>>(async (_, ct) =>
            {
                hit = true;
                await Task.CompletedTask;
            });

            await signalBus.SendAbstractAsync(new TestSignal());

            Assert.IsFalse(hit);
        }

        [Test]
        public async Task SendAsync_Sequence_SortOptions_ExecutesInCorrectOrder()
        {
            var signalBus = new SignalBus();
            var executionOrder = new List<int>();
            var cts = new CancellationTokenSource();

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await UniTask.Delay(50, cancellationToken: ct);
                executionOrder.Add(1);
            }, new SortOptions(typeof(SortTest1)));

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await UniTask.Delay(10, cancellationToken: ct);
                executionOrder.Add(2);
            }, new SortOptions(typeof(SortTest2), afterOrder: new[] { typeof(SortTest1) }));

            await signalBus.SendAsync(new TestSignal(), asyncSendInvocationType: AsyncSendInvocationType.Sequence, token: cts.Token);

            Assert.AreEqual(new List<int> { 1, 2 }, executionOrder);
        }

        [Test]
        public async Task SendAsync_Parallel_AllHandlersInvoked()
        {
            var signalBus = new SignalBus();
            var count = 0;

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await UniTask.Delay(30, cancellationToken: ct);
                Interlocked.Increment(ref count);
            });

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await UniTask.Delay(10, cancellationToken: ct);
                Interlocked.Increment(ref count);
            });

            await signalBus.SendAsync(new TestSignal(), asyncSendInvocationType: AsyncSendInvocationType.Parallel);

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task SendAsync_Parallel_RunsConcurrently()
        {
            // При последовательном выполнении суммарное время ~200ms, при параллельном ~100ms
            var signalBus = new SignalBus();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            signalBus.Register<TestSignal>(async (_, ct) => await UniTask.Delay(100, cancellationToken: ct));
            signalBus.Register<TestSignal>(async (_, ct) => await UniTask.Delay(100, cancellationToken: ct));

            await signalBus.SendAsync(new TestSignal(), asyncSendInvocationType: AsyncSendInvocationType.Parallel);
            sw.Stop();

            Assert.Less(sw.ElapsedMilliseconds, 180, "Parallel send должен выполняться конкурентно");
        }

        [Test]
        public async Task SendAsync_MultipleSignalTypes_DoNotInterfere()
        {
            var signalBus = new SignalBus();
            var count1 = 0;
            var count2 = 0;

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await UniTask.Delay(50, cancellationToken: ct);
                count1++;
            });

            signalBus.Register<PayloadSignal<int>>(async (_, ct) =>
            {
                await UniTask.Delay(50, cancellationToken: ct);
                count2++;
            });

            await UniTask.WhenAll(
                signalBus.SendAsync(new TestSignal()),
                signalBus.SendAsync(new PayloadSignal<int>(2))
            );

            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void SendAsync_HandlerThrows_ExceptionPropagates()
        {
            var signalBus = new SignalBus();

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("test error");
            });

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await signalBus.SendAsync(new TestSignal()));
        }

        [Test]
        public async Task SendAsync_WithCancellationToken_RespectsCancellation()
        {
            var signalBus = new SignalBus();
            var cts = new CancellationTokenSource();

            signalBus.Register<TestSignal>(async (_, ct) => await Task.Delay(500, ct));

            var task = signalBus.SendAsync(new TestSignal(), cts.Token);
            cts.CancelAfter(100);

            try
            {
                await task;
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // ожидаемо
            }
        }

        [Test]
        public async Task SendAsync_MultipleInvocationModes_AllCallsComplete()
        {
            var signalBus = new SignalBus();
            var signal = new TestSignal();
            var callCount = 0;

            Func<TestSignal, CancellationToken, UniTask> handler1 = async (_, ct) =>
            {
                callCount++;
                await UniTask.Delay(15, cancellationToken: ct);
            };

            Func<TestSignal, CancellationToken, UniTask> handler2 = async (_, ct) =>
            {
                callCount++;
                await UniTask.Delay(10, cancellationToken: ct);
            };

            signalBus.Register(handler1);
            signalBus.Register(handler2);

            await signalBus.SendAsync(signal, asyncSendInvocationType: AsyncSendInvocationType.Parallel);
            await signalBus.SendAsync(signal, asyncSendInvocationType: AsyncSendInvocationType.Sequence);
            await signalBus.SendAsync(signal, asyncSendInvocationType: AsyncSendInvocationType.Parallel);
            await signalBus.SendAsync(signal, asyncSendInvocationType: AsyncSendInvocationType.Sequence);

            Assert.AreEqual(8, callCount);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Deregister (async)
        // ─────────────────────────────────────────────────────────────────────

        [Test]
        public async Task Deregister_AsyncByDelegate_StopsInvocation()
        {
            var signalBus = new SignalBus();
            var callCount = 0;

            Func<TestSignal, CancellationToken, UniTask> handler = (_, ct) =>
            {
                callCount++;
                return UniTask.CompletedTask;
            };

            signalBus.Register(handler);
            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount);

            signalBus.Deregister(handler);
            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount, "Handler не должен быть вызван после Deregister");
        }

        [Test]
        public async Task Deregister_AsyncActionWithoutParam_ByDelegate_StopsInvocation()
        {
            var signalBus = new SignalBus();
            var callCount = 0;

            Func<CancellationToken, UniTask> handler = _ =>
            {
                callCount++;
                return UniTask.CompletedTask;
            };

            signalBus.Register<TestSignal>(handler);
            await signalBus.SendAsync<TestSignal>();
            Assert.AreEqual(1, callCount);

            signalBus.Deregister<TestSignal>(handler);
            await signalBus.SendAsync<TestSignal>();
            Assert.AreEqual(1, callCount, "Handler не должен быть вызван после Deregister");
        }

        [Test]
        public async Task Deregister_AsyncByToken_StopsInvocation()
        {
            var signalBus = new SignalBus();
            var callCount = 0;

            var token = signalBus.Register<TestSignal>(async (_, ct) =>
            {
                callCount++;
                await Task.CompletedTask;
            });

            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount);

            signalBus.Deregister(token);

            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount, "Handler не должен быть вызван после Deregister по токену");
        }

        [Test]
        public async Task Deregister_AsyncInsideSend_DoesNotAffectCurrentSend()
        {
            var signalBus = new SignalBus();
            var callCount = 0;
            object token = null;

            Func<TestSignal, CancellationToken, UniTask> handler = async (_, ct) =>
            {
                callCount++;
                signalBus.Deregister(token);
                await Task.CompletedTask;
            };

            token = signalBus.Register(handler);

            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount);

            await signalBus.SendAsync(new TestSignal());
            Assert.AreEqual(1, callCount, "Handler не должен быть вызван после Deregister изнутри");
        }

        [Test]
        public async Task Register_AsyncInsideSend_NewHandlerNotCalledInCurrentSend()
        {
            var signalBus = new SignalBus();
            var originalCallCount = 0;
            var newHandlerCallCount = 0;
            var signal = new TestSignal();

            signalBus.Register<TestSignal>(async (_, ct) =>
            {
                originalCallCount++;
                signalBus.Register<TestSignal>(async (_, ct2) =>
                {
                    newHandlerCallCount++;
                    await Task.CompletedTask;
                });
                await Task.CompletedTask;
            });

            await signalBus.SendAsync(signal);
            Assert.AreEqual(1, originalCallCount);
            Assert.AreEqual(0, newHandlerCallCount, "Новый хендлер не должен вызываться в текущем Send");

            await signalBus.SendAsync(signal);
            Assert.AreEqual(2, originalCallCount);
            Assert.AreEqual(1, newHandlerCallCount);
        }

        [Test]
        public async Task Deregister_AsyncWhenCancelled_HandlerRemovedForNextSend()
        {
            var signalBus = new SignalBus();
            var cts = new CancellationTokenSource();
            var callCount = 0;

            Func<TestSignal, CancellationToken, UniTask> handler1 = (_, ct) =>
            {
                callCount++;
                return UniTask.CompletedTask;
            };

            Func<TestSignal, CancellationToken, UniTask> handler2 = async (_, ct) =>
            {
                signalBus.Deregister(handler1);
                await UniTask.Delay(500, cancellationToken: ct);
            };

            signalBus.Register(handler1);
            signalBus.Register(handler2);

            cts.CancelAfter(100);
            await signalBus.SendAsync(new TestSignal(), cts.Token).SuppressCancellationThrow();

            cts = new CancellationTokenSource();
            await signalBus.SendAsync(new TestSignal(), cts.Token).SuppressCancellationThrow();

            Assert.AreEqual(1, callCount, "Handler1 должен быть вызван ровно один раз");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper methods
        // ─────────────────────────────────────────────────────────────────────

        private void CustomTestSignalMethodHandler(TestSignal testSignal)
        {
            Assert.AreEqual("Custom Signal", testSignal.Name);
            Assert.AreEqual(1, testSignal.Identifier);
            _methodHandlerHit = true;
        }

        private void TestFailSignalMethodHandler(TestSignal testSignal)
        {
            Assert.Fail("This handler should not be invoked after deregister.");
        }

        private void Test2SignalMethodHandler(TestSignalWithSignalBusField signal)
        {
            if (signal.IsFail)
            {
                Assert.Fail();
                return;
            }

            signal.SignalBus.Deregister(signal.Token);
        }

        private void Test3SignalMethodHandler(TestSignalWithSignalBusField signal)
        {
            if (signal.IsFail)
            {
                Assert.Fail();
                return;
            }

            signal.SignalBus.Deregister<TestSignalWithSignalBusField>(Test3SignalMethodHandler);
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

    internal class SortTest1 { }
    internal class SortTest2 { }
    internal class SortTest3 { }
}

