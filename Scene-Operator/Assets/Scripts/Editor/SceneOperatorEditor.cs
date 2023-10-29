using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        public const int DELAY_AFTER_SCENE_CLOSED = 10;
    }

    public static class EditorStyles
    {
        
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

            Label remark = new Label
            {
                text = "by Kyoto"
            };
            remark.AddToClassList("remark");
            //rootVisualElement.Add(remark);
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

            Button toggleContentButton = new() { text = sceneGroup.GroupName };
            Button loadGroupAdditiveButton = new() { text = "Group (additive)" };
            Button loadGroupSingleButton = new() { text = "Group (single)" };
            Button unloadGroupButton = new();

            //groupHeader.AddToClassList("group-header");
            groupContent.AddToClassList("zero-height");
            //groupContent.AddToClassList("scene-group-content");
            //groupContent.AddToClassList("scene-collection-content");
            //groupName.AddToClassList("");
            //toggleContentButton.AddToClassList("");
            //loadGroupButton.AddToClassList("");
            unloadGroupButton.AddToClassList("unload-button");

            toggleContentButton.clicked += () => { ChangeSceneGroupVisibility(groupContent); };
            loadGroupAdditiveButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Additive); };
            loadGroupSingleButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Single); };
            unloadGroupButton.clicked += () => { RequestToUploadMultipleScenes(sceneGroup); };
            _sceneLoader.ScenesHasChanged += () => { UpdateStateOfTheGroupButtons(sceneGroup, loadGroupAdditiveButton, loadGroupSingleButton); };
            
            UpdateStateOfTheGroupButtons(sceneGroup, loadGroupAdditiveButton, loadGroupSingleButton);

            //groupHeader.Add(groupName);
            groupHeader.Add(toggleContentButton);
            groupHeader.Add(loadGroupAdditiveButton);
            groupHeader.Add(loadGroupSingleButton);
            groupHeader.Add(unloadGroupButton);

            (VisualElement groupHeader, VisualElement groupContent) result = (groupHeader, groupContent);
            return result;
        }
        
        private VisualElement CreateSceneGroupContent(SceneAsset sceneAsset)
        {
            VisualElement sceneContent = new() { style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), flexGrow = 1 } };
            Label sceneName = new() { text = sceneAsset.name };
            Button loadAdditiveButton = new() { text = "Scene (additive)" };
            Button loadSingleButton = new() { text = "Scene (single)" };
            Button unloadButton = new();
            
            sceneContent.AddToClassList("");
            sceneName.AddToClassList("scene-name-label");
            loadAdditiveButton.AddToClassList("");
            loadSingleButton.AddToClassList("");
            unloadButton.AddToClassList("unload-button");
            
            loadAdditiveButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Additive); };
            loadSingleButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Single); };
            unloadButton.clicked += () => { RequestToUnloadSingleScene(sceneAsset); };
            _sceneLoader.ScenesHasChanged += () => { UpdateStateSceneButtons(sceneAsset, loadAdditiveButton, loadSingleButton); };
            
            UpdateStateSceneButtons(sceneAsset, loadAdditiveButton, loadSingleButton);
            
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
        
        private void UpdateStateOfTheGroupButtons(SceneGroup sceneGroup, Button loadGroupAdditiveButton, Button loadGroupSingleButton)
        {
            bool isAllGroupSceneLoaded = IsAllGroupSceneLoaded(sceneGroup);
            bool isAllGroupSceneUnloaded = IsAllGroupSceneUnloaded(sceneGroup);
                
            loadGroupAdditiveButton.SetEnabled(!isAllGroupSceneLoaded);
            loadGroupSingleButton.SetEnabled(!isAllGroupSceneLoaded);

            if (!isAllGroupSceneUnloaded && !isAllGroupSceneLoaded)
            {
                loadGroupAdditiveButton.AddToClassList("highlight-button");
                loadGroupSingleButton.AddToClassList("highlight-button");
            }
            else
            {
                loadGroupAdditiveButton.RemoveFromClassList("highlight-button");
                loadGroupSingleButton.RemoveFromClassList("highlight-button");
            }
        }
        
        private void UpdateStateSceneButtons(SceneAsset sceneAsset, Button loadAdditiveButton, Button loadSingleButton)
        {
            bool isLoadedScene = _sceneLoader.IsLoadedScene(sceneAsset.name);
            loadAdditiveButton.SetEnabled(!isLoadedScene);
            loadSingleButton.SetEnabled(!isLoadedScene);
        }
        
        private bool IsAllGroupSceneLoaded(SceneGroup sceneGroup)
        {
            return sceneGroup.SceneAssets.All(sceneAsset => _sceneLoader.IsLoadedScene(sceneAsset.name));
        }

        private bool IsAllGroupSceneUnloaded(SceneGroup sceneGroup)
        {
            return sceneGroup.SceneAssets.All(sceneAsset => !_sceneLoader.IsLoadedScene(sceneAsset.name));
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
            _sceneLoader.Dispose();
            
            ActiveCollectionViewChanged -= OnActiveCollectionViewChanged;
            SceneCollectionProcessor.CollectionCreated -= OnCollectionCreated;
            SceneCollectionProcessor.CollectionDestroyed -= OnCollectionDestroyed;
        }
    }
}