using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 실행 시 코드로 만들던 UI를 열린 씬에 한 번 만들어 저장한다. 이후 위치·크기·색·글자는 씬에서 직접 고친다.
// 이미 있는 요소는 건드리지 않고 빠진 것만 채운다. 처음부터 다시 만들려면 GameUI 오브젝트를 지우고 실행
public static class UIWiring
{
    [MenuItem("Tools/UI/씬에 UI 생성")]
    static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[UIWiring] 플레이 중에는 실행할 수 없습니다.");
            return;
        }

        var ui = Object.FindAnyObjectByType<GameUI>(FindObjectsInactive.Include);
        if (!ui)
        {
            ui = new GameObject("GameUI").AddComponent<GameUI>();
            Undo.RegisterCreatedObjectUndo(ui.gameObject, "UI 생성");
        }

        ui.Setup();
        ui.GetComponentInChildren<HUD>(true).EnsureBuilt();
        ui.GetComponentInChildren<AbilitySelectUI>(true).EnsureBuilt();
        ui.GetComponentInChildren<ResultUI>(true).EnsureBuilt();
        ui.GetComponentInChildren<RebirthOverlay>(true).EnsureBuilt();

        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Selection.activeGameObject = ui.gameObject;
        Debug.Log("[UIWiring] UI를 씬에 만들었습니다. 씬을 저장하세요.");
    }
}
