using System;
using System.Collections.Generic;

namespace Core.BehaviourTree.Debug
{
    public interface IDebugBrainProvider : IDisposable
    {
        public IEnumerable<(string brainTitle, BehaviourTreeLogic.BehaviourTree brain)> Get();
    }
}