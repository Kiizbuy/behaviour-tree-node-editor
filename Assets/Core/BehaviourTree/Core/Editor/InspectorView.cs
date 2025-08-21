using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Reflection;
using System.Linq;
using BehaviourTreeLogic.Attributes;
using UnityEditor;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [UxmlElement]
    public partial class InspectorView : VisualElement
    {
        private VisualElement validationBox;
        private Label validationLabel;

        public InspectorView()
        {
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            Add(scrollView);
        }

        internal void UpdateSelection(SerializedBehaviourTree serializer, NodeView nodeView)
        {
            Clear();

            if (nodeView == null)
                return;

            var nodeProperty = serializer.FindNode(serializer.Nodes, nodeView.node);
            if (nodeProperty == null)
                return;

            nodeProperty.isExpanded = true;

            var contentScrollView = new ScrollView(ScrollViewMode.Vertical);
            validationBox = new VisualElement
            {
                style =
                {
                    marginTop = 10,
                    backgroundColor = new Color(1f, 0.8f, 0.4f, 0.2f),
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    display = DisplayStyle.None
                }
            };

            var validationHeader = new Label("Errors")
            {
                style =
                {
                    color = Color.red,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 4
                }
            };
            validationBox.Add(validationHeader);

            validationLabel = new Label
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 11
                }
            };
            validationBox.Add(validationLabel);
            contentScrollView.Add(validationBox);

            var field = new PropertyField
            {
                label = nodeProperty.managedReferenceValue.GetType().Name
            };
            field.BindProperty(nodeProperty);

            contentScrollView.Add(field);

            var helpEntries = CollectAllHelpEntries(nodeProperty);
            if (helpEntries.Count > 0)
            {
                AddGroupedHelpContainer(contentScrollView, helpEntries);
            }
            
            
            Add(contentScrollView);
            UpdateValidationDisplay(nodeView);
            
            var so = nodeProperty.serializedObject;
            field.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                nodeView.UpdateValidation();
                UpdateValidationDisplay(nodeView);
            });
            
            field.TrackSerializedObjectValue(so, _ =>
            {
                nodeView.UpdateValidation();
                UpdateValidationDisplay(nodeView);
            });
        }

        private void UpdateValidationDisplay(NodeView nodeView)
        {
            if (nodeView?.node is INodeValidation validator)
            {
                if (validator.IsValid(out var message))
                {
                    validationBox.style.display = DisplayStyle.None;
                }
                else
                {
                    validationLabel.text = message;
                    validationBox.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                validationBox.style.display = DisplayStyle.None;
            }
        }

        private System.Collections.Generic.List<HelpEntry> CollectAllHelpEntries(SerializedProperty nodeProperty)
        {
            var helpEntries = new System.Collections.Generic.List<HelpEntry>();
            var nodeType = nodeProperty.managedReferenceValue.GetType();

            var classHelp = nodeType.GetCustomAttribute<BTHelpAttribute>();
            if (classHelp != null)
            {
                helpEntries.Add(new HelpEntry
                {
                    Title = nodeType.Name,
                    Text = classHelp.HelpText,
                    IndentLevel = 0
                });
            }

            var fieldInfos = nodeType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsDefined(typeof(HideInInspector)) &&
                            !f.IsDefined(typeof(NonSerializedAttribute)));

            foreach (var fieldInfo in fieldInfos)
            {
                var fieldHelp = fieldInfo.GetCustomAttribute<BTHelpAttribute>();
                if (fieldHelp != null)
                {
                    helpEntries.Add(new HelpEntry
                    {
                        Title = $"{ObjectNames.NicifyVariableName(fieldInfo.Name)}",
                        Text = fieldHelp.HelpText,
                        IndentLevel = 1
                    });
                }
            }

            return helpEntries;
        }

        private void AddGroupedHelpContainer(VisualElement parent,
            System.Collections.Generic.List<HelpEntry> helpEntries)
        {
            var helpContainer = new VisualElement
            {
                style =
                {
                    marginTop = 10,
                    backgroundColor = new Color(0f, 0f, 0.2f, 0.5f),
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    marginBottom = 4,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5
                }
            };

            var helpHeader = new Label("Node Documentation")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 12,
                    marginBottom = 5,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    width = Length.Percent(100)
                }
            };
            helpContainer.Add(helpHeader);

            foreach (var entry in helpEntries)
            {
                var entryContainer = new VisualElement
                {
                    style =
                    {
                        marginLeft = entry.IndentLevel * 15,
                        marginBottom = 5
                    }
                };

                var titleLabel = new Label(entry.Title)
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };
                entryContainer.Add(titleLabel);

                var textLabel = new Label(entry.Text)
                {
                    style =
                    {
                        whiteSpace = WhiteSpace.Normal
                    }
                };
                entryContainer.Add(textLabel);

                helpContainer.Add(entryContainer);
            }

            parent.Add(helpContainer);
        }

        private struct HelpEntry
        {
            public string Title;
            public string Text;
            public int IndentLevel;
        }
    }
}