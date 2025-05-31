using System;
using System.Collections.Generic;

namespace Pragma.SignalBus
{
    public class SignalBusBroadcaster : SignalBus, ISignalBusBroadcaster
    {
        private List<ISignalBusBroadcaster> _children; 
        
        private List<ISignalBusBroadcaster> _childrenToAdded;
        private List<ISignalBusBroadcaster> _childrenToRemoved;
        
        private bool _isAlreadyBroadcast;
        private bool _isDirtyChildren;
        
        public SignalBusBroadcaster(Configuration configuration = null) : base(configuration)
        {
            _children = new List<ISignalBusBroadcaster>();

            _childrenToAdded = new List<ISignalBusBroadcaster>();
            _childrenToRemoved = new List<ISignalBusBroadcaster>();
        }

        public void AddChildren(ISignalBusBroadcaster signalBus)
        {
            if (_isAlreadyBroadcast)
            {
                _childrenToAdded.Add(signalBus);
                _isDirtyChildren = true;
            }
            else
            {
                _children.Add(signalBus);
            }
        }

        public void RemoveChildren(ISignalBusBroadcaster signalBus)
        {
            if (_isAlreadyBroadcast)
            {
                _childrenToRemoved.Add(signalBus);
                _isDirtyChildren = true;
            }
            else
            {
                _children.Remove(signalBus);
            }
        }
        
        private void BroadcastInternal(Type signalType, object signal)
        {
            _isAlreadyBroadcast = true;
            
            foreach (var signalBus in _children)
            {
                signalBus.Broadcast(signalType, signal);
            }

            _isAlreadyBroadcast = false;

            if (_isDirtyChildren)
            {
                RefreshChildren();
                
                _isDirtyChildren = false;
            }
        }
        
        public void Broadcast<TSignal>(TSignal signal) where TSignal : class
        {
            Broadcast(typeof(TSignal), signal);
        }
        
        public void Broadcast<TSignal>() where TSignal : class
        {
            Broadcast(typeof(TSignal), null);
        }
        
        public void Broadcast(Type signalType, object signal)
        {
            Send(signalType, signal);
            
            BroadcastInternal(signalType, signal);
        }
        
        private void RefreshChildren()
        {
            foreach (var signalBus in _childrenToRemoved)
            {
                _children.Remove(signalBus);
            }
            
            _childrenToRemoved.Clear();

            foreach (var signalBus in _childrenToAdded)
            {
                _children.Add(signalBus);
            }
            
            _childrenToAdded.Clear();
        }
    }
}