using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace BehaviourTreeLogic
{
    [UxmlElement]
    public partial class OverlayView : VisualElement
    {
        public Action<BehaviourTree> OnTreeSelected;

        private Button createButton;
        private VisualElement listViewContainer;
        private MultiColumnListView projectListView;

        private string NameColumn = "Name";
        private string PathColumn = "Path";
        private List<string> assetPaths;

        private Column CreateColumn(string name)
        {
            var column = new Column();
            column.name = name;
            column.title = name;
            column.width = 100.0f;
            column.stretchable = true;
            return column;
        }

        private MultiColumnListView CreateListView()
        {
            var listView = new MultiColumnListView();
            listView.showBorder = true;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            listView.fixedItemHeight = 30.0f;
            listView.showBoundCollectionSize = false;
            listView.showAddRemoveFooter = false;
            listView.reorderable = false;
            listView.itemsSource = assetPaths;

            listView.columns.Add(CreateColumn(NameColumn));
            listView.columns.Add(CreateColumn(PathColumn));

            listView.columns[NameColumn].makeCell = () => new Label();
            listView.columns[PathColumn].makeCell = () => new Label();

            listView.columns[NameColumn].bindCell = BindName;
            listView.columns[PathColumn].bindCell = BindPath;

            return listView;
        }

        private void BindName(VisualElement element, int index)
        {
            var label = element as Label;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPaths[index]);
            label.text = fileName;
        }

        private void BindPath(VisualElement element, int index)
        {
            var label = element as Label;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.text = assetPaths[index];
        }

        public void Show()
        {
            // Hidden in UIBuilder while editing..
            style.display = DisplayStyle.Flex;
            style.visibility = Visibility.Visible;

            // Configure fields
            createButton = this.Q<Button>("CreateButton");
            listViewContainer = this.Q<VisualElement>("ListViewContainer");

            // Find all behaviour tree assets
            assetPaths = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
            assetPaths.Sort();

            // Configure create asset button
            createButton.clicked -= OnCreateAsset;
            createButton.clicked += OnCreateAsset;

            projectListView = CreateListView();
            listViewContainer.Clear();
            listViewContainer.Add(projectListView);
            projectListView.selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IEnumerable<object> obj)
        {
            OnOpenAsset();
        }

        public void Hide()
        {
            style.visibility = Visibility.Hidden;
        }

        public string ToMenuFormat(string one)
        {
            // Using the slash creates submenus...
            return one.Replace("/", "|");
        }

        public string ToAssetFormat(string one)
        {
            // Using the slash creates submenus...
            return one.Replace("|", "/");
        }

        private void OnOpenAsset()
        {
            var path = assetPaths[projectListView.selectedIndex];

            var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
            if (tree)
            {
                TreeSelected(tree);
                style.visibility = Visibility.Hidden;
            }
        }

        private void OnCreateAsset()
        {
            var tree = BehaviourTreeEditorUtility.CreateNewTree();
            if (tree)
            {
                TreeSelected(tree);
                style.visibility = Visibility.Hidden;
            }
        }

        private void TreeSelected(BehaviourTree tree)
        {
            OnTreeSelected.Invoke(tree);
        }
    }
}