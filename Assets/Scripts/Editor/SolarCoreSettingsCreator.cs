using UnityEditor;
using UnityEngine;

// 태양핵 설정 에셋이 없으면 에디터 시작 시 자동 생성. 메뉴로 바로 선택
[InitializeOnLoad]
static class SolarCoreSettingsCreator
{
    const string AssetPath = "Assets/Resources/" + SolarCoreSettings.ResourcePath + ".asset";

    static SolarCoreSettingsCreator() => EditorApplication.delayCall += () => Ensure();

    [MenuItem("Tools/태양핵 설정 열기")]
    static void Open()
    {
        var settings = Ensure();
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    static SolarCoreSettings Ensure()
    {
        var settings = AssetDatabase.LoadAssetAtPath<SolarCoreSettings>(AssetPath);
        if (settings) return settings;
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        settings = ScriptableObject.CreateInstance<SolarCoreSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"태양핵 설정 에셋 생성: {AssetPath}");
        return settings;
    }
}
