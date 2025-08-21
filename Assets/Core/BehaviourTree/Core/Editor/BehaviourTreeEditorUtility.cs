using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif
namespace BehaviourTreeLogic
{
    public static class BehaviourTreeEditorUtility
    {
        public struct ScriptTemplate
        {
            public TextAsset templateFile;
            public string defaultFileName;
            public string subFolder;
        }

        [System.Serializable]
        public class PackageManifest
        {
            public string name;
            public string version;
        }


        public static BehaviourTree CreateNewTree()
        {
            var settings = BehaviourTreeEditorWindow.Instance.settings;

            var savePath =
                EditorUtility.SaveFilePanel("Create New", settings.newTreePath, "New Behavior Tree",
                    "asset");
            if (string.IsNullOrEmpty(savePath))
            {
                return null;
            }

            var assetName = System.IO.Path.GetFileNameWithoutExtension(savePath);
            var folder = System.IO.Path.GetDirectoryName(savePath);
            folder = folder.Substring(folder.IndexOf("Assets"));


            var path = System.IO.Path.Join(folder, $"{assetName}.asset");
            if (System.IO.File.Exists(path))
            {
                Debug.LogError($"Failed to create behaviour tree asset: Path already exists:{assetName}");
                return null;
            }

            var tree = ScriptableObject.CreateInstance<BehaviourTree>();
            tree.name = assetName;
            AssetDatabase.CreateAsset(tree, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(tree);
#if UNITY_ADDRESSABLES

            var settingsAsset = AddressableAssetSettingsDefaultObject.Settings;
            if (settingsAsset == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Please setup Addressables in the project.");
                return tree;
            }

            var assetEntry = settingsAsset.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), FindOrCreateGroup(settingsAsset, "BehaviourTrees"));
            assetEntry.address = assetName;

            settingsAsset.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, assetEntry, true);
            AssetDatabase.SaveAssets();
#endif
            return tree;
        }
#if UNITY_ADDRESSABLES

        private static AddressableAssetGroup FindOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null, typeof(BundledAssetGroupSchema));
            }
            return group;
        }
        
        #endif

        public static void CreateNewScript(ScriptTemplate scriptTemplate, NodeView source, bool isSourceParent,
            Vector2 position)
        {
            BehaviourTreeEditorWindow.Instance.newScriptDialog.CreateScript(scriptTemplate, source, isSourceParent,
                position);
        }


        public static List<T> LoadAssets<T>() where T : Object
        {
            var assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var assets = new List<T>();
            foreach (var assetId in assetIds)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetId);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                assets.Add(asset);
            }

            return assets;
        }

        public static List<string> GetAssetPaths<T>() where T : Object
        {
            var assetIds = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var paths = new List<string>();
            foreach (var assetId in assetIds)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetId);
                paths.Add(path);
            }

            return paths;
        }

        public static TextAsset GetNodeScriptPath(NodeView nodeView)
        {
            var nodeName = nodeView.node.GetType().Name;
            var assetGuids = AssetDatabase.FindAssets($"t:TextAsset {nodeName}");
            for (var i = 0; i < assetGuids.Length; ++i)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                var filename = System.IO.Path.GetFileName(path);
                if (filename == $"{nodeName}.cs")
                {
                    var script = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    return script;
                }
            }

            return null;
        }

        public static void OpenScriptInEditor(NodeView nodeView)
        {
            var script = GetNodeScriptPath(nodeView);
            if (script)
            {
                // Open script in the editor:
                AssetDatabase.OpenAsset(script);
            }
        }
    }
}