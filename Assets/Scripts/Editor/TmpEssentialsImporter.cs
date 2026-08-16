using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// TextMeshPro Essential Resources를 대화 상자 없이 임포트한다.
    ///
    /// Unity 기본 메뉴(Window/TextMeshPro/Import TMP Essential Resources)는
    /// 사람이 버튼을 눌러야 하는 창을 띄우기 때문에 자동화가 안 된다.
    /// 이 도구는 패키지 캐시에서 .unitypackage를 직접 찾아 비대화형으로 임포트한다.
    /// </summary>
    public static class TmpEssentialsImporter
    {
        private const string EssentialsPackageName = "TMP Essential Resources.unitypackage";
        private const string InstalledMarkerPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("MathRPG/Setup/Import TMP Essential Resources", priority = 10)]
        public static void ImportEssentials()
        {
            if (File.Exists(InstalledMarkerPath))
            {
                Debug.Log("[TmpEssentialsImporter] 이미 임포트되어 있습니다: " + InstalledMarkerPath);
                return;
            }

            string packagePath = FindEssentialsPackage();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("[TmpEssentialsImporter] '" + EssentialsPackageName + "'를 패키지 캐시에서 찾지 못했습니다. " +
                               "com.unity.ugui 패키지가 설치되어 있는지 확인하세요.");
                return;
            }

            Debug.Log("[TmpEssentialsImporter] 임포트 시작: " + packagePath);
            AssetDatabase.ImportPackage(packagePath, interactive: false);
        }

        /// <summary>패키지 캐시와 내장 패키지 폴더에서 TMP Essentials 패키지를 찾는다.</summary>
        private static string FindEssentialsPackage()
        {
            string[] searchRoots =
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Library", "PackageCache"),
                Path.Combine(EditorApplication.applicationContentsPath, "Resources", "PackageManager", "BuiltInPackages")
            };

            foreach (string root in searchRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                string[] matches = Directory.GetFiles(root, EssentialsPackageName, SearchOption.AllDirectories);
                if (matches.Length > 0)
                {
                    return matches[0];
                }
            }

            return null;
        }
    }
}
