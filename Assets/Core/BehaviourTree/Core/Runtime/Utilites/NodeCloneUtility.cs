using System;
using System.Reflection;

namespace BehaviourTreeLogic
{
    public static class NodeCloneUtility
    {
        public static T DeepClone<T>(T obj) where T : Node
        {
            if (obj == null)
                return default;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var type = obj.GetType();
            var clone = (T)Activator.CreateInstance(type);
            var fields = type.GetFields(flags);
            var properties = type.GetProperties(flags);

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);

                if (value != null)
                {
                    var fieldType = field.FieldType;

                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(NodeProperty<>))
                    {
                        var clonedNodeProperty = Activator.CreateInstance(fieldType);

                        var defaultValueField = fieldType.GetField("defaultValue");
                        if (defaultValueField != null)
                        {
                            var defaultValue = defaultValueField.GetValue(value);
                            defaultValueField.SetValue(clonedNodeProperty, defaultValue);
                        }

                        var referenceField = fieldType.GetField("reference");
                        if (referenceField != null)
                        {
                            var originalReference = referenceField.GetValue(value) as BlackboardKey;
                            var clonedReference = CloneBlackboardKey(originalReference);
                            referenceField.SetValue(clonedNodeProperty, clonedReference);
                        }

                        field.SetValue(clone, clonedNodeProperty);
                        continue;
                    }
                }

                field.SetValue(clone, value);
            }

            foreach (var property in properties)
            {
                if (!property.CanWrite) 
                    continue;
                var value = property.GetValue(obj, null);
                property.SetValue(clone, value, null);
            }

            return clone;
        }

        private static BlackboardKey CloneBlackboardKey(BlackboardKey key)
        {
            if (key == null)
                return null;

            var clonedKey = BlackboardKey.CreateKey(key.GetType());

            if (clonedKey != null)
            {
                clonedKey.CopyFrom(key);
                clonedKey.name = key.name;
            }

            return clonedKey;
        }
    }
}