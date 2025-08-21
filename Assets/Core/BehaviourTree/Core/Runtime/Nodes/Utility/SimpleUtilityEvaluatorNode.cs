namespace BehaviourTreeLogic.Utility
{
    public class SimpleUtilityEvaluatorNode : BaseUtilityEvaluator
    {
        protected override float EvaluateUtility()
        {
            return 1f;
        }
    }
}