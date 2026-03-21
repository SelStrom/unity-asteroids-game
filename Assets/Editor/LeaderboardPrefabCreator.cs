using SelStrom.Asteroids;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SelStrom.AsteroidsEditor
{
    public static class LeaderboardPrefabCreator
    {
        [MenuItem("Tools/Create Leaderboard Entry Prefab")]
        public static void CreatePrefab()
        {
            var root = new GameObject("leaderboard_entry");
            var rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 30);

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            var rankGo = CreateTextChild("rank_text", root.transform, 50, "#");
            var nameGo = CreateTextChild("name_text", root.transform, 80, "AAA");
            var scoreGo = CreateTextChild("score_text", root.transform, 120, "0");

            var entryVisual = root.AddComponent<LeaderboardEntryVisual>();

            var so = new SerializedObject(entryVisual);
            so.FindProperty("_rank").objectReferenceValue = rankGo.GetComponent<TMP_Text>();
            so.FindProperty("_name").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("_score").objectReferenceValue = scoreGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();

            const string path = "Assets/Media/prefabs/gui/leaderboard_entry.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            Debug.Log($"Prefab created at: {path}");
        }

        private static GameObject CreateTextChild(string name, Transform parent, float width,
            string defaultText)
        {
            var go = new GameObject(name);
            var childRt = go.AddComponent<RectTransform>();
            childRt.SetParent(parent, false);
            childRt.sizeDelta = new Vector2(width, 30);

            go.AddComponent<CanvasRenderer>();

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;

            return go;
        }
    }
}
