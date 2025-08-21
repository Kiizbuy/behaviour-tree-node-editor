using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [BTHelp(
            "Node that starts a child behavior tree and, if it completes successfully, starts executing the child node")]
    public class SubTreeDecorator : DecoratorNode
    {
        [Tooltip("Behaviour tree asset to run as a subtree")]
        [BTHelp("Behaviour tree asset to run as a subtree")]
        public BehaviourTree treeAsset;

        [HideInInspector] public BehaviourTree treeInstance;

        protected override void OnStart()
        {
            if (treeAsset)
            {
                treeInstance = treeAsset.Clone();
                treeInstance.Bind(BehaviourTreeContext);
            }
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (!treeInstance)
                return State.Failure;

            return treeInstance.lastTickState == State.Success
                ? child.Update()
                : treeInstance.Tick(BehaviourTreeContext.TickDelta);
        }
    }
}