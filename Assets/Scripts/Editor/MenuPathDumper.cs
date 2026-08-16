using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 이 Unity 버전에서 실제로 존재하는 메뉴 경로를 콘솔에 출력한다.
    /// 씬 빌더가 쓰는 UI 생성 메뉴 이름이 버전마다 달라서, 추측 대신 확인하려고 만든 진단 도구다.
    ///
    /// UnityEditor.Menu.GetMenuItems와 ScriptingMenuItem은 internal이라 리플렉션으로 접근한다.
    /// 진단용이므로 실패해도 게임에 영향이 없다.
    /// </summary>
    public static class MenuPathDumper
    {
        [MenuItem("MathRPG/Diagnostics/Dump GameObject-UI Menu Paths", priority = 90)]
        public static void DumpUiMenuPaths()
        {
            List<string> paths = GetMenuPaths("GameObject/UI");

            if (paths.Count == 0)
            {
                Debug.LogWarning("[MenuPathDumper] 'GameObject/UI' 하위 메뉴를 찾지 못했습니다.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("[MenuPathDumper] GameObject/UI 하위 메뉴 " + paths.Count + "개:");
            for (int i = 0; i < paths.Count; i++)
            {
                builder.AppendLine("  " + paths[i]);
            }

            Debug.Log(builder.ToString());
        }

        public static List<string> GetMenuPaths(string rootMenuPath)
        {
            var result = new List<string>();

            try
            {
                MethodInfo method = typeof(Menu).GetMethod(
                    "GetMenuItems",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    Debug.LogWarning("[MenuPathDumper] Menu.GetMenuItems를 찾지 못했습니다.");
                    return result;
                }

                var items = method.Invoke(null, new object[] { rootMenuPath, false, false }) as Array;
                if (items == null)
                {
                    return result;
                }

                foreach (object item in items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    Type itemType = item.GetType();

                    PropertyInfo pathProperty = itemType.GetProperty("path",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pathProperty != null)
                    {
                        result.Add((string)pathProperty.GetValue(item));
                        continue;
                    }

                    FieldInfo pathField = itemType.GetField("path",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pathField != null)
                    {
                        result.Add((string)pathField.GetValue(item));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[MenuPathDumper] 메뉴 목록을 읽지 못했습니다: " + e.Message);
            }

            return result;
        }
    }
}
