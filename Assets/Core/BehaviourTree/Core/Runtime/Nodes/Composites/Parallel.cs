using BehaviourTreeLogic.Attributes;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    [BTHelp("Node that performs all actions in parallel")]
    public class Parallel : CompositeNode
    {
        public enum ParallelActionCompleteStatus
        {
            RequireAll,
            RequireOne
        }
        
        public ParallelActionCompleteStatus _successActionCompleteStatus;
        public ParallelActionCompleteStatus _failureActionCompleteStatus;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var successCount = 0;
            var failureCount = 0;

            foreach (var child in children)
            {
                var status = child.Update();
                switch (status)
                {
                    case State.Success:
                        successCount++;
                        break;
                    case State.Failure:
                        failureCount++;
                        break;
                }
            }

            switch (_successActionCompleteStatus)
            {
                case ParallelActionCompleteStatus.RequireAll when successCount == children.Count:
                case ParallelActionCompleteStatus.RequireOne when successCount > 0:
                    return State.Success;
            }

            switch (_failureActionCompleteStatus)
            {
                case ParallelActionCompleteStatus.RequireAll when failureCount == children.Count:
                case ParallelActionCompleteStatus.RequireOne when failureCount > 0:
                    return State.Failure;
                default:
                    return State.Running;
            }
        }
    }
}