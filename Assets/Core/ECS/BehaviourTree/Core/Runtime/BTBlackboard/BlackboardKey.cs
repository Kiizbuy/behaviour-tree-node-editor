using UnityEngine;

namespace BehaviourTreeLogic
{
    [System.Serializable]
    public abstract class BlackboardKey : ISerializationCallbackReceiver
    {
        public string name;
        public System.Type underlyingType;
        public string typeName;

        protected BlackboardKey(System.Type underlyingType)
        {
            this.underlyingType = underlyingType;
            typeName = this.underlyingType.FullName;
        }

        public void OnBeforeSerialize()
        {
            typeName = underlyingType.AssemblyQualifiedName;
        }

        public void OnAfterDeserialize()
        {
            underlyingType = System.Type.GetType(typeName);
        }

        public abstract void CopyFrom(BlackboardKey key);
        public abstract bool Equals(BlackboardKey key);

        public static BlackboardKey CreateKey(System.Type type)
        {
            return System.Activator.CreateInstance(type) as BlackboardKey;
        }
    }

    [System.Serializable]
    public abstract class BlackboardKey<T> : BlackboardKey
    {
        public T value;

        public BlackboardKey() : base(typeof(T))
        {
        }

        public override string ToString()
        {
            return $"{name} : {value}";
        }

        public override void CopyFrom(BlackboardKey key)
        {
            if (key.underlyingType == underlyingType)
            {
                var other = key as BlackboardKey<T>;
                this.value = other.value;
            }
        }

        public override bool Equals(BlackboardKey key)
        {
            if (key.underlyingType == underlyingType)
            {
                var other = key as BlackboardKey<T>;
                return this.value.Equals(other.value);
            }

            return false;
        }
    }
}