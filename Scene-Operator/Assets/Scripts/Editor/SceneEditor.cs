using System.Collections.Generic;
using System.Linq;
using Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class SceneEditor : EditorWindow
    {
        private const string EDITOR_TITLE = "Scene Operator";
        private const string SETTINGS_TITLE = "Settings";
        private const string SO_FILTER = "t:ScriptableObject";
        private const string DROPDOWN_TITLE = "Current container";
        private const string SAVED_CONTAINER_KEY = "Saved Container";

        [SerializeField] private StyleSheet _styleSheet;
        
        private List<SceneContainer> _sceneContainers;
        private SceneContainer _activeContainer;
        private ScrollView _activeContainerView;
        private DropdownField _dropdownField;
        private SceneLoader _sceneLoader;

        private VisualElement _topPanel;
        
        [MenuItem("Tools/Scene Manager/Scene Manager Window")]
        public static void ShowEditor()
        {
            EditorWindow editorWindow = GetWindow<SceneEditor>();
            editorWindow.titleContent = new GUIContent(EDITOR_TITLE);
        }

        private void OnEnable()
        {
            EditorSettings.PathChanged += OnPathChanged;
            _sceneContainers = new List<SceneContainer>();
            _sceneLoader = new SceneLoader();
            FindSceneContainers();
        }

        private void OnPathChanged()
        {
            Debug.Log("blablanya");
        }

        private void CreateGUI()
        {
            rootVisualElement.styleSheets.Add(_styleSheet);
            DrawEditor();
        }

        private void DrawEditor()
        {
            DrawTopPanel();
            TrySelectSavedContainer();
            DrawSelectedContainer();
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
            EditorWindow editorWindow = GetWindow<EditorSettings>();
            editorWindow.titleContent = new GUIContent(SETTINGS_TITLE);
        }

        private void FindSceneContainers()
        {
            string[] guids = AssetDatabase.FindAssets(SO_FILTER, new[] { "Assets/Resources/Scene Containers" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SceneContainer sceneContainer = AssetDatabase.LoadAssetAtPath<SceneContainer>(path);
                _sceneContainers.Add(sceneContainer);
            }
        }

        private DropdownField DrawDropDown()
        {
            _dropdownField = new DropdownField { /*label = DROPDOWN_TITLE*/ };
            _dropdownField.AddToClassList("container-dropdown");
            _dropdownField.RegisterValueChangedCallback(OnSelectedSceneContainerChanger);
           
            foreach (SceneContainer sceneContainer in _sceneContainers)
                _dropdownField.choices.Add(sceneContainer.name);

            return _dropdownField;
            //rootVisualElement.Add(_dropdownField);
        }

        private void OnSelectedSceneContainerChanger(ChangeEvent<string> changeEvent) 
        {
            _activeContainerView?.Clear();
            TryChangeActiveContainer();
            DrawSelectedContainer();
        }

        private void TryChangeActiveContainer()
        {
            foreach (SceneContainer sceneContainer in _sceneContainers.Where(sceneContainer => sceneContainer.name == _dropdownField.value))
            {
                _activeContainer = sceneContainer;
                EditorPrefs.SetString(SAVED_CONTAINER_KEY, _activeContainer.name);
                break;
            }
        }

        private void TrySelectSavedContainer()
        {
            _dropdownField.SetValueWithoutNotify(EditorPrefs.GetString(SAVED_CONTAINER_KEY));
            TryChangeActiveContainer();
        }

        private void DrawSelectedContainer()
        {
            if (_activeContainer == null)
                return;
            
            _activeContainerView = new ScrollView();
            
            foreach (SceneDataElement sceneDataElement in _activeContainer.SceneDataElements)
            {
                VisualElement sceneContent = new();
                sceneContent.AddToClassList("zero-height");
                
                Button buttonContainer = new() { text = "content", style = { width = 100 } };
                buttonContainer.clicked += () =>
                {
                    if (sceneContent.visible)
                        sceneContent.AddToClassList("zero-height");
                    else
                        sceneContent.RemoveFromClassList("zero-height");
                };

                Button buttonLoadGroup = new() { text = "Load group", style = { width = 100 } };
                Button buttonUnloadGroup = new() { text = "Unload group", style = { width = 100 }};

                VisualElement buttonsContainer = new()
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        flexGrow = 1
                    }
                };
                
                buttonsContainer.Add(buttonContainer);
                buttonsContainer.Add(buttonLoadGroup);
                buttonsContainer.Add(buttonUnloadGroup);

                foreach (SceneAsset sceneAsset in sceneDataElement.SceneAssets)
                {
                    if (sceneAsset == null)
                        continue;
                    
                    VisualElement sceneElement = new()
                    {
                        style =
                        {
                            flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                            flexGrow = 1
                        }
                    };

                    Label sceneName = new() { text = sceneAsset.name, style = { width = 70 } };
                    Button loadAdditiveButton = new() { text = "Additive", style = { width = 70}};
                    Button loadSingleButton = new() { text = "Single", style = { width = 70} };
                    Button unloadButton = new() { text = "Unload", style = { width = 70} };

                    loadAdditiveButton.clicked += () => { _sceneLoader.Load(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive); };
                    loadSingleButton.clicked += () => { _sceneLoader.Load(AssetDatabase.GetAssetPath(sceneAsset)); };
                    unloadButton.clicked += () => { _sceneLoader.Unload(sceneAsset.name); };
                    
                    sceneElement.Add(sceneName);
                    sceneElement.Add(loadAdditiveButton);
                    sceneElement.Add(loadSingleButton);
                    sceneElement.Add(unloadButton);
                    sceneContent.Add(sceneElement);
                }
                
                _activeContainerView.Add(buttonsContainer);
                _activeContainerView.Add(sceneContent);
            }
            
            rootVisualElement.Add(_activeContainerView);
        }

        private void OnDestroy()
        {
            EditorSettings.PathChanged -= OnPathChanged;
        }
    }
}