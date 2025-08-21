using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("RootNode. Can't delete")]
    public class RootNode : Node
    {
        [SerializeReference] [HideInInspector] public Node child;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            return child?.Update() ?? State.Failure;
        }
    }
}