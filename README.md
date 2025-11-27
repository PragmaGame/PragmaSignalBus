# PragmaSignalBus
Simple and fast in memory EventBus / SignalBus / MessageBus library in C# with no dependencies
## Install via git URL :
```
https://github.com/PragmaGame/PragmaSignalBus.git?path=Assets/PragmaSignalBus
```
## Simple usage

You can use any custom classes.

```c#
// Example
public class CustomSignalClass {}

// Example
public class PayloadSignal<TPayload>
{
    public TPayload Payload { get; protected set; }

    public PayloadEvent(TPayload payload)
    {
        Payload = payload;
    }
}

private void TestMethod()
{
    ISignalBus signalBus = new SignalBus();

    signalBus.Register<CustomSignalClass>(OnCustomSignal);
    signalBus.Register<PayloadEvent<int>>(OnIntSignal);

    signalBus.Register<PayloadSignal<string>>(s =>
    {
        Debug.Log(s.Payload);
    }, token: 12345);

    signalBus.Send(new CustomSignalClass()); // OnCustomSignal will be invoked
    signalBus.Send(new PayloadSignal<int>(5)); // OnIntSignal will be invoked
    signalBus.Send(new PayloadSignal<string>("Hello"));

    signalBus.Deregister<CustomSignalClass>(OnCustomSignal);
    signalBus.Deregister<PayloadEvent<int>>(OnIntSignal);
    signalBus.Deregister(12345);
}

private async void TestAsyncMethod()
{
    ISignalBus signalBus = new SignalBus();
    
    signalBus.Register<CustomSignalClass>(OnAsyncSignal);
    
    await signalBus.SendAsync(new CustomSignalClass()); // OnAsyncSignal will be invoked
}

private void OnCustomSignal(CustomSignalClass customSignal)
{
    Debug.Log("Received customSignal");
}

private void OnIntSignal(PayloadEvent<int> intSignal)
{
    Debug.Log(intEvent.Payload);
}

private async void OnAsyncSignal(CustomSignalClass customSignal, CancellationToken token)
{
    await Task.Delay(1000, token);
    Debug.Log("Received CustomEvent after 1 second");
}
```

## Ordering

You can use order for event handlers.

```c#
signalBus.Register<CustomSignalClass>(OnCustomSignal3, new SortOptions(typeof(SomeClass3), afterOrder: new[] { typeof(SomeClass2) }));
signalBus.Register<CustomSignalClass>(OnCustomSignal, new SortOptions(typeof(SomeClass1)));
signalBus.Register<CustomSignalClass>(OnCustomSignal2, new SortOptions(typeof(SomeClass2), afterOrder: new[] { typeof(SomeClass1) }));

signalBus.Send(new CustomSignalClass()); // OnCustomSignal, OnCustomSignal2, OnCustomSignal3 will be invoked
```

## Deregister with token

Use single token for deregister.

```c#
var token = gameObject;
signalBus.Register<CustomSignalClass>(OnCustomSignal, token: token);
signalBus.Register<PayloadEvent<int>>(OnIntSignal, token: token);
signalBus.Register<PayloadEvent<string>>(s =>
{
    Debug.Log(s.Payload);
}, token: token);

// Deregister all events with single token
eventBus.Deregister(token);
```

## Using from Dependency Injection container

Install from Dependency Injection container (Zenject/Extenject).

```c#
container.Bind<ISignalBus>().To<SignalBus>().AsSingle().NonLazy();
```
