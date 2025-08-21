using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace BehaviourTreeLogic
{
    [CustomEditor(typeof(BehaviourTreeInstance))]
    public class BehaviourTreeInstanceEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            var treeField = new PropertyField
            {
                bindingPath = nameof(BehaviourTreeInstance.behaviourTree)
            };

            var validateField = new PropertyField
            {
                bindingPath = nameof(BehaviourTreeInstance.validate)
            };

            var tickMode = new PropertyField
            {
                bindingPath = nameof(BehaviourTreeInstance.tickMode)
            };

            var startMode = new PropertyField
            {
                bindingPath = nameof(BehaviourTreeInstance.startMode)
            };

            var publicKeys = new PropertyField
            {
                bindingPath = nameof(BehaviourTreeInstance.blackboardOverrides)
            };

            container.Add(treeField);
            container.Add(tickMode);
            container.Add(startMode);
            container.Add(validateField);
            container.Add(publicKeys);

            return container;
        }
    }
}