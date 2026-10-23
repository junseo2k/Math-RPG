using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 열린 씬의 컴포넌트 참조를 에디터에서 채워, 씬 저장 시 Inspector에도 남게 한다
[InitializeOnLoad]
static class SceneWiring
{
    static SceneWiring()
    {
        EditorApplication.delayCall += Wire;
        EditorSceneManager.sceneOpened += (_, _) => Wire();
    }

    [MenuItem("Tools/씬 연결 실행")]
    static void Wire()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var flow = Object.FindAnyObjectByType<GameFlow>();
        if (!flow)
        {
            Debug.Log("[SceneWiring] 씬에 GameFlow가 없음. 플레이 시 자동 생성됨.");
            return;
        }

        bool changed = false;
        var pool = flow.GetComponent<StartAbilityPool>();
        if (!pool)
        {
            pool = Undo.AddComponent<StartAbilityPool>(flow.gameObject);
            changed = true;
        }
        changed |= SetRef(flow, "abilityPool", pool);

        // 역행 키 등 인스펙터 값이 저장되도록 씬에 둔다(없으면 실행 시 코드가 만들어 값이 남지 않음)
        if (!Object.FindAnyObjectByType<RewindSystem>())
        {
            Undo.AddComponent<RewindSystem>(flow.gameObject);
            changed = true;
        }
        // 플레이어 애니메이션 속도 등 인스펙터 값 저장용
        if (!Object.FindAnyObjectByType<RoomManager>())
        {
            Undo.AddComponent<RoomManager>(flow.gameObject);
            changed = true;
        }

        var ui = flow.GetComponent<FlowDebugUI>();
        if (ui) changed |= SetRef(ui, "flow", flow);

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            Debug.Log("[SceneWiring] GameFlow 연결 완료. 씬을 저장하세요.");
        }
    }

    static bool SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop.objectReferenceValue == value) return false;
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
        return true;
    }
}
