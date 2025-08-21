using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that allows you to stop PlayMode (needed for debugging Behaviour Tree)")]
    public class Breakpoint : ActionNode
    {
        protected override void OnStart()
        {
            Debug.Log("Trigging Breakpoint");
            Debug.Break();
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            return State.Success;
        }
    }
}