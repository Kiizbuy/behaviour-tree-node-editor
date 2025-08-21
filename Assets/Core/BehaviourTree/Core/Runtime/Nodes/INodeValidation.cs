namespace BehaviourTreeLogic
{
    public interface INodeValidation
    {
        bool IsValid(out string errorMessage);
    }
}