using System.Collections.Generic;
using Core.BehaviourTree.Debug;
using UnityEngine;

namespace BehaviourTreeLogic.Bootstrap
{
    public class BehaviourTreeBootstrap : MonoBehaviour
    {
        [SerializeField] private BehaviourTreeInstance _btInstance;

        private ExampleBehaviourTreeBrainProvider _brainProvider;
        
        private void Awake()
        {
            _brainProvider = new ExampleBehaviourTreeBrainProvider();
            _brainProvider.RegisterBrainOwner(_btInstance);
            MainDebugBrainProvider.RegisterBrainProvider(_brainProvider);
        }

        private void OnDestroy()
        {
            _brainProvider.Dispose();
            MainDebugBrainProvider.Clear();
        }

        private class ExampleBehaviourTreeBrainProvider : IDebugBrainProvider
        {
            private readonly List<BehaviourTreeInstance> _registeredTrees = new();

            public void RegisterBrainOwner(BehaviourTreeInstance owner)
            {
                _registeredTrees.Add(owner);
            }
            
            public void Dispose()
            {
                _registeredTrees.Clear();
            }

            public IEnumerable<(string brainTitle, BehaviourTree brain)> Get()
            {
                foreach (var brainInstance in _registeredTrees)
                {
                    yield return ($"{brainInstance.gameObject.name} - {brainInstance.RuntimeTree.name}",
                        brainInstance.RuntimeTree);
                }
            }
        }
    }
}