using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeLogic
{
    public partial class HorizontalTabView : TabView
    {
        private ScrollView _horizontalScrollView;

        public new class UxmlFactory : UxmlFactory<HorizontalTabView, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _reorderable = new() { name = "reorderable" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve is HorizontalTabView tabView)
                {
                    tabView.reorderable = _reorderable.GetValueFromBag(bag, cc);
                }
            }
        }

        public HorizontalTabView()
        {
            //мы не можем просто так взять и получить визуальный контейнер т.к он internal - блядские юнитеки
            var headerField = typeof(TabView).GetField("m_HeaderContainer", BindingFlags.NonPublic | BindingFlags.Instance);
            var headerContainer = (VisualElement)headerField?.GetValue(this);

            if (headerContainer == null)
            {
                Debug.LogError("in TabView we don't found a HeaderContainer.");
                return;
            }

            _horizontalScrollView = new ScrollView(ScrollViewMode.Horizontal)
            {
                name = "horizontal-tab-scroll-view",
                pickingMode = PickingMode.Ignore
            };

            _horizontalScrollView.AddToClassList("horizontal-tab-scroll-view");
            headerContainer.RemoveFromHierarchy();
            _horizontalScrollView.Add(headerContainer);
            hierarchy.Insert(0, _horizontalScrollView);
        }
    }
}