using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace BehaviourTreeLogic
{
    [UxmlElement]
    public partial class BlackboardView : VisualElement
    {
        private SerializedBehaviourTree _behaviourTree;

        private ListView _listView;
        private TextField _newKeyTextField;
        private PopupField<Type> _newKeyTypeField;

        private Button _createButton;

        internal void Bind(SerializedBehaviourTree behaviourTree)
        {
            _behaviourTree = behaviourTree;

            _listView = this.Q<ListView>("ListView_Keys");
            _newKeyTextField = this.Q<TextField>("TextField_KeyName");
            var popupContainer = this.Q<VisualElement>("PopupField_Placeholder");

            _createButton = this.Q<Button>("Button_KeyCreate");

            // ListView
            _listView.Bind(behaviourTree.serializedObject);
            _listView.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Delete)
                {
                    var key = _listView.selectedItem as SerializedProperty;
                    if (key != null)
                    {
                        BehaviourTreeEditorWindow.Instance.CurrentSerializer.DeleteBlackboardKey(key.displayName);
                    }
                }
            });

            _newKeyTypeField = new PopupField<Type>();
            _newKeyTypeField.label = "Type";
            _newKeyTypeField.formatListItemCallback = FormatItem;
            _newKeyTypeField.formatSelectedValueCallback = FormatItem;

            var types = TypeCache.GetTypesDerivedFrom<BlackboardKey>();
            foreach (var type in types)
            {
                if (type.IsGenericType)
                {
                    continue;
                }

                _newKeyTypeField.choices.Add(type);
                if (_newKeyTypeField.value == null)
                {
                    _newKeyTypeField.value = type;
                }
            }

            popupContainer.Clear();
            popupContainer.Add(_newKeyTypeField);

            // TextField
            _newKeyTextField.RegisterCallback<ChangeEvent<string>>((evt) => { ValidateButton(); });

            // Button
            _createButton.clicked -= CreateNewKey;
            _createButton.clicked += CreateNewKey;

            ValidateButton();
        }

        private string FormatItem(Type arg) => arg == null ? "(null)" : arg.Name.Replace("Key", "");

        private void ValidateButton()
        {
            // Disable the create button if trying to create a non-unique key
            var isValidKeyText = ValidateKeyText(_newKeyTextField.text);
            _createButton.SetEnabled(isValidKeyText);
        }

        private bool ValidateKeyText(string text)
        {
            if (text == "")
            {
                return false;
            }

            var tree = _behaviourTree.Blackboard.serializedObject.targetObject as BehaviourTree;
            var keyExists = tree.blackboard.Find(_newKeyTextField.text) != null;
            return !keyExists;
        }

        private void CreateNewKey()
        {
            var newKeyType = _newKeyTypeField.value;
            if (newKeyType != null)
            {
                _behaviourTree.CreateBlackboardKey(_newKeyTextField.text, newKeyType);
            }

            ValidateButton();
        }

        public void ClearView()
        {
            _behaviourTree = null;
            if (_listView != null)
            {
                _listView.Unbind();
            }
        }
    }
}