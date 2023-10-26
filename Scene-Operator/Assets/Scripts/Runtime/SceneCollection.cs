using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Runtime
{
    [CreateAssetMenu(fileName = "Scene Collection", menuName = "Scene Operator/Scene Collection", order = 50)]
    public class SceneCollection : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SceneGroup> _sceneGroups = new();
        [SerializeField, HideInInspector] private string _id;

        private string _idDump;

        private bool IsIdEmpty => string.IsNullOrEmpty(_id);

        public string Id => _id;
        public IEnumerable<SceneGroup> SceneGroups => _sceneGroups;

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

        //one development
        [ContextMenu("Clear prefs")]
        private void ClearPrefs()
        {
            EditorPrefs.DeleteAll();
        }
    }
}