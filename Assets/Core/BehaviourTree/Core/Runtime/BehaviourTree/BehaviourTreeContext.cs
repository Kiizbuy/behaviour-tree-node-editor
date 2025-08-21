using System;
using System.Collections.Generic;

namespace BehaviourTreeLogic
{
    // The context is a storage object for sharing common data between nodes in the tree.
    // Useful for caching components, game objects, or other data that is used by multiple nodes.
    public class BehaviourTreeContext
    {
        public Dictionary<string, Node.State> TickResults = new();
        public float TickDelta;
        
        private readonly Dictionary<Type, object> _contextObjects = new();


        public void Register<T>(T service)
        {
            var type = typeof(T);
            if (_contextObjects.ContainsKey(type))
                throw new InvalidOperationException($"Service of type {type} already registered.");

            _contextObjects[type] = service;
        }

        public T Get<T>()
        {
            var type = typeof(T);
            if (_contextObjects.TryGetValue(type, out var service))
                return (T)service;

            throw new KeyNotFoundException($"Service of type {type} is not registered.");
        }

        public bool IsRegistered<T>()
        {
            return _contextObjects.ContainsKey(typeof(T));
        }
    }
}