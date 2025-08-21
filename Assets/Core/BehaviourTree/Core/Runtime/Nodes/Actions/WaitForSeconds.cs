using System;
using BehaviourTreeLogic.Attributes;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [Serializable]
    [BTHelp("Node that can wait for seconds")]
    public class WaitForSeconds : ActionNode, INodeValidation
    {
        [SerializeField] 
        [BTHelp("duration per seconds")]
        private float _duration;

        private float _cooldown;
        
        protected override void OnStart()
        {
            _cooldown = 0f;
        }

        protected override void OnStop()
        {
            _cooldown = 0f;
        }

        protected override State OnUpdate()
        {
            _cooldown += Time.deltaTime;
            return _cooldown >= _duration ? State.Success : State.Running;
        }

        public bool IsValid(out string errorMessage)
        {
            errorMessage = string.Empty;
            if (_duration <= 0)
            {
                errorMessage = "duration must be greater than zero!";
                return false;
            }

            return true;
        }
    }
}