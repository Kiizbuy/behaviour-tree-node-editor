using System;
using System.Collections.Generic;

namespace Core.ECS.BehaviourTree.Debug
{
    public interface IDebugBrainProvider : IDisposable
    {
        public IEnumerable<(string brainTitle, BehaviourTreeLogic.BehaviourTree brain)> Get();
    }
}