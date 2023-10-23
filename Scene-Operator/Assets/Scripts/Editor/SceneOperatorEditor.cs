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
        public const string SO_FILTER = "t:" + nameof(SceneCollection);
        public const string SAVED_COLLECTION_KEY = "Saved Collection";
        public const string COLLECTIONS_PATH_KEY = "Collection Path";
    } 
    
    public class SceneOperatorEditor : EditorWindow
    {
        [SerializeField] private StyleSheet _styleSheet;
        
        private List<SceneCollection> _sceneCollections;
        private SceneCollection _selectedCollection;
        private ScrollView _activeCollectionView;
        private SceneLoader _sceneLoader;
        
        private DropdownField _dropdownField; //?
        private VisualElement _topPanel; //?

        private SerializedObject _serializedCollection;
        
        [MenuItem(EditorConstants.EDITOR_PATH)]
        public static void ShowEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneOperatorEditor>();
            editorWindow.titleContent = new GUIContent(EditorConstants.MAIN_EDITOR_TITLE);
        }

        private void OnEnable()
        {
            //_sceneCollections = new List<SceneCollection>();
            _sceneLoader = new SceneLoader();

            SceneOperatorSettingsEditor.PathChanged += RedrawEditor;
            SceneOperatorSettingsEditor.NewCollectionCreated += OnNewCollectionCreated;
            
            LoadSceneCollections();
            TrySelectSavedCollection();

            /*_serializedCollection = new SerializedObject(_selectedCollection);
            rootVisualElement.TrackSerializedObjectValue(_serializedCollection);*/
        }

        private void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(_styleSheet);
            DrawTopPanel();
            DrawSelectedCollection();
        }
        
        private void RedrawEditor()
        {
            Debug.Log("redraw editor");
            rootVisualElement.Clear();
            //_sceneCollections = new List<SceneCollection>();
            _selectedCollection = null;
            LoadSceneCollections();
            TrySelectSavedCollection(); //?
            DrawTopPanel();
        }

        private void OnNewCollectionCreated()
        {
            LoadSceneCollections();
            UpdatedDropdownChoices();
        }
        
        private void DrawTopPanel()
        {
            _topPanel = new VisualElement();
            _topPanel.AddToClassList("top-panel");
            _topPanel.Add(DrawDropDown());
            _topPanel.Add(DrawSettingsButton());
            rootVisualElement.Add(_topPanel);
        }

        private Button DrawSettingsButton()
        {
            Button settingsButton = new() /*{ text = SETTINGS_TITLE }*/;
            settingsButton.AddToClassList("settings-button");
            settingsButton.clicked += OpenSettingsWindow;
            return settingsButton;
        }

        private void OpenSettingsWindow()
        {
            SceneOperatorSettingsEditor.OpenEditor();
        }

        private void LoadSceneCollections()
        {
            string collectionsPath = EditorPrefs.GetString(EditorConstants.COLLECTIONS_PATH_KEY);
            
            if (string.IsNullOrEmpty(collectionsPath))
                return;

            _sceneCollections = new List<SceneCollection>();
            string[] guids = AssetDatabase.FindAssets(EditorConstants.SO_FILTER, new[] { collectionsPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneCollection sceneCollection = AssetDatabase.LoadAssetAtPath<SceneCollection>(path);
                _sceneCollections.Add(sceneCollection);
            }
        }

        private DropdownField DrawDropDown()
        {
            _dropdownField = new DropdownField();
            _dropdownField.AddToClassList("container-dropdown");
            _dropdownField.RegisterValueChangedCallback(OnSceneCollectionChanged);
           
            foreach (SceneCollection sceneCollection in _sceneCollections)
                _dropdownField.choices.Add(sceneCollection.name);

            if (_selectedCollection != null)
                _dropdownField.SetValueWithoutNotify(_selectedCollection.name);
            
            return _dropdownField;
            //rootVisualElement.Add(_dropdownField);
        }

        private void UpdatedDropdownChoices()
        {
            _dropdownField.choices.Clear();
            foreach (SceneCollection sceneCollection in _sceneCollections)
                _dropdownField.choices.Add(sceneCollection.name);
        }

        private void OnSceneCollectionChanged(ChangeEvent<string> changeEvent) 
        {
            _activeCollectionView?.Clear();
            ChangeSelectedCollection();
            DrawSelectedCollection();
        }

        private void ChangeSelectedCollection()
        {
            foreach (SceneCollection sceneContainer in _sceneCollections.Where(sceneContainer => sceneContainer.name == _dropdownField.value))
            {
                _selectedCollection = sceneContainer;
                EditorPrefs.SetString(EditorConstants.SAVED_COLLECTION_KEY, _selectedCollection.Id);
                break;
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
        
        //--------------------------
        private void DrawSelectedCollection()
        {
            if (_selectedCollection == null)
                return;
            
            _activeCollectionView = new ScrollView();
            
            foreach (SceneGroup sceneGroup in _selectedCollection.SceneGroups)
            {
                (VisualElement groupHeader, VisualElement groupContent) collectionGroup = CreateCollectionGroup(sceneGroup);

                foreach (SceneAsset sceneAsset in sceneGroup.SceneAssets)
                {
                    if (sceneAsset == null)
                        continue;
                    
                    VisualElement sceneContent = CreateSceneContent(sceneAsset);
                    collectionGroup.groupContent.Add(sceneContent);
                }
                
                _activeCollectionView.Add(collectionGroup.groupHeader);
                _activeCollectionView.Add(collectionGroup.groupContent);
            }
            
            rootVisualElement.Add(_activeCollectionView);
        }

        private (VisualElement groupHeader, VisualElement groupContent) CreateCollectionGroup(SceneGroup sceneGroup)
        {
            VisualElement groupHeader = new() { style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), flexGrow = 1 } };
            VisualElement groupContent = new();

            Label groupName = new() { text = sceneGroup.GroupName };
            Button toggleContentButton = new() { text = "content" };
            Button loadGroupButton = new() { text = "Load group" };
            Button unloadGroupButton = new() { text = "Unload group" };

            //groupHeader.AddToClassList("");
            groupContent.AddToClassList("zero-height");
            //groupName.AddToClassList("");
            //toggleContentButton.AddToClassList("");
            //loadGroupButton.AddToClassList("");
            //unloadGroupButton.AddToClassList("");

            toggleContentButton.clicked += () => { ChangeSceneGroupVisibility(groupContent); };
            loadGroupButton.clicked += () => { RequestToLoadMultipleScenes(sceneGroup); };
            unloadGroupButton.clicked += () => { RequestToUploadMultipleScenes(sceneGroup); };

            groupHeader.Add(groupName);
            groupHeader.Add(toggleContentButton);
            groupHeader.Add(loadGroupButton);
            groupHeader.Add(unloadGroupButton);

            (VisualElement groupHeader, VisualElement groupContent) result = (groupHeader, groupContent);
            return result;
        }
        
        private VisualElement CreateSceneContent(SceneAsset sceneAsset)
        {
            VisualElement sceneContent = new() { style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), flexGrow = 1 } };
            Label sceneName = new() { text = sceneAsset.name };
            Button loadAdditiveButton = new() { text = "Additive" };
            Button loadSingleButton = new() { text = "Single" };
            Button unloadButton = new() { text = "Unload" };
            
            /*sceneContent.AddToClassList("");
            sceneName.AddToClassList("");
            loadAdditiveButton.AddToClassList("");
            loadSingleButton.AddToClassList("");
            unloadButton.AddToClassList("");*/

            loadAdditiveButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Additive); };
            loadSingleButton.clicked += () => { RequestToLoadSingleScene(sceneAsset, OpenSceneMode.Single); };
            unloadButton.clicked += () => { RequestToUnloadSingleScene(sceneAsset); };
                    
            sceneContent.Add(sceneName);
            sceneContent.Add(loadAdditiveButton);
            sceneContent.Add(loadSingleButton);
            sceneContent.Add(unloadButton);

            return sceneContent;
        }
        
        //--------------------
        private void ChangeSceneGroupVisibility(VisualElement groupContent)
        {
            if (groupContent.visible)
                groupContent.AddToClassList("zero-height");
            else
                groupContent.RemoveFromClassList("zero-height");
        }

        private void RequestToLoadSingleScene(SceneAsset sceneAsset, OpenSceneMode openSceneMode)
        {
            _sceneLoader.LoadSingleScene(AssetDatabase.GetAssetPath(sceneAsset), openSceneMode);
        }

        private void RequestToLoadMultipleScenes(SceneGroup sceneGroup)
        {
            List<string> scenePaths = (from sceneAsset in sceneGroup.SceneAssets where sceneAsset != null select AssetDatabase.GetAssetPath(sceneAsset)).ToList();
            _sceneLoader.LoadMultipleScenes(scenePaths);
        }

        private void RequestToUnloadSingleScene(SceneAsset sceneAsset)
        {
            _sceneLoader.UnloadSingleScene(sceneAsset.name);
        }
        
        private void RequestToUploadMultipleScenes(SceneGroup sceneGroup)
        {
            List<string> sceneNames = (from sceneAsset in sceneGroup.SceneAssets where sceneAsset != null select sceneAsset.name).ToList();
            _sceneLoader.UnloadMultipleScenes(sceneNames);
        }

        //--------------------
        
        private void OnDestroy()
        {
            SceneOperatorSettingsEditor.PathChanged -= RedrawEditor;
            SceneOperatorSettingsEditor.NewCollectionCreated -= OnNewCollectionCreated;
        }
    }
}