using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using KindFall.Player.Dragon;
using KindFall.World.Obstacles;
using KindFall.World.Puzzles;

namespace KindFall.Editor
{
    /// <summary>
    /// BabyDragon sprite sheet'ini böler, Animator Controller + animasyon kliplerini üretir
    /// ve 2D URP Point Light dahil modüler BabyDragon.prefab & DragonFireball.prefab dosyalarını oluşturur.
    /// </summary>
    [InitializeOnLoad]
    public static class DragonSetupBuilder
    {
        private const string DragonSheetPath = "Assets/Art/Characters/Dragon/BabyDragon_Sheet.png";
        private const string FireballSheetPath = "Assets/Art/Characters/Dragon/DragonFireball_Sheet.png";
        private const string VineSpritePath = "Assets/Art/World/DragonObstacles/BurnableVine.png";
        private const string VineBurnedSpritePath = "Assets/Art/World/DragonObstacles/BurnableVine_Burned.png";
        private const string IceSheetPath = "Assets/Art/World/DragonObstacles/BurnableIce_Sheet.png";
        private const string CrystalSheetPath = "Assets/Art/World/DragonObstacles/FireCrystal_Sheet.png";
        private const string BarrierSheetPath = "Assets/Art/World/DragonObstacles/EnergyBarrier_Sheet.png";
        private const string BurnFireSheetPath = "Assets/Art/World/DragonObstacles/BurnFire_Sheet.png";
        private const string AnimDir = "Assets/Art/Characters/Dragon/Animations";
        private const string ControllerPath = "Assets/Art/Characters/Dragon/BabyDragon_Animator.controller";
        private const string FireballControllerPath = "Assets/Art/Characters/Dragon/DragonFireball_Animator.controller";
        private const string PrefabDir = "Assets/Prefabs/Characters";
        private const string WorldPrefabDir = "Assets/Prefabs/World";
        private const string DragonPrefabPath = "Assets/Prefabs/Characters/BabyDragon.prefab";
        private const string FireballPrefabPath = "Assets/Prefabs/Characters/DragonFireball.prefab";
        private const string VinePrefabPath = "Assets/Prefabs/World/BurnableVine.prefab";
        private const string IcePrefabPath = "Assets/Prefabs/World/BurnableIce.prefab";
        private const string CrystalPrefabPath = "Assets/Prefabs/World/FireTriggerCrystal.prefab";
        private const string PreviewScenePath = "Assets/Scenes/DragonPreviewScene.unity";

        static DragonSetupBuilder()
        {
            EditorApplication.delayCall += EnsureDragonSetupOnce;
        }

        private static void EnsureDragonSetupOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(DragonSheetPath)) return;

