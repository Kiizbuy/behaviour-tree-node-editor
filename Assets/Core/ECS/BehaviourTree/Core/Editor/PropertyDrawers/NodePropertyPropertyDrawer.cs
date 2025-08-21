using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [CustomPropertyDrawer(typeof(NodeProperty<>))]
    public class GenericNodePropertyPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var tree = property.serializedObject.targetObject as BehaviourTree;
            var genericTypes = fieldInfo.FieldType.GenericTypeArguments;
            var propertyType = genericTypes[0];
            var reference = property.FindPropertyRelative("reference");

            var label = new Label();
            label.AddToClassList("unity-base-field__label");
            label.AddToClassList("unity-property-field__label");
            label.AddToClassList("unity-property-field");
            label.text = property.displayName;

            var defaultValueField = new PropertyField
            {
                label = "",
                style =
                {
                    flexGrow = 1.0f
                },
                bindingPath = nameof(NodeProperty<int>.defaultValue)
            };

            var dropdown = new PopupField<BlackboardKey>
            {
                label = "",
                formatListItemCallback = FormatItem,
                formatSelectedValueCallback = FormatSelectedItem,
                value = reference.managedReferenceValue as BlackboardKey,
                tooltip = "Bind value to a BlackboardKey",
                style =
                {
                    flexGrow = 1.0f
                }
            };

            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    if (propertyType.IsAssignableFrom(key.underlyingType))
                    {
                        dropdown.choices.Add(key);
                    }
                }

                dropdown.choices.Add(null);

                dropdown.choices.Sort((left, right) =>
                {
                    if (left == null) return -1;
                    if (right == null) return 1;
                    return left.name.CompareTo(right.name);
                });
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                var newKey = evt.newValue;
                reference.managedReferenceValue = newKey;
                BehaviourTreeEditorWindow.Instance.CurrentSerializer.ApplyChanges();

                if (evt.newValue == null)
                {
                    defaultValueField.style.display = DisplayStyle.Flex;
                    dropdown.style.flexGrow = 0.0f;
                }
                else
                {
                    defaultValueField.style.display = DisplayStyle.None;
                    dropdown.style.flexGrow = 1.0f;
                }
            });

            defaultValueField.style.display = dropdown.value == null ? DisplayStyle.Flex : DisplayStyle.None;
            dropdown.style.flexGrow = dropdown.value == null ? 0.0f : 1.0f;

            var container = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    marginBottom = 4,
                    marginTop = 2,
                    flexDirection = FlexDirection.Row
                }
            };

            container.Add(label);
            container.Add(defaultValueField);
            container.Add(dropdown);

            return container;
        }

        private string FormatItem(BlackboardKey item)
        {
            return item == null ? "[Inline]" : item.name;
        }

        private string FormatSelectedItem(BlackboardKey item)
        {
            return item == null ? "" : item.name;
        }
    }


    [CustomPropertyDrawer(typeof(NodeProperty))]
    public class NodePropertyPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var tree = property.serializedObject.targetObject as BehaviourTree;

            var reference = property.FindPropertyRelative("reference");

            var dropdown = new PopupField<BlackboardKey>
            {
                label = property.displayName,
                formatListItemCallback = FormatItem,
                formatSelectedValueCallback = FormatItem,
                value = reference.managedReferenceValue as BlackboardKey
            };

            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    dropdown.choices.Add(key);
                }

                dropdown.choices.Sort((left, right) => { return left.name.CompareTo(right.name); });
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                var newKey = evt.newValue;
                reference.managedReferenceValue = newKey;
                BehaviourTreeEditorWindow.Instance.CurrentSerializer.ApplyChanges();
            });
            return dropdown;
        }

        private string FormatItem(BlackboardKey item)
        {
            return item == null ? "(null)" : item.name;
        }
    }
}