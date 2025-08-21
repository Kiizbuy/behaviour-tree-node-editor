using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace BehaviourTreeLogic
{
    [InitializeOnLoad]
    public static class PropertyBinder
    {
        private class BoundLabel
        {
            public Label label;
            public Func<string> getter;
            public string lastValue;
        }

        private static readonly List<BoundLabel> bindings = new();

        static PropertyBinder()
        {
            EditorApplication.update += Update;
        }

        public static void BindLabel(Label label, Func<string> getter)
        {
            if (label == null || getter == null)
                return;

            var bound = new BoundLabel
            {
                label = label,
                getter = getter,
                lastValue = getter()
            };

            label.text = bound.lastValue;
            bindings.Add(bound);
        }

        private static void Update()
        {
            foreach (var bound in bindings)
            {
                if (bound.label == null)
                    continue;

                var current = bound.getter();
                if (current != bound.lastValue)
                {
                    bound.label.text = current;
                    bound.lastValue = current;
                }
            }

            bindings.RemoveAll(b => b.label == null);
        }
    }

}