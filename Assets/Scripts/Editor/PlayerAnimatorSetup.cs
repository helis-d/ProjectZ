using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace ProjectZ.Editor
{
    /// <summary>
    /// Unity Editor aracı: Player için AnimatorController oluşturur.
    /// Menüden Tools > ProjectZ > Create Player Animator ile çalıştır.
    /// </summary>
    public static class PlayerAnimatorSetup
    {
        [MenuItem("Tools/ProjectZ/Create Player Animator Controller")]
        public static void CreateController()
        {
            // ─── OTOMATİK FBX AYARLAMA (Humanoid) ───
            // Kullanıcının FBX dosyalarını bulup Humanoid yapmasına gerek kalmadan kodla yapıyoruz
            string mixamoAnimPath = "Assets/Animations/X Bot@Shooting.fbx";
            string syntyIdlePath = "Assets/Synty/SidekickCharacters/_Demos/Animations/BodyCycles/A_Body_IdleSubtle.fbx";

            SetToHumanoid(mixamoAnimPath);
            SetToHumanoid(syntyIdlePath);

            // Klasörü oluştur
            string folderPath = "Assets/Animations";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            string controllerPath = folderPath + "/PlayerAnimatorController.controller";

            // Controller oluştur
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Parametre ekle — hareket hızını bu değerle kontrol edeceğiz
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            // Root state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            // Idle animasyon klibini bul
            AnimationClip idleClip = FindAnimationClip("A_Body_IdleSubtle");

            // ─── IDLE State ───
            var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
            if (idleClip != null)
            {
                idleState.motion = idleClip;
            }
            rootStateMachine.defaultState = idleState;

            // ─── WALK State (Idle animasyonunu hızlandırarak yürüme hissi) ───
            var walkState = rootStateMachine.AddState("Walk", new Vector3(300, 100, 0));
            if (idleClip != null)
            {
                walkState.motion = idleClip;
                walkState.speed = 2.5f; // Daha hızlı oynat
            }

            // ─── Geçişler ───
            // Idle → Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToWalk.duration = 0.15f;
            idleToWalk.hasExitTime = false;

            // Walk → Idle
            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            walkToIdle.duration = 0.2f;
            walkToIdle.hasExitTime = false;

            // ─── UPPER BODY LAYER (Ateş etme animasyonu için) ───
            controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);

            // Üst vücut maskesi oluştur (sadece kolları ve gövdeyi etkiler)
            string maskPath = folderPath + "/UpperBodyMask.mask";
            AvatarMask upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (upperBodyMask == null)
            {
                upperBodyMask = new AvatarMask();
                upperBodyMask.name = "UpperBodyMask";
                // Humanoid maskesi ayarla (sadece kollar ve gövde açık)
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    upperBodyMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                }
                upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
                upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
                upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
                upperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
                
                AssetDatabase.CreateAsset(upperBodyMask, maskPath);
            }

            // Yeni katman ekle (Katman 1)
            AnimatorControllerLayer upperBodyLayer = new AnimatorControllerLayer
            {
                name = "UpperBody",
                stateMachine = new AnimatorStateMachine { name = "UpperBody", hideFlags = HideFlags.HideInHierarchy },
                avatarMask = upperBodyMask,
                defaultWeight = 1f, // Katman ağırlığı tam
                blendingMode = AnimatorLayerBlendingMode.Override
            };
            
            AssetDatabase.AddObjectToAsset(upperBodyLayer.stateMachine, controller);
            controller.AddLayer(upperBodyLayer);

            var shootStateMachine = controller.layers[1].stateMachine;

            // Boş Idle (silah sıkmıyorken alt katmanı gösterir)
            var upperIdleState = shootStateMachine.AddState("Empty", new Vector3(300, 0, 0));
            shootStateMachine.defaultState = upperIdleState;

            // Ateş Etme Durumu
            var shootState = shootStateMachine.AddState("Shoot", new Vector3(300, 100, 0));
            AnimationClip shootClip = FindAnimationClip("X Bot@Shooting"); // Mixamo animasyonunun adı
            if (shootClip != null)
            {
                shootState.motion = shootClip;
                shootState.speed = 1.5f; // Biraz hızlandır
            }

            // Any State → Shoot
            var anyToShoot = shootStateMachine.AddAnyStateTransition(shootState);
            anyToShoot.AddCondition(AnimatorConditionMode.If, 0, "Fire");
            anyToShoot.duration = 0.05f;

            // Shoot → Empty
            var shootToEmpty = shootState.AddTransition(upperIdleState);
            shootToEmpty.hasExitTime = true;
            shootToEmpty.exitTime = 0.8f; // Animasyonun %80'i bitince dön
            shootToEmpty.duration = 0.2f;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Kullanıcıya sonucu göster
            EditorUtility.DisplayDialog(
                "Başarılı!",
                $"PlayerAnimatorController oluşturuldu:\n{controllerPath}\n\n" +
                "Şimdi Player prefabının içindeki Human-Custom objesine\n" +
                "bu controller'ı Animator bileşenine sürükle-bırak yap.",
                "Tamam");

            // Oluşturulan dosyayı seç
            var asset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            // ─── OTOMATİK PREFAB ATAMASI ───
            AssignControllerToPrefab(asset);

            Debug.Log($"[ProjectZ] PlayerAnimatorController başarıyla oluşturuldu: {controllerPath}");
        }

        private static void AssignControllerToPrefab(RuntimeAnimatorController controller)
        {
            if (controller == null) return;

            string prefabPath = "Assets/Player.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                // Prefab içindeki Human-Custom objesini bul
                Transform humanCustom = prefab.transform.Find("Human-Custom");
                if (humanCustom != null)
                {
                    Animator animator = humanCustom.GetComponent<Animator>();
                    if (animator != null)
                    {
                        // Sadece Unity Editor'de prefabı güvenle değiştirmek için
                        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                        {
                            var prefabRoot = editingScope.prefabContentsRoot;
                            var targHuman = prefabRoot.transform.Find("Human-Custom");
                            if (targHuman != null)
                            {
                                var targAnim = targHuman.GetComponent<Animator>();
                                if (targAnim != null)
                                {
                                    targAnim.runtimeAnimatorController = controller;
                                    Debug.Log("[ProjectZ] PlayerAnimatorController başarıyla Player prefabına bağlandı!");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[ProjectZ] Player prefabının içinde 'Human-Custom' objesi bulunamadı!");
                }
            }
            else
            {
                Debug.LogWarning("[ProjectZ] 'Assets/Player.prefab' dosyası bulunamadı, manuel atama gerekiyor.");
            }
        }

        private static AnimationClip FindAnimationClip(string name)
        {
            // FBX dosyasının içindeki animation clip'i bul
            string[] guids = AssetDatabase.FindAssets(name);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    // FBX'in içindeki tüm assetleri tara
                    Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (Object sub in subAssets)
                    {
                        if (sub is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            Debug.Log($"[ProjectZ] Animasyon bulundu: {clip.name} ({path})");
                            return clip;
                        }
                    }
                }
                else if (path.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip != null)
                    {
                        Debug.Log($"[ProjectZ] Animasyon bulundu: {clip.name} ({path})");
                        return clip;
                    }
                }
            }
            Debug.LogWarning("[ProjectZ] Idle animasyon klibi bulunamadı!");
            return null;
        }

        private static void SetToHumanoid(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.SaveAndReimport();
                    Debug.Log($"[ProjectZ] {path} başarıyla Humanoid olarak ayarlandı!");
                }
            }
            else
            {
                Debug.LogWarning($"[ProjectZ] Dosya bulunamadı veya ModelImporter geçersiz: {path}");
            }
        }
    }
}
