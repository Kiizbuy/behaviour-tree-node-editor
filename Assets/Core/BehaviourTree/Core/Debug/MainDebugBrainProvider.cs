using System.Collections.Generic;
using System.Linq;

namespace Core.BehaviourTree.Debug
{
    public static class MainDebugBrainProvider
    {
        private static IDebugBrainProvider _debugBrainProvider;
        private static bool _isDisposed;

        public static void RegisterBrainProvider(IDebugBrainProvider provider)
        {
            TryDispose();
            _debugBrainProvider = provider;
            _isDisposed = false;
        }

        public static IEnumerable<(string title, BehaviourTreeLogic.BehaviourTree brain)> Get()
        {
            return _isDisposed || _debugBrainProvider == null
                ? Enumerable.Empty<(string title, BehaviourTreeLogic.BehaviourTree brain)>()
                : _debugBrainProvider.Get();
        }

        private static void TryDispose()
        {
            if (_debugBrainProvider != null)
            {
                _debugBrainProvider.Dispose();
                _debugBrainProvider = null;
            }
        }

        public static void Clear()
        {
            TryDispose();
            _isDisposed = true;

        }
    }
}