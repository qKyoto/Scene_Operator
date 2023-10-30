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
    public class SceneOperatorEditor : EditorWindow
    {
        [SerializeField] private StyleSheet _styleSheet;
        
        private List<SceneCollection> _sceneCollections;
        private SceneCollection _selectedCollection;
        private SerializedObject _serializedCollection;
        private ScrollView _activeCollectionView;
        private SceneLoader _sceneLoader;
        
        private event Action ActiveCollectionViewChanged;
        
        [MenuItem(EditorMainConstants.EDITOR_PATH)]
        public static void ShowEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneOperatorEditor>();
            editorWindow.titleContent = new GUIContent(EditorMainConstants.MAIN_EDITOR_TITLE);
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
            string[] guids = AssetDatabase.FindAssets(EditorMainConstants.SCENE_COLLECTION_FILTER, new[] { EditorMainConstants.SCENE_COLLECTION_PATH });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneCollection sceneCollection = AssetDatabase.LoadAssetAtPath<SceneCollection>(path);
                _sceneCollections.Add(sceneCollection);
            }
        }

        private void TrySelectSavedCollection()
        {
            string collectionId = EditorPrefs.GetString(EditorMainConstants.SAVED_COLLECTION_KEY);

            if (!IsValidCollection(collectionId))
            {
                EditorPrefs.DeleteKey(EditorMainConstants.SAVED_COLLECTION_KEY);
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
            DrawSignature();
        }

        private void DropHorizontalLine()
        {
            Box horizontalLine = new();
            horizontalLine.AddToClassList(EditorStyles.HORIZONTAL_LINE);
            rootVisualElement.Add(horizontalLine);
        }
        
        private void DrawDropDown()
        {
            DropdownField dropdownField = new();
            dropdownField.AddToClassList(EditorStyles.CONTAINER_DROPDOWN);
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
            
            if (rootVisualElement.Contains(_activeCollectionView))
                rootVisualElement.Remove(_activeCollectionView);
            
            ChangeSelectedCollection(changeEvent.newValue);
            DrawSelectedCollection();
        }

        private void ChangeSelectedCollection(string newValue)
        {
            foreach (SceneCollection sceneContainer in _sceneCollections.Where(sceneContainer => sceneContainer.name == newValue))
            {
                _selectedCollection = sceneContainer;
                EditorPrefs.SetString(EditorMainConstants.SAVED_COLLECTION_KEY, _selectedCollection.Id);
                break;
            }
        }
        
        private void DrawSelectedCollection()
        {
            if (_selectedCollection == null)
                return;
            
            _activeCollectionView = new ScrollView();
            _activeCollectionView.AddToClassList(EditorStyles.ACTIVE_COLLECTION_VIEW);
        
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

            groupContent.AddToClassList(EditorStyles.ZERO_HEIGHT);
            unloadGroupButton.AddToClassList(EditorStyles.UNLOAD_BUTTON);

            toggleContentButton.clicked += () => { ChangeSceneGroupVisibility(groupContent); };
            loadGroupAdditiveButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Additive); };
            loadGroupSingleButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup, OpenSceneMode.Single); };
            unloadGroupButton.clicked += () => { RequestToUploadMultipleScenes(sceneGroup); };
            _sceneLoader.ScenesHasChanged += () => { UpdateStateOfTheGroupButtons(sceneGroup, loadGroupAdditiveButton, loadGroupSingleButton); };
            
            UpdateStateOfTheGroupButtons(sceneGroup, loadGroupAdditiveButton, loadGroupSingleButton);

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
            
            sceneName.AddToClassList(EditorStyles.SCENE_NAME_LABEL);
            unloadButton.AddToClassList(EditorStyles.UNLOAD_BUTTON);
            
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
                groupContent.RemoveFromClassList(EditorStyles.SCENE_COLLECTION_CONTENT);
                groupContent.AddToClassList(EditorStyles.ZERO_HEIGHT);
            }
            else
            {
                groupContent.AddToClassList(EditorStyles.SCENE_COLLECTION_CONTENT);
                groupContent.RemoveFromClassList(EditorStyles.ZERO_HEIGHT);
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
                loadGroupAdditiveButton.AddToClassList(EditorStyles.HIGHLIGHT_BUTTON);
                loadGroupSingleButton.AddToClassList(EditorStyles.HIGHLIGHT_BUTTON);
            }
            else
            {
                loadGroupAdditiveButton.RemoveFromClassList(EditorStyles.HIGHLIGHT_BUTTON);
                loadGroupSingleButton.RemoveFromClassList(EditorStyles.HIGHLIGHT_BUTTON);
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
            return sceneGroup.SceneAssets.All(sceneAsset => _sceneLoader.IsLoadedScene(sceneAsset != null ? sceneAsset.name : null));
        }

        private bool IsAllGroupSceneUnloaded(SceneGroup sceneGroup)
        {
            return sceneGroup.SceneAssets.All(sceneAsset => !_sceneLoader.IsLoadedScene(sceneAsset != null ? sceneAsset.name : null));
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
        
        private void DrawSignature()
        {
            VisualElement signature = new();
            Label prefixLabel = new() { text = EditorMainConstants.SIGNATURE_PREFIX_LABEL };
            Image icon = new();
            Label postfixLabel = new() { text = EditorMainConstants.SIGNATURE_POSTFIX_LABEL };
            
            signature.AddToClassList(EditorStyles.SIGNATURE);
            prefixLabel.AddToClassList(EditorStyles.SIGNATURE_LABEL);
            postfixLabel.AddToClassList(EditorStyles.SIGNATURE_LABEL);
            icon.AddToClassList(EditorStyles.SIGNATURE_ICON);
            
            signature.Add(prefixLabel);
            signature.Add(icon);
            signature.Add(postfixLabel);
            rootVisualElement.Add(signature);
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