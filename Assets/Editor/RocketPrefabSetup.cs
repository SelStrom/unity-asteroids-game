using SelStrom.Asteroids;
using SelStrom.Asteroids.Configs;
using UnityEditor;
using UnityEngine;

namespace SelStrom.AsteroidsEditor
{
    public static class RocketPrefabSetup
    {
        [MenuItem("Tools/Setup Rocket Trail ParticleSystem")]
        public static void SetupRocketTrail()
        {
            const string prefabPath = "Assets/Media/prefabs/rocket.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Rocket prefab not found at: " + prefabPath);
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            // Проверить, есть ли уже дочерний Trail
            var existingTrail = prefabRoot.transform.Find("Trail");
            if (existingTrail != null)
            {
                Debug.Log("Trail already exists on rocket prefab. Updating settings...");
                var existingPs = existingTrail.GetComponent<ParticleSystem>();
                if (existingPs != null)
                {
                    ConfigureTrailParticleSystem(existingPs);
                    BindTrailToRocketVisual(prefabRoot, existingPs);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    Debug.Log("Rocket trail ParticleSystem updated successfully!");
                    return;
                }
            }

            // Создать дочерний GameObject "Trail"
            var trailGo = new GameObject("Trail");
            trailGo.transform.SetParent(prefabRoot.transform, false);
            trailGo.transform.localPosition = Vector3.zero;
            trailGo.transform.localRotation = Quaternion.identity;
            trailGo.transform.localScale = Vector3.one;

            // Добавить ParticleSystem
            var ps = trailGo.AddComponent<ParticleSystem>();
            ConfigureTrailParticleSystem(ps);

            // Привязать _trailEffect к RocketVisual
            BindTrailToRocketVisual(prefabRoot, ps);

            // Сохранить префаб
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("Rocket trail ParticleSystem created and configured successfully!");
        }

        private static void ConfigureTrailParticleSystem(ParticleSystem ps)
        {
            // Основные настройки
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.4f;
            main.startSize = 0.08f;
            main.startSpeed = 0f;
            main.startColor = new Color(1f, 1f, 1f, 0.78f);
            main.maxParticles = 50;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;

            // Эмиссия
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 40f;

            // Форма -- узкий конус
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 0.01f;

            // Размер за время жизни -- убывающая кривая
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            ));

            // Цвет за время жизни -- fade to transparent
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            // Отключить Renderer shape (используем default particle)
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        private static void BindTrailToRocketVisual(GameObject prefabRoot, ParticleSystem ps)
        {
            var rocketVisual = prefabRoot.GetComponent<RocketVisual>();
            if (rocketVisual == null)
            {
                Debug.LogError("RocketVisual component not found on rocket prefab root!");
                return;
            }

            var so = new SerializedObject(rocketVisual);
            var trailProp = so.FindProperty("_trailEffect");
            if (trailProp != null)
            {
                trailProp.objectReferenceValue = ps;
                so.ApplyModifiedProperties();
                Debug.Log("_trailEffect bound to Trail ParticleSystem");
            }
            else
            {
                Debug.LogError("_trailEffect property not found on RocketVisual!");
            }
        }

        [MenuItem("Tools/Setup Rocket Config Values")]
        public static void SetupRocketConfigValues()
        {
            const string gameDataPath = "Assets/Media/configs/GameData.asset";
            var gameData = AssetDatabase.LoadAssetAtPath<GameData>(gameDataPath);
            if (gameData == null)
            {
                Debug.LogError("GameData asset not found at: " + gameDataPath);
                return;
            }

            var so = new SerializedObject(gameData);
            var rocketProp = so.FindProperty("Rocket");

            if (rocketProp == null)
            {
                Debug.LogError("Rocket property not found on GameData!");
                return;
            }

            // Выставить значения из ранее hardcoded констант
            rocketProp.FindPropertyRelative("Speed").floatValue = 8f;
            rocketProp.FindPropertyRelative("LifeTimeSec").floatValue = 5f;
            rocketProp.FindPropertyRelative("TurnRateDegPerSec").floatValue = 180f;
            rocketProp.FindPropertyRelative("MaxAmmo").intValue = 3;
            rocketProp.FindPropertyRelative("ReloadDurationSec").floatValue = 5f;
            rocketProp.FindPropertyRelative("Score").intValue = 50;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameData);
            AssetDatabase.SaveAssets();

            Debug.Log("Rocket config values set: Speed=8, LifeTimeSec=5, TurnRateDegPerSec=180, MaxAmmo=3, ReloadDurationSec=5, Score=50");
        }

        [MenuItem("Tools/Setup Rocket (All)")]
        public static void SetupRocketAll()
        {
            SetupRocketTrail();
            SetupRocketConfigValues();
            Debug.Log("=== Rocket setup complete! ===");
        }
    }
}
