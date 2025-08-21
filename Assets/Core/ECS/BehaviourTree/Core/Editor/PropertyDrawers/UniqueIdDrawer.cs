using UnityEditor;
using UnityEngine.UIElements;
using System;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [CustomPropertyDrawer(typeof(UniqueIdAttribute))]
    public class UniqueIdDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            if (property.propertyType != SerializedPropertyType.String)
            {
                container.Add(new Label($"[UniqueId] only valid on string fields"));
                return container;
            }

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    width = Length.Percent(100),
                    paddingBottom = 2,
                    paddingTop = 2,
                    paddingLeft = 2,
                    paddingRight = 2,
                    borderBottomWidth = 1,
                    borderTopWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1
                }
            };

            var warningLabel = new Label($"{property.name} Is null!")
            {
                style =
                {
                    color = Color.red,
                    marginTop = 2,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    fontSize = 11,
                    display = DisplayStyle.None
                }
            };

            void UpdateValidation(string value)
            {
                var isEmpty = string.IsNullOrEmpty(value);
                row.style.backgroundColor = isEmpty 
                    ? Color.red 
                    : new Color(0.1f, 0.1f, 0.1f, 1f);

                warningLabel.style.display = isEmpty 
                    ? DisplayStyle.Flex 
                    : DisplayStyle.None;
            }

            var textField = new TextField
            {
                value = property.stringValue,
                label = property.displayName,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    marginRight = 4
                }
            };

            UpdateValidation(property.stringValue);

            textField.RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
                UpdateValidation(evt.newValue);
            });

            var button = new Button(() =>
            {
                var newGuid = Guid.NewGuid().ToString();
                property.stringValue = newGuid;
                property.serializedObject.ApplyModifiedProperties();
                textField.value = newGuid;
                UpdateValidation(newGuid);
            })
            {
                text = "Generate",
                style =
                {
                    flexGrow = 0,
                    flexShrink = 1,
                    minWidth = 60,
                    maxWidth = 100
                }
            };

            row.Add(textField);
            row.Add(button);

            container.Add(row);
            container.Add(warningLabel);

            return container;
        }
    }
}
