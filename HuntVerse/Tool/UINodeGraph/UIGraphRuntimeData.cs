using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    [Serializable]
    public class UIGraphRuntimeData
    {
        public List<UIGraphExecutionStep> executionSteps = new List<UIGraphExecutionStep>();
    }
    
    [Serializable]
    public class UIGraphExecutionStep
    {
        public UINodeType nodeType;
        public string nodeGuid;
        public SerializableDictionary<string, string> stringParams = new SerializableDictionary<string, string>();
        public SerializableDictionary<string, int> intParams = new SerializableDictionary<string, int>();
        public SerializableDictionary<string, float> floatParams = new SerializableDictionary<string, float>();
        public string[] layerParams;
        public string[] gameObjectIds; // Bake 시점에 부여된 고유 ID
    }
    
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
        
        public void OnAfterDeserialize()
        {
            dictionary.Clear();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                dictionary[keys[i]] = values[i];
            }
        }
        
        public void Add(TKey key, TValue value)
        {
            dictionary[key] = value;
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }
        
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }
    }
}

