using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 모든 UI 글자를 Kenney Future 폰트로 바꾸는 에디터 메뉴. TMP 폰트 에셋은 에디터에서만
// 만들 수 있어서 메뉴로 둔다. 여러 번 눌러도 괜찮다(이미 있으면 만든 걸 다시 쓴다).
public static class KenneyFontTool
{
    private const string FontPath = "Assets/UI/Kenney/Fonts/Kenney Future.ttf";
    private const string AssetPath = "Assets/UI/Kenney/Fonts/Kenney Future SDF.asset";

    [MenuItem("Tools/Overwatch/Apply Kenney Font")]
    private static void Apply()
    {
        // 열린 씬을 닫았다 열기 때문에 저장 안 한 변경이 있으면 먼저 물어본다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        TMP_FontAsset fontAsset = LoadOrCreateFontAsset();
        if (fontAsset == null)
            return;

        int prefabCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (Swap(root.GetComponentsInChildren<TMP_Text>(true), fontAsset, false) > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                prefabCount++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        string original = EditorSceneManager.GetActiveScene().path;
        int sceneCount = 0;
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                changed += Swap(root.GetComponentsInChildren<TMP_Text>(true), fontAsset, true);

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                sceneCount++;
            }
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

        Debug.Log($"[KenneyFontTool] 프리팹 {prefabCount}개, 씬 {sceneCount}개의 글꼴을 바꿨다.");
    }

    private static TMP_FontAsset LoadOrCreateFontAsset()
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (existing != null)
            return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null)
        {
            Debug.LogError($"[KenneyFontTool] {FontPath}를 찾지 못했다.");
            return null;
        }

        // 쓰는 글자만 그때그때 아틀라스에 넣는 동적 폰트. 영어만 쓰니 작게 둬도 충분하다.
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
        fontAsset.name = "Kenney Future SDF";
        AssetDatabase.CreateAsset(fontAsset, AssetPath);

        // 아틀라스 텍스처와 재질은 폰트 에셋 안에 같이 저장해야 다시 열었을 때 사라지지 않는다.
        fontAsset.atlasTexture.name = "Kenney Future Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        fontAsset.material.name = "Kenney Future Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    // 씬에 놓인 프리팹 인스턴스의 글자는 프리팹을 바꾸면 따라오므로 건드리지 않는다.
    // 건드리면 인스턴스마다 쓸데없는 덮어쓰기(override)가 생긴다.
    private static int Swap(TMP_Text[] texts, TMP_FontAsset fontAsset, bool skipPrefabInstances)
    {
        int changed = 0;
        foreach (TMP_Text text in texts)
        {
            if (text.font == fontAsset)
                continue;
            if (skipPrefabInstances && PrefabUtility.IsPartOfPrefabInstance(text))
                continue;

            Undo.RecordObject(text, "Apply Kenney Font");
            text.font = fontAsset;
            EditorUtility.SetDirty(text);
            changed++;
        }
        return changed;
    }
}
