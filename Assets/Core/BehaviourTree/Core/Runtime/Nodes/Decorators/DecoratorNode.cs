using UnityEngine;

namespace BehaviourTreeLogic
{
    public abstract class DecoratorNode : Node
    {
        [SerializeReference] [HideInInspector] public Node child;
    }
}