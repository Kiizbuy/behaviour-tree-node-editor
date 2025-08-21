using System;
using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [Serializable]
    [BTHelp("Node that searches for a higher-priority node by weight and executes it until the child node is SUCCESS or FAILURE")]
    public class UtilitySelector : CompositeNode
    {
        private Node activeChild;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
            if (activeChild != null)
            {
                activeChild.Abort();
                activeChild = null;
            }
        }

        protected override State OnUpdate()
        {
            if (children.Count == 0)
            {
                return State.Failure;
            }

            if (activeChild != null)
            {
                return activeChild.Update();
            }

            Node bestNode = null;
            float highestUtility = float.NegativeInfinity;

            foreach (var child in children)
            {
                var utility = child.GetUtility();
                if (utility > highestUtility)
                {
                    highestUtility = utility;
                    bestNode = child;
                }
            }

            if (bestNode == null)
                return State.Failure;

            if (activeChild != bestNode)
            {
                activeChild?.Abort();
                activeChild = bestNode;
            }

            return activeChild.Update();
        }
    }
}