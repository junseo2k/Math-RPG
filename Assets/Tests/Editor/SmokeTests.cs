using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SmokeTests
{
    [Test]
    public void CombatBalanceAssetLoads()
    {
        Assert.IsNotNull(Resources.Load<CombatBalance>(CombatBalance.ResourcePath),
            $"Resources/{CombatBalance.ResourcePath}.asset 없음");
    }

    [Test]
    public void BuildScenesOpenWithoutMissingScripts()
    {
        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled) continue;

            // Additive so an editor-open run never discards the user's unsaved scene.
            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
            try
            {
                Assert.IsTrue(scene.IsValid(), $"{entry.path} 열기 실패");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),
                            $"{entry.path}: {t.name}에 빠진 스크립트");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
