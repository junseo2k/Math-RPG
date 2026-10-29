using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ReferenceTests
{
    // verify.ps1 prints this file's path; empty fields are reported, not failed,
    // because many fields are filled in Awake when left empty.
    const string EmptyReportPath = "TestResults-EditMode-EmptyRefs.txt";

    [Test]
    public void BuildScenesHaveNoMissingReferences()
    {
        var missing = new List<string>();
        var empty = new List<string>();

        foreach (var entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled) continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                        if (component) Collect(component, $"{entry.path}: {PathOf(component.transform)}", missing, empty);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        File.WriteAllLines(EmptyReportPath, empty);
        Assert.IsEmpty(missing, "Missing 참조:\n" + string.Join("\n", missing));
    }

    static void Collect(Component component, string where, List<string> missing, List<string> empty)
    {
        // Only our own scripts: package components (TextMeshPro etc.) leave many fields empty by design.
        var userScript = component is MonoBehaviour mb &&
                         AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(mb)).StartsWith("Assets/");
        var prop = new SerializedObject(component).GetIterator();
        var enterChildren = true;
        while (prop.Next(enterChildren))
        {
            enterChildren = prop.propertyType == SerializedPropertyType.Generic;
            if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.objectReferenceValue) continue;

            var label = $"{where} [{component.GetType().Name}.{prop.propertyPath}]";
            if (prop.objectReferenceInstanceIDValue != 0) missing.Add(label);
            else if (userScript && !prop.propertyPath.StartsWith("m_")) empty.Add(label);
        }
    }

    static string PathOf(Transform t) => t.parent ? $"{PathOf(t.parent)}/{t.name}" : t.name;
}
