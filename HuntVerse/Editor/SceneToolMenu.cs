using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;
using Debug = UnityEngine.Debug;


namespace Hades.Tool
{
    [InitializeOnLoad]
    public class SceneToolBar
    {
        public static GUIStyle commandButtonStyle;
        static void CreateStyle()
        {
            commandButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(251 / 255f, 200 / 255f, 255 / 255f, 1.0f) },

            };

            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(126 / 255f, 160 / 255f, 185 / 255f, 0.7f));
            bgTex.Apply();
            commandButtonStyle.normal.background = bgTex;

            commandButtonStyle.padding = new RectOffset(10, 10, 2, 2);
        }
        static SceneToolBar()
        {
            ToolbarExtender.LeftToolbarGUI.Add(BuildToolbarLeft);
            ToolbarExtender.RightToolbarGUI.Add(BuildToolbarRight);
        }

        private static void BuildToolbarLeft()
        {
            if (commandButtonStyle == null) CreateStyle();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Run Game", "Start Run Game"), commandButtonStyle))
            {
                SceneHelper.StartScene("Boot");
            }
          
        }

        private static void BuildToolbarRight()
        {
            if (commandButtonStyle == null) CreateStyle();
            if (GUILayout.Button(new GUIContent("Boot", "open Boot scene"), commandButtonStyle))
            {
                SceneHelper.OpenScene("Boot");
            }
            if (GUILayout.Button(new GUIContent("Menu", "open Menu scene"), commandButtonStyle))
            {
                SceneHelper.OpenScene("MainMenu");
            }
            if (GUILayout.Button(new GUIContent("Village", "open Village scene"), commandButtonStyle))
            {
                SceneHelper.OpenScene("Village");
            }
            GUILayout.FlexibleSpace();
        }
    }

    static class SceneHelper
    {
        static string sceneToOpen;

        public static void StartScene(string sceneName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            sceneToOpen = sceneName;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (sceneToOpen == null ||
                EditorApplication.isPlaying || EditorApplication.isPaused ||
                EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.update -= OnUpdate;

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // need to get scene via search because the path to the scene
                // file contains the package version so it'll change over time
                string[] guids = AssetDatabase.FindAssets("t:scene " + sceneToOpen, null);
                if (guids.Length == 0)
                {
                    Debug.LogWarning("Couldn't find scene file");
                }
                else
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    EditorSceneManager.OpenScene(scenePath);
                    EditorApplication.isPlaying = true;
                }
            }
            sceneToOpen = null;
        }

        public static void OpenScene(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets("t:scene " + sceneName, null);
            if (guids.Length == 0)
            {
                Debug.LogWarning("Couldn't find scene file");
            }
            else
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}