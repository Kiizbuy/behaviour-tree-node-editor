using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace BehaviourTreeLogic
{
    // Create a new type of Settings Asset.
    public class BehaviourTreeProjectSettings : ScriptableObject
    {
        [Header("Asset Settings")] [Tooltip("Folder where new tree assets will be created. (Must begin with 'Assets')")]
        public string newTreePath = "Assets/";

        [Tooltip("Folder where new node scripts will be created. (Must begin with 'Assets')")]
        public string newNodePath = "Assets/";
        [Header("Styles")]
        public VisualTreeAsset behaviourTreeXml;
        public VisualTreeAsset nodeXml;
        public StyleSheet behaviourTreeStyle;

        [Tooltip("Custom script template to use when creating action nodes")]
        public TextAsset scriptTemplateActionNode;

        [Tooltip("Custom script template to use when creating condition nodes")]
        public TextAsset scriptTemplateConditionNode;

        [Tooltip("Custom script template to use when creating composite nodes")]
        public TextAsset scriptTemplateCompositeNode;

        [Tooltip("Custom script template to use when creating decorator nodes")]
        public TextAsset scriptTemplateDecoratorNode;
        [Tooltip("Custom script template to use when creating UtilityEvaluator nodes")]
        public TextAsset scriptTemplateUtilityEvaluatorNode;

        [Tooltip(
            "If enabled, selecting a node will automatically add all it's children to the selection. If disabled, hold control to select entire node hierarchy")]
        public bool autoSelectNodeHierarchy = false;

        //[HideInInspector]
        public BehaviourTreeEditorWindowState windowState = new BehaviourTreeEditorWindowState();

        private static string GetPath(ScriptableObject input)
        {
            return AssetDatabase.GetAssetPath(input);
        }

        public string GetBehaviourTreeXmlPath() => GetPath(behaviourTreeXml);
        public string GetNodeXmlPath() => GetPath(nodeXml);
        public string GetBehaviourTreeStylePath() => GetPath(behaviourTreeStyle);
        
        private static BehaviourTreeProjectSettings FindSettings()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(BehaviourTreeProjectSettings)}");
            if (guids.Length > 1)
            {
                Debug.LogWarning($"Found multiple settings files, using the first.");
            }

            switch (guids.Length)
            {
                case 0:
                    return null;
                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<BehaviourTreeProjectSettings>(path);
            }
        }

        internal static BehaviourTreeProjectSettings GetOrCreateSettings()
        {
            var settings = FindSettings();
            if (settings == null)
            {
                settings = CreateInstance<BehaviourTreeProjectSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/BehaviourTreeProjectSettings.asset");
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    // Register a SettingsProvider using UIElements for the drawing framework:
    internal static class MyCustomSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/BehaviourTreeProjectSettings", SettingsScope.Project)
            {
                label = "BehaviourTree",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = BehaviourTreeProjectSettings.GetSerializedSettings();

                    // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    var title = new Label()
                    {
                        text = "Behaviour Tree Settings"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new InspectorElement(settings));

                    rootElement.Bind(settings);
                },
            };

            return provider;
        }
    }
}