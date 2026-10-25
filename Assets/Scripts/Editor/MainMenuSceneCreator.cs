using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 메인 메뉴 씬을 만들어 저장하고 빌드 목록 맨 앞(첫 실행 씬)에 넣는다. 씬이 이미 있으면 빌드 목록만 맞춘다
public static class MainMenuSceneCreator
{
    const string ScenePath = "Assets/Scenes/" + MainMenuUI.SceneName + ".unity";

    [MenuItem("Tools/Scenes/메인 메뉴 씬 생성")]
    static void Create()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[MainMenuSceneCreator] 플레이 중에는 실행할 수 없습니다.");
            return;
        }

        if (!File.Exists(ScenePath))
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("MainMenu").AddComponent<MainMenuUI>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Where(s => s.path != ScenePath)
            .Prepend(new EditorBuildSettingsScene(ScenePath, true))
            .ToArray();
        Debug.Log($"[MainMenuSceneCreator] {ScenePath}를 빌드 목록 0번에 넣었습니다.");
    }
}
