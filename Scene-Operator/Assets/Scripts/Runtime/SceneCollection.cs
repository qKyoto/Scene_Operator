using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Runtime
{
    public class SceneCollection : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SceneGroup> _sceneGroups = new();
        [SerializeField, HideInInspector] private string _id;

        private string _idDump;

        private bool IsIdEmpty => string.IsNullOrEmpty(_id);

        public string Id => _id;
        public IEnumerable<SceneGroup> SceneGroups => _sceneGroups;

        public event Action CollectionDestroyed;

        private void OnValidate()
        {
            TryCreateId();
        }

        private void TryCreateId()
        {
            if (!IsIdEmpty)
                return;
            
            _id = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
        }
        
        public void OnBeforeSerialize()
        {
            _idDump = _id;
        }

        public void OnAfterDeserialize()
        {
            _id = _idDump;
        }

        private void OnDestroy()
        {
            CollectionDestroyed?.Invoke();
        }

        //one development
        [ContextMenu("Clear prefs")]
        private void ClearPrefs()
        {
            EditorPrefs.DeleteAll();
        }
    }
}