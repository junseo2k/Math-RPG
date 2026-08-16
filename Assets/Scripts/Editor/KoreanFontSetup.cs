using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MathRPG.EditorTools
{
    /// <summary>
    /// 한글 TMP 폰트 에셋을 만들고 TMP 기본 폰트로 지정한다.
    ///
    /// TMP 기본 폰트(LiberationSans)에는 한글 글리프가 없어서 그대로 두면
    /// 모든 한국어 UI가 빈 네모로 보인다. 여기서 만드는 폰트는 동적(Dynamic) 아틀라스라
    /// 실제로 쓰인 글자만 실행 중에 아틀라스로 구워진다 — 한글 전체를 미리 굽지 않아도 된다.
    ///
    /// 폰트는 Noto Sans KR(SIL Open Font License)이라 저장소 포함·재배포에 제약이 없다.
    /// </summary>
    public static class KoreanFontSetup
    {
        private const string SourceFontPath = "Assets/Art/Fonts/NotoSansKR-VF.ttf";
        private const string FontAssetPath = "Assets/Art/Fonts/NotoSansKR SDF.asset";

        private const int SamplingPointSize = 60;
        private const int AtlasPadding = 6;
        private const int AtlasWidth = 1024;
        private const int AtlasHeight = 1024;

        [MenuItem("MathRPG/Setup/Create Korean TMP Font", priority = 11)]
        public static void CreateKoreanFontAsset()
        {
            TMP_FontAsset fontAsset = GetOrCreateFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            AssignAsTmpDefault(fontAsset);
        }

        public static TMP_FontAsset GetOrCreateFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            if (!File.Exists(SourceFontPath))
            {
                Debug.LogError("[KoreanFontSetup] 원본 폰트를 찾지 못했습니다: " + SourceFontPath);
                return null;
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[KoreanFontSetup] 폰트를 Font 에셋으로 읽지 못했습니다: " + SourceFontPath);
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasWidth,
                AtlasHeight,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError("[KoreanFontSetup] TMP 폰트 에셋 생성에 실패했습니다.");
                return null;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(FontAssetPath);

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            // 아틀라스 텍스처와 머티리얼은 폰트 에셋의 하위 에셋으로 함께 저장해야 한다.
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlas = fontAsset.atlasTextures[i];
                    if (atlas != null)
                    {
                        atlas.name = fontAsset.name + " Atlas " + i;
                        AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KoreanFontSetup] 한글 폰트 에셋 생성 완료: " + FontAssetPath);
            return fontAsset;
        }

        /// <summary>
        /// TMP Settings의 기본 폰트를 교체한다.
        /// 이렇게 해두면 이후 새로 만드는 모든 TMP 텍스트가 자동으로 한글 폰트를 쓴다.
        /// </summary>
        private static void AssignAsTmpDefault(TMP_FontAsset fontAsset)
        {
            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                Debug.LogWarning("[KoreanFontSetup] TMP Settings를 찾지 못했습니다. " +
                                 "TMP Essential Resources를 먼저 임포트하세요.");
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty defaultFont = serialized.FindProperty("m_defaultFontAsset");

            if (defaultFont == null)
            {
                Debug.LogWarning("[KoreanFontSetup] TMP Settings에서 m_defaultFontAsset 필드를 찾지 못했습니다.");
                return;
            }

            defaultFont.objectReferenceValue = fontAsset;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log("[KoreanFontSetup] TMP 기본 폰트를 " + fontAsset.name + "으로 지정했습니다.");
        }
    }
}
