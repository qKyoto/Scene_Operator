using System;
using System.Collections.Generic;
using System.Linq;
using Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public static class EditorConstants
    {
        public const string EDITOR_PATH = "Tools/Scene Operator";
        public const string MAIN_EDITOR_TITLE = "Scene Operator";
        public const string SETTINGS_EDITOR_TITLE = "Settings";
        public const string SCENE_COLLECTION_FILTER = "t:" + nameof(SceneCollection);
        public const string SCENE_COLLECTION_PATH = "Assets/";
        public const string SAVED_COLLECTION_KEY = "Saved Collection";
        public const string COLLECTIONS_PATH_KEY = "Collection Path";
    } 
    
    public class SceneOperatorEditor : EditorWindow
    {
        [SerializeField] private StyleSheet _styleSheet;
        
        private List<SceneCollection> _sceneCollections;
        private SceneCollection _selectedCollection;
        private SerializedObject _serializedCollection;
        private ScrollView _activeCollectionView;
        private SceneLoader _sceneLoader;
        
        private event Action ActiveCollectionViewChanged;
        
        [MenuItem(EditorConstants.EDITOR_PATH)]
        public static void ShowEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneOperatorEditor>();
            editorWindow.titleContent = new GUIContent(EditorConstants.MAIN_EDITOR_TITLE);
        }

        private void OnEnable()
        {
            _sceneCollections = new List<SceneCollection>();
            _sceneLoader = new SceneLoader();

            LoadSceneCollections();
            TrySelectSavedCollection();

            ActiveCollectionViewChanged += OnActiveCollectionViewChanged;
            SceneCollectionProcessor.CollectionCreated += OnCollectionCreated;
            SceneCollectionProcessor.CollectionDestroyed += OnCollectionDestroyed;
        }

        private void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(_styleSheet);
            DrawEditor();
        }

        private void LoadSceneCollections()
        {
            string[] guids = AssetDatabase.FindAssets(EditorConstants.SCENE_COLLECTION_FILTER, new[] { EditorConstants.SCENE_COLLECTION_PATH });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneCollection sceneCollection = AssetDatabase.LoadAssetAtPath<SceneCollection>(path);
                _sceneCollections.Add(sceneCollection);
            }
        }

        private void TrySelectSavedCollection()
        {
            string collectionId = EditorPrefs.GetString(EditorConstants.SAVED_COLLECTION_KEY);

            if (!IsValidCollection(collectionId))
            {
                EditorPrefs.DeleteKey(EditorConstants.SAVED_COLLECTION_KEY);
                return;
            }
            
            foreach (SceneCollection sceneCollection in _sceneCollections.Where(sceneCollection => sceneCollection.Id == collectionId))
            {
                _selectedCollection = sceneCollection;
                return;
            }
        }

        private bool IsValidCollection(string collectionId)
        {
            return _sceneCollections.Any(sceneCollection => sceneCollection.Id == collectionId);
        }

        private void OnActiveCollectionViewChanged()
        {
            _serializedCollection?.Dispose();
            _serializedCollection = new SerializedObject(_selectedCollection);
            _activeCollectionView.TrackSerializedObjectValue(_serializedCollection, _ =>
            {
                DrawEditor();
            });
        }

        private void OnCollectionCreated(SceneCollection sceneCollection)
        {
            _sceneCollections.Add(sceneCollection);
            DrawEditor();
        }

        private void OnCollectionDestroyed(SceneCollection sceneCollection)
        {
            _sceneCollections.Remove(sceneCollection);
            EditorPrefs.DeleteKey(sceneCollection.Id);

            if (_selectedCollection == sceneCollection)
                _selectedCollection = null;

            DrawEditor();
        }

        private void DrawEditor()
        {
            rootVisualElement.Clear();
            DrawDropDown();
            DropHorizontalLine();
            DrawSelectedCollection();
        }

        private void DropHorizontalLine()
        {
            Box horizontalLine = new();
            horizontalLine.AddToClassList("horizontal-line");
            rootVisualElement.Add(horizontalLine);
        }

        private void DrawDropDown()
        {
            DropdownField dropdownField = new();
            dropdownField.AddToClassList("container-dropdown");
            dropdownField.RegisterValueChangedCallback(OnSelectedCollectionChanged);

            foreach (SceneCollection sceneCollection in _sceneCollections)
                dropdownField.choices.Add(sceneCollection.name);
            
            if (_selectedCollection != null)
                dropdownField.SetValueWithoutNotify(_selectedCollection.name);
            
            rootVisualElement.Add(dropdownField);
        }
        
        private void OnSelectedCollectionChanged(ChangeEvent<string> changeEvent) 
        {
            _activeCollectionView?.Clear();
            ChangeSelectedCollection(changeEvent.newValue);
            DrawSelectedCollection();
        }

        private void ChangeSelectedCollection(string newValue)
        {
            foreach (SceneCollection sceneContainer in _sceneCollections.Where(sceneContainer => sceneContainer.name == newValue))
            {
                _selectedCollection = sceneContainer;
                EditorPrefs.SetString(EditorConstants.SAVED_COLLECTION_KEY, _selectedCollection.Id);
                break;
            }
        }
        
        private void DrawSelectedCollection()
        {
            if (_selectedCollection == null)
                return;
            
            _activeCollectionView = new ScrollView();
            _activeCollectionView.AddToClassList("active-collection-view");
            
            foreach (SceneGroup sceneGroup in _selectedCollection.SceneGroups)
            {
                (VisualElement groupHeader, VisualElement groupContent) collectionGroup = CreateCollectionGroup(sceneGroup);

                foreach (SceneAsset sceneAsset in sceneGroup.SceneAssets)
                {
                    if (sceneAsset == null)
                        continue;
                    
                    VisualElement sceneContent = CreateSceneGroupContent(sceneAsset);
                    collectionGroup.groupContent.Add(sceneContent);
                }
                
                _activeCollectionView.Add(collectionGroup.groupHeader);
                _activeCollectionView.Add(collectionGroup.groupContent);
            }
            
            ActiveCollectionViewChanged?.Invoke();
            rootVisualElement.Add(_activeCollectionView);
        }

        private (VisualElement groupHeader, VisualElement groupContent) CreateCollectionGroup(SceneGroup sceneGroup)
        {
            VisualElement groupHeader = new() { style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), flexGrow = 1 } };
            VisualElement groupContent = new();

            Label groupName = new() { text = sceneGroup.GroupName };
            Button toggleContentButton = new() { text = sceneGroup.GroupName/*"Expand"*/ };
            Button loadGroupAsyncButton = new() { text = "Additive load" };
            Button loadGroupSingleButton = new() { text = "Single load" };
            Button unloadGroupButton = new() { text = "Unload" };

            groupHeader.AddToClassList("group-header");
            groupContent.AddToClassList("zero-height");
            groupContent.AddToClassList("group-content");
            //groupContent.AddToClassList("scene-collection-content");
            //groupName.AddToClassList("");
            //toggleContentButton.AddToClassList("");
            //loadGroupButton.AddToClassList("");
            //unloadGroupButton.AddToClassList("");

            toggleContentButton.clicked += () => { ChangeSceneGroupVisibility(groupContent); };
            loadGroupAsyncButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Additive); };
            loadGroupSingleButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Single); };
            unloadGroupButton.clicked += () => { RequestToUploadMultipleScenes(sceneGroup); };

            //groupHeader.Add(groupName);
            groupHeader.Add(toggleContentButton);
            groupHeader.Add(loadGroupAsyncButton);
            groupHeader.Add(loadGroupSingleButton);
            groupHeader.Add(unloadGroupButton);

            (VisualElement groupHeader, VisualElement groupContent) result = (groupHeader, groupContent);
            return result;
        }
        
        private VisualElement CreateSceneGroupContent(SceneAsset sceneAsset)
        {
            VisualElement sceneContent = new() { style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), flexGrow = 1 } };
            Label sceneName = new() { text = sceneAsset.name };
            Button loadAdditiveButton = new() { text = "Additive load" };
            Button loadSingleButton = new() { text = "Single load" };
            Button unloadButton = new() { text = "Unload" };
            
            sceneContent.AddToClassList("");
            sceneName.AddToClassList("scene-name");
            loadAdditiveButton.AddToClassList("");
            loadSingleButton.AddToClassList("");
            unloadButton.AddToClassList("");

            loadAdditiveButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Additive); };
            loadSingleButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Single); };
            unloadButton.clicked += () => { RequestToUnloadSingleScene(sceneAsset); };
                    
            sceneContent.Add(sceneName);
            sceneContent.Add(loadAdditiveButton);
            sceneContent.Add(loadSingleButton);
            sceneContent.Add(unloadButton);

            return sceneContent;
        }
        
        private void ChangeSceneGroupVisibility(VisualElement groupContent)
        {
            if (groupContent.visible)
            {
                groupContent.RemoveFromClassList("scene-collection-content");
                groupContent.AddToClassList("zero-height");
            }
            else
            {
                groupContent.AddToClassList("scene-collection-content");
                groupContent.RemoveFromClassList("zero-height");
            }
        }

        private void RequestToLoadSingleScene(SceneAsset sceneAsset, OpenSceneMode openSceneMode)
        {
            _sceneLoader.LoadSingleScene(AssetDatabase.GetAssetPath(sceneAsset), openSceneMode);
        }

        private void RequestToLoadMultipleScenes(SceneGroup sceneGroup, OpenSceneMode openSceneMode)
        {
            List<string> scenePaths = (from sceneAsset in sceneGroup.SceneAssets where sceneAsset != null select AssetDatabase.GetAssetPath(sceneAsset)).ToList();
            _sceneLoader.LoadMultipleScenes(scenePaths, openSceneMode);
        }

        private void RequestToUnloadSingleScene(SceneAsset sceneAsset)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            _sceneLoader.UnloadSingleScene(sceneAsset.name);
        }
        
        private void RequestToUploadMultipleScenes(SceneGroup sceneGroup)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            List<string> sceneNames = (from sceneAsset in sceneGroup.SceneAssets where sceneAsset != null select sceneAsset.name).ToList();
            _sceneLoader.UnloadMultipleScenes(sceneNames);
        }
        
        private void OnDestroy()
        {
            ActiveCollectionViewChanged -= OnActiveCollectionViewChanged;
            SceneCollectionProcessor.CollectionCreated -= OnCollectionCreated;
            SceneCollectionProcessor.CollectionDestroyed -= OnCollectionDestroyed;
        }
    }
}