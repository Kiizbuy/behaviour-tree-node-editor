using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    public class Selector : CompositeNode
    {
        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            foreach (var node in children)
            {
                var childStatus = node.Update();
                switch (childStatus)
                {
                    case State.Running:
                        return State.Running;
                    case State.Success:
                        return State.Success;
                    case State.Failure:
                        continue;
                }
            }

            return State.Failure;
        }
    }
}