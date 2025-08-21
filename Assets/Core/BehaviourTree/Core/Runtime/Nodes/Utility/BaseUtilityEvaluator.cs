using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic.Utility
{
    public abstract class BaseUtilityEvaluator : DecoratorNode
    {
        [SerializeField, Range(0,2)]
        [BTHelp("Score Utility multiplier")]
        private float _baseScoreMultiplier = 1f;
        
        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected abstract float EvaluateUtility();

        public override float GetUtility()
        {
            return EvaluateUtility() * _baseScoreMultiplier;
        }

        protected override State OnUpdate()
        {
            return child?.Update() ?? State.Failure;
        }
    }
}