using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace BehaviourTreeLogic
{
    [CustomPropertyDrawer(typeof(BlackboardKeyValuePair))]
    public class BlackboardKeyValuePairPropertyDrawer : PropertyDrawer
    {
        private VisualElement pairContainer;

        private BehaviourTree GetBehaviourTree(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;

            if (target is BehaviourTree tree)
                return tree;

            var type = target.GetType();
            var field = type
                .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(f => typeof(BehaviourTree).IsAssignableFrom(f.FieldType));

            if (field != null)
            {
                var value = field.GetValue(target) as BehaviourTree;
                if (value != null)
                    return value;
            }

            Debug.LogError($"Could not find BehaviourTree field on {type.Name}");
            return null;
        }


        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var first = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.key));
            var second = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.value));

            var dropdown = new PopupField<BlackboardKey>
            {
                label = first.displayName,
                formatListItemCallback = FormatItem,
                formatSelectedValueCallback = FormatItem,
                value = first.managedReferenceValue as BlackboardKey
            };

            var tree = GetBehaviourTree(property);
            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    dropdown.choices.Add(key);
                }
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                var newKey = evt.newValue;
                first.managedReferenceValue = newKey;
                property.serializedObject.ApplyModifiedProperties();

                if (pairContainer.childCount > 1)
                {
                    pairContainer.RemoveAt(1);
                }

                if (second.managedReferenceValue == null ||
                    second.managedReferenceValue.GetType() != dropdown.value.GetType())
                {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }

                var field = new PropertyField
                {
                    label = second.displayName
                };
                field.BindProperty(second.FindPropertyRelative(nameof(BlackboardKey<object>.value)));
                pairContainer.Add(field);
            });

            pairContainer = new VisualElement();
            pairContainer.Add(dropdown);

            if (dropdown.value != null)
            {
                if (second.managedReferenceValue == null ||
                    first.managedReferenceValue.GetType() != second.managedReferenceValue.GetType())
                {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }

                var field = new PropertyField
                {
                    label = second.displayName
                };
                field.BindProperty(second.FindPropertyRelative(nameof(BlackboardKey<object>.value)));
                pairContainer.Add(field);
            }

            return pairContainer;
        }

        private string FormatItem(BlackboardKey item) => item == null ? "(null)" : item.name;
    }
}