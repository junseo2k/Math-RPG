using UnityEditor;
using UnityEngine;

// 전투 밸런스 에셋이 없으면 에디터 시작 시 자동 생성. 메뉴로 바로 선택
[InitializeOnLoad]
static class CombatBalanceCreator
{
    const string AssetPath = "Assets/Resources/" + CombatBalance.ResourcePath + ".asset";

    static CombatBalanceCreator() => EditorApplication.delayCall += () => Ensure();

    [MenuItem("Tools/전투 밸런스 열기")]
    static void Open()
    {
        var balance = Ensure();
        Selection.activeObject = balance;
        EditorGUIUtility.PingObject(balance);
    }

    static CombatBalance Ensure()
    {
        var balance = AssetDatabase.LoadAssetAtPath<CombatBalance>(AssetPath);
        if (balance) return balance;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        balance = ScriptableObject.CreateInstance<CombatBalance>();
        AssetDatabase.CreateAsset(balance, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"전투 밸런스 에셋 생성: {AssetPath}");
        return balance;
    }
}