            if (!File.Exists(DragonPrefabPath) || !File.Exists(VinePrefabPath) || !File.Exists(IcePrefabPath) || !File.Exists(CrystalPrefabPath))
            {
                BuildAllDragonAssets();
            }
        }

        [MenuItem("KindFall/Open Dragon Preview Scene")]
        public static void OpenPreviewSceneMenu()
        {
            if (!File.Exists(DragonPrefabPath) || !File.Exists(VinePrefabPath) || !File.Exists(IcePrefabPath) || !File.Exists(CrystalPrefabPath))
            {
                BuildAllDragonAssets();
            }
            CreateAndOpenPreviewScene(promptToSave: true);
        }

        private static void CreateAndOpenPreviewScene(bool promptToSave)
        {
            Scene active = SceneManager.GetActiveScene();
            if (!promptToSave && active.isDirty) return;
            if (promptToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene previewScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera (Yakın 2D kamera - piksel art net görünsün)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0.5f, 0.55f, -10f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.14f, 1f);
            camObj.AddComponent<AudioListener>();

            // 2. Global Light 2D (2D URP'de gerçek renklerin görünmesi için ortam ışığı)
            GameObject globalLightObj = new GameObject("Global Light 2D");
            Light2D globalLight = globalLightObj.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.85f;
            globalLight.color = Color.white;

            GameObject vinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VinePrefabPath);

            // 3. Zemin (Ground Layer - Sarmaşık ve küllerin yere dökülüşünü görmek için)
            CreatePreviewGround(vinePrefab);

            // 4. BabyDragon Prefab örneği
            GameObject dragonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DragonPrefabPath);
            if (dragonPrefab != null)
            {
                GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(dragonPrefab);
                d.transform.position = new Vector3(-1.4f, 0.25f, 0f);
            }

            // 5. BurnableVine Prefab örneği (Tam Ground_Floor üst yüzeyine y=-0.75 oturur)
            if (vinePrefab != null)
            {
                GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(vinePrefab);
                v.transform.position = new Vector3(1.5f, 0.0f, 0f);
            }

            // 6. BurnableIce Prefab örneği (Buz bariyeri erime testi için)
            GameObject icePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IcePrefabPath);
            if (icePrefab != null)
            {
                GameObject ice = (GameObject)PrefabUtility.InstantiatePrefab(icePrefab);
                ice.transform.position = new Vector3(3.1f, 0.0f, 0f);
            }

            // 7. FireTriggerCrystal Prefab örneği (Orta-üst yükseklikte süzülür, altından Prenses rahat geçer)
            GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
            if (crystalPrefab != null)
            {
                GameObject crys = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab);
                crys.transform.position = new Vector3(0.4f, 2.15f, 0f);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath)!);
            EditorSceneManager.SaveScene(previewScene, PreviewScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[KindFall] Ejderha önizleme sahnesi açıldı: {PreviewScenePath}");
        }

        private static void CreatePreviewGround(GameObject vinePrefab)
        {
            const string groundTexPath = "Assets/Art/World/DragonObstacles/PreviewGround.png";
            if (!File.Exists(groundTexPath))
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(groundTexPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(groundTexPath, ImportAssetOptions.ForceUpdate);

                TextureImporter imp = AssetImporter.GetAtPath(groundTexPath) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spritePixelsPerUnit = 4f;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
            }

            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(groundTexPath);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) groundLayer = 0;

            GameObject ground = new GameObject("Ground_Floor");
            ground.layer = groundLayer;
            ground.transform.position = new Vector3(0f, -1.25f, 0f);
            ground.transform.localScale = new Vector3(16f, 1f, 1f);

            SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = groundSprite;
            sr.color = new Color(0.18f, 0.22f, 0.20f, 1f);
            ground.AddComponent<BoxCollider2D>();
        }

        [MenuItem("KindFall/Setup BabyDragon Prefab & Animator")]
        public static void BuildAllDragonAssets()
        {
            Directory.CreateDirectory(AnimDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(WorldPrefabDir);

            // 1. Sprite sheet'leri Point, Uncompressed olarak dilimle (Ejderha 96x96 @ 96 PPU, Ateş topu 32x32 @ 64 PPU)
            Sprite[] dragonSprites = SliceSheet(DragonSheetPath, 96, 96, 4, "BabyDragon", 96f);
            Sprite[] fireballSprites = SliceSheet(FireballSheetPath, 32, 32, 3, "DragonFireball", 64f);

            if (dragonSprites.Length < 4)
            {
                Debug.LogError("[DragonSetupBuilder] BabyDragon_Sheet.png 4 kare olarak dilimlenemedi.");
                return;
            }

            // 2. Animasyon kliplerini oluştur
            AnimationClip idleClip = CreateSpriteClip($"{AnimDir}/Dragon_Idle.anim",
                new[] { dragonSprites[0], dragonSprites[1] }, 4f, loop: true);

            AnimationClip glideClip = CreateSpriteClip($"{AnimDir}/Dragon_Glide.anim",
                new[] { dragonSprites[0], dragonSprites[1] }, 6f, loop: true);

            AnimationClip flyClip = CreateSpriteClip($"{AnimDir}/Dragon_Fly.anim",
                new[] { dragonSprites[0], dragonSprites[1] }, 10f, loop: true);

            AnimationClip attackClip = CreateSpriteClip($"{AnimDir}/Dragon_Attack.anim",
                new[] { dragonSprites[2], dragonSprites[0] }, 8f, loop: false);

            AnimationClip carryClip = CreateSpriteClip($"{AnimDir}/Dragon_Carry.anim",
                new[] { dragonSprites[3] }, 4f, loop: true);

            AnimationClip fireballClip = fireballSprites.Length >= 3
                ? CreateSpriteClip($"{AnimDir}/Dragon_Fireball.anim",
                    new[] { fireballSprites[0], fireballSprites[1], fireballSprites[2] }, 12f, loop: true)
                : null;

            // 3. Animator Controller (DragonState bool parametreleriyle birebir eşleşir)
            AnimatorController controller = BuildAnimatorController(idleClip, glideClip, flyClip, attackClip, carryClip);
            AnimatorController fireballController = fireballClip != null ? BuildSingleClipController(FireballControllerPath, "Fly", fireballClip) : null;

            // 4. Modüler DragonFireball Prefab
            DragonFireball fireballPrefab = CreateFireballPrefab(fireballSprites.Length > 0 ? fireballSprites[0] : null, fireballController);

            // 5. Modüler BabyDragon Prefab (2D URP Point Light dahil)
            CreateDragonPrefab(dragonSprites[0], controller, fireballPrefab);

            // 6. Modüler BurnableVine, BurnableIce & FireTriggerCrystal Prefab'ları
            CreateBurnableVinePrefab();
            CreateBurnableIcePrefab();
            CreateFireCrystalPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KindFall] BabyDragon Prefab, Animator ve 2D URP Point Light kurulumu tamamlandı: Assets/Prefabs/Characters/BabyDragon.prefab");
        }

        private static Sprite[] SliceSheet(string assetPath, int cellWidth, int cellHeight, int count, string baseName, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return System.Array.Empty<Sprite>();

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            List<SpriteRect> rects = new List<SpriteRect>(count);
            for (int i = 0; i < count; i++)
            {
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{i}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(i * cellWidth, 0, cellWidth, cellHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }

            dataProvider.SetSpriteRects(rects.ToArray());
            dataProvider.Apply();
            importer.SaveAndReimport();

            Object[] loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object obj in loaded)
            {
                if (obj is Sprite sp)
                {
                    sprites.Add(sp);
                }
            }

            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites.ToArray();
        }

        private static AnimationClip CreateSpriteClip(string clipPath, Sprite[] frames, float frameRate, bool loop)
        {
            AnimationClip clip = new AnimationClip
            {
                frameRate = frameRate
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (File.Exists(clipPath))
            {
                AssetDatabase.DeleteAsset(clipPath);
            }

            AssetDatabase.CreateAsset(clip, clipPath);
            return clip;
        }

        private static AnimatorController BuildAnimatorController(
            AnimationClip idleClip,
            AnimationClip glideClip,
            AnimationClip flyClip,
            AnimationClip attackClip,
            AnimationClip carryClip)
        {
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Idle", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Glide", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Fly", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsCarrying", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

            AnimatorState idleState = rootSM.AddState("Idle");
            idleState.motion = idleClip;
            rootSM.defaultState = idleState;

            AnimatorState glideState = rootSM.AddState("Glide");
            glideState.motion = glideClip;

            AnimatorState flyState = rootSM.AddState("Fly");
            flyState.motion = flyClip;

            AnimatorState attackState = rootSM.AddState("Attack");
            attackState.motion = attackClip;

            AnimatorState carryState = rootSM.AddState("Carry");
            carryState.motion = carryClip;

            AddAnyStateTransition(rootSM, idleState, "Idle", requireNotCarrying: true);
            AddAnyStateTransition(rootSM, glideState, "Glide", requireNotCarrying: true);
            AddAnyStateTransition(rootSM, flyState, "Fly", requireNotCarrying: true);
            AddAnyStateTransition(rootSM, attackState, "Attack", requireNotCarrying: false);

            AnimatorStateTransition carryT = rootSM.AddAnyStateTransition(carryState);
            carryT.hasExitTime = false;
            carryT.duration = 0f;
            carryT.canTransitionToSelf = false;
            carryT.AddCondition(AnimatorConditionMode.If, 0f, "IsCarrying");
            carryT.AddCondition(AnimatorConditionMode.IfNot, 0f, "Attack");

            return controller;
        }

        private static void AddAnyStateTransition(AnimatorStateMachine sm, AnimatorState target, string boolParam, bool requireNotCarrying)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(target);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, boolParam);
            if (requireNotCarrying)
            {
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCarrying");
            }
        }

        private static AnimatorController BuildSingleClipController(string path, string stateName, AnimationClip clip)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorState st = ctrl.layers[0].stateMachine.AddState(stateName);
            st.motion = clip;
            ctrl.layers[0].stateMachine.defaultState = st;
            return ctrl;
        }

        private static DragonFireball CreateFireballPrefab(Sprite sprite, AnimatorController fireballController)
        {
            GameObject temp = new GameObject("DragonFireball");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;

            if (fireballController != null)
            {
                Animator fbAnim = temp.AddComponent<Animator>();
                fbAnim.runtimeAnimatorController = fireballController;
            }

            GameObject fbLightObj = new GameObject("FireballGlow");
            fbLightObj.transform.SetParent(temp.transform, false);
            Light2D fbLight = fbLightObj.AddComponent<Light2D>();
            fbLight.lightType = Light2D.LightType.Point;
            fbLight.color = new Color(1f, 0.58f, 0.16f, 1f);
            fbLight.intensity = 0.7f;
            fbLight.pointLightInnerRadius = 0.25f;
            fbLight.pointLightOuterRadius = 1.6f;

            CircleCollider2D col = temp.AddComponent<CircleCollider2D>();
            col.radius = 0.18f;
            col.isTrigger = true;

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            DragonFireball fireball = temp.AddComponent<DragonFireball>();
            SerializedObject fbSO = new SerializedObject(fireball);
            fbSO.FindProperty("hitLayer").intValue = LayerMask.GetMask("Default", "Ground", "Wall");
            fbSO.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, FireballPrefabPath);
            Object.DestroyImmediate(temp);
            return saved.GetComponent<DragonFireball>();
        }

        private static void CreateDragonPrefab(
            Sprite defaultSprite,
            AnimatorController controller,
            DragonFireball fireballPrefab)
        {
            GameObject dragonObj = new GameObject("BabyDragon");

            // Görsel + Animator child objesi
            GameObject visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(dragonObj.transform, false);
            SpriteRenderer sr = visualObj.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingOrder = 6;

            Animator anim = visualObj.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // 2D URP Point Light (Ejderha pulları karanlıkta yumuşak sıcak ışık yaysın)
            GameObject lightObj = new GameObject("ScaleGlowLight");
            lightObj.transform.SetParent(dragonObj.transform, false);
            Light2D pointLight = lightObj.AddComponent<Light2D>();
            pointLight.lightType = Light2D.LightType.Point;
            pointLight.color = new Color(1f, 0.78f, 0.45f, 1f);
            pointLight.intensity = 0.65f;
            pointLight.pointLightInnerRadius = 0.6f;
            pointLight.pointLightOuterRadius = 3.5f;

            // Fizik ve Collider
            BoxCollider2D col = dragonObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.85f);

            Rigidbody2D rb = dragonObj.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.gravityScale = 0f;

            // Çıkış ve Taşıma Noktaları
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(dragonObj.transform, false);
            firePoint.transform.localPosition = new Vector3(0.48f, -0.02f, 0f);

            GameObject carryAnchor = new GameObject("CarryAnchor");
            carryAnchor.transform.SetParent(dragonObj.transform, false);
            carryAnchor.transform.localPosition = new Vector3(0f, -0.95f, 0f);

            // DragonController Bağlantıları
            DragonController dragonCtrl = dragonObj.AddComponent<DragonController>();
            SerializedObject dSO = new SerializedObject(dragonCtrl);
            dSO.FindProperty("rb").objectReferenceValue = rb;
            dSO.FindProperty("mainCollider").objectReferenceValue = col;
            dSO.FindProperty("anim").objectReferenceValue = anim;
            dSO.FindProperty("spriteRenderer").objectReferenceValue = sr;
            dSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            dSO.FindProperty("carryAnchor").objectReferenceValue = carryAnchor.transform;
            dSO.FindProperty("fireballPrefab").objectReferenceValue = fireballPrefab;
            dSO.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
            dSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(dragonObj, DragonPrefabPath);
            Object.DestroyImmediate(dragonObj);
        }

        private static void CreateBurnableVinePrefab()
        {
            ConfigureSingleSprite(VineSpritePath, 64f);
            ConfigureSingleSprite(VineBurnedSpritePath, 64f);

            Sprite vineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VineSpritePath);
            Sprite vineBurnedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(VineBurnedSpritePath);
            Sprite[] flameSprites = SliceSheet(BurnFireSheetPath, 32, 32, 3, "BurnFire", 64f);

            GameObject vineObj = new GameObject("BurnableVine");
            SpriteRenderer sr = vineObj.AddComponent<SpriteRenderer>();
            sr.sprite = vineSprite;
            sr.sortingOrder = 4;

            BoxCollider2D col = vineObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 1.45f);

            BurnableObstacle obs = vineObj.AddComponent<BurnableObstacle>();
            SerializedObject obsSO = new SerializedObject(obs);
            obsSO.FindProperty("obstacleType").enumValueIndex = (int)BurnableObstacle.ObstacleType.Vine;
            obsSO.FindProperty("spriteRenderer").objectReferenceValue = sr;
            obsSO.FindProperty("burnedSprite").objectReferenceValue = vineBurnedSprite;

            SerializedProperty flamesProp = obsSO.FindProperty("flameSprites");
            flamesProp.arraySize = flameSprites.Length;
            for (int i = 0; i < flameSprites.Length; i++)
            {
                flamesProp.GetArrayElementAtIndex(i).objectReferenceValue = flameSprites[i];
            }

            obsSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(vineObj, VinePrefabPath);
            Object.DestroyImmediate(vineObj);
        }

        private static void CreateBurnableIcePrefab()
        {
            Sprite[] iceFrames = SliceSheet(IceSheetPath, 64, 96, 5, "BurnableIce", 64f);
            Sprite initialIce = iceFrames.Length > 0 ? iceFrames[0] : null;

            GameObject iceObj = new GameObject("BurnableIce");
            SpriteRenderer sr = iceObj.AddComponent<SpriteRenderer>();
            sr.sprite = initialIce;
            sr.sortingOrder = 4;

            BoxCollider2D col = iceObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 1.4f);

            BurnableObstacle obs = iceObj.AddComponent<BurnableObstacle>();
            SerializedObject obsSO = new SerializedObject(obs);
            obsSO.FindProperty("obstacleType").enumValueIndex = (int)BurnableObstacle.ObstacleType.Ice;
            obsSO.FindProperty("burnDuration").floatValue = 0.95f;
            obsSO.FindProperty("destroyAfterBurn").boolValue = false;
            obsSO.FindProperty("spriteRenderer").objectReferenceValue = sr;

            SerializedProperty meltProp = obsSO.FindProperty("meltFrames");
            meltProp.arraySize = iceFrames.Length;
            for (int i = 0; i < iceFrames.Length; i++)
            {
                meltProp.GetArrayElementAtIndex(i).objectReferenceValue = iceFrames[i];
            }

            obsSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(iceObj, IcePrefabPath);
            Object.DestroyImmediate(iceObj);
        }

        private static void CreateFireCrystalPrefab()
        {
            Sprite[] crystalFrames = SliceSheet(CrystalSheetPath, 64, 64, 2, "FireCrystal", 64f);
            Sprite[] barrierFrames = SliceSheet(BarrierSheetPath, 32, 192, 3, "EnergyBarrier", 64f);
            Sprite unlit = crystalFrames.Length > 0 ? crystalFrames[0] : null;
            Sprite lit = crystalFrames.Length > 1 ? crystalFrames[1] : unlit;
            Sprite barrierInitial = barrierFrames.Length > 0 ? barrierFrames[0] : EnsureSimpleSquareSprite();

            GameObject crystalObj = new GameObject("FireTriggerCrystal");
            SpriteRenderer sr = crystalObj.AddComponent<SpriteRenderer>();
            sr.sprite = unlit;
            sr.sortingOrder = 5;

            CircleCollider2D col = crystalObj.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            col.isTrigger = false;

            GameObject lightObj = new GameObject("CrystalGlowLight");
            lightObj.transform.SetParent(crystalObj.transform, false);
            Light2D glowLight = lightObj.AddComponent<Light2D>();
            glowLight.lightType = Light2D.LightType.Point;
            glowLight.color = new Color(0.45f, 0.72f, 1f, 1f);
            glowLight.intensity = 0.15f;
            glowLight.pointLightInnerRadius = 0.35f;
            glowLight.pointLightOuterRadius = 2.8f;

            // Üst ve alt büyülü enerji bariyerleri (Ejderha üstten/alttan geçemez, Prenses altından geçebilir)
            GameObject upperBarrier = new GameObject("UpperEnergyBarrier");
            upperBarrier.transform.SetParent(crystalObj.transform, false);
            upperBarrier.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            SpriteRenderer upperSr = upperBarrier.AddComponent<SpriteRenderer>();
            upperSr.sprite = barrierInitial;
            upperSr.sortingOrder = 4;
            BoxCollider2D upperCol = upperBarrier.AddComponent<BoxCollider2D>();
            upperCol.size = new Vector2(0.32f, 3.0f);

            GameObject lowerBarrier = new GameObject("LowerEnergyBarrier");
            lowerBarrier.transform.SetParent(crystalObj.transform, false);
            lowerBarrier.transform.localPosition = new Vector3(0f, -2.25f, 0f);
            lowerBarrier.transform.localScale = new Vector3(1f, 1.25f, 1f);
            SpriteRenderer lowerSr = lowerBarrier.AddComponent<SpriteRenderer>();
            lowerSr.sprite = barrierInitial;
            lowerSr.sortingOrder = 4;
            BoxCollider2D lowerCol = lowerBarrier.AddComponent<BoxCollider2D>();
            lowerCol.size = new Vector2(0.32f, 3.0f);

            FireTriggerCrystal crystal = crystalObj.AddComponent<FireTriggerCrystal>();
            SerializedObject cSO = new SerializedObject(crystal);
            cSO.FindProperty("spriteRenderer").objectReferenceValue = sr;
            cSO.FindProperty("unlitSprite").objectReferenceValue = unlit;
            cSO.FindProperty("litSprite").objectReferenceValue = lit;
            cSO.FindProperty("glowLight").objectReferenceValue = glowLight;
            cSO.FindProperty("barrierCollider").objectReferenceValue = upperCol;
            cSO.FindProperty("barrierVisual").objectReferenceValue = upperSr;
            cSO.FindProperty("lowerBarrierCollider").objectReferenceValue = lowerCol;
            cSO.FindProperty("lowerBarrierVisual").objectReferenceValue = lowerSr;

            SerializedProperty bfProp = cSO.FindProperty("barrierFrames");
            bfProp.arraySize = barrierFrames.Length;
            for (int i = 0; i < barrierFrames.Length; i++)
            {
                bfProp.GetArrayElementAtIndex(i).objectReferenceValue = barrierFrames[i];
            }

            cSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(crystalObj, CrystalPrefabPath);
            Object.DestroyImmediate(crystalObj);
        }

        private static Sprite EnsureSimpleSquareSprite()
        {
            const string path = "Assets/Art/World/DragonObstacles/PreviewGround.png";
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureSingleSprite(path, 4f);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureSingleSprite(string path, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
