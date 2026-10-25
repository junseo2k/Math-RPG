using System;
using System.Linq;
using UnityEngine.SceneManagement;

// 자동 생성(RuntimeInitializeOnLoadMethod)은 첫 씬에서 한 번만 불린다.
// 게임 씬이 열릴 때마다(처음·다시 불러오기 모두) 대신 실행한다
public static class GameScene
{
    // GameFlow가 저장된 씬만 게임 씬. 메뉴·크레딧 등 다른 씬에는 게임 오브젝트를 만들지 않는다
    public static bool IsGameScene(Scene scene) =>
        scene.GetRootGameObjects().Any(go => go.GetComponentInChildren<GameFlow>(true));

    public static void WhenLoaded(Action bootstrap)
    {
        if (IsGameScene(SceneManager.GetActiveScene())) bootstrap();
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (IsGameScene(scene)) bootstrap();
        };
    }
}
