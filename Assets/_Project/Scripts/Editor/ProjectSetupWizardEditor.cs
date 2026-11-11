#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ProjectSetupWizardEditor
{
    private const string SetupFlagKey = "TurnBasedRPG_Phase2_AutoSetupDone_v2";

    static ProjectSetupWizardEditor()
    {
        EditorApplication.delayCall += AutoRunIfNeeded;
    }

    private static void AutoRunIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        bool alreadySetup = File.Exists("Assets/_Project/Scenes/BattleArena.unity");

        if (!alreadySetup && !SessionState.GetBool(SetupFlagKey, false))
        {
            SessionState.SetBool(SetupFlagKey, true);
            RunFullSetup();
        }
    }

    [MenuItem("TurnBasedRPG/Setup/1. Khởi tạo Data, Prefab & Scene (Phase 1 + 2)", priority = 1)]
    public static void RunFullSetup()
    {
        // Xóa selection hiện tại để tránh Unity ListViewShared đọc nhầm index cũ khi tạo lại Asset/Scene
        Selection.objects = System.Array.Empty<Object>();

        EnsureDirectories();
        ConfigureAllSprites();

        SkillDataSO slashSkill = CreateOrUpdateSkill(
            "Assets/_Project/Data/Skills/Skill_HeavySlash.asset",
            "Trảm Kích Hạng Nặng",
            "Chém mạnh vào 1 kẻ địch, gây 160% sát thương vật lý.",
            "Assets/_Project/Art/UI/Icon_Slash.png",
            10,
            SkillCategory.PhysicalDamage,
            SkillTargetType.SingleEnemy,
            1.6f,
            5
        );

        SkillDataSO fireStormSkill = CreateOrUpdateSkill(
            "Assets/_Project/Data/Skills/Skill_FireStorm.asset",
            "Bão Lửa Hủy Diệt",
            "Thiêu đốt toàn bộ đội hình địch.",
            "Assets/_Project/Art/UI/Icon_Fireball.png",
            20,
            SkillCategory.MagicalDamage,
            SkillTargetType.AllEnemies,
            1.3f,
            8
        );

        SkillDataSO holyLightSkill = CreateOrUpdateSkill(
            "Assets/_Project/Data/Skills/Skill_HolyLight.asset",
            "Ánh Sáng Thánh Thần",
            "Hồi phục sinh lực cho 1 đồng minh.",
            "Assets/_Project/Art/UI/Icon_Heal.png",
            15,
            SkillCategory.Heal,
            SkillTargetType.SingleAlly,
            1.4f,
            15
        );

        CharacterDataSO knightData = CreateOrUpdateCharacter(
            "Assets/_Project/Data/Characters/Hero_Knight.asset",
            "Hiệp Sĩ Arthur",
            true,
            "Assets/_Project/Art/Characters/Hero_Knight.png",
            160,
            40,
            26,
            10,
            12,
            new List<SkillDataSO> { slashSkill }
        );

        CharacterDataSO mageData = CreateOrUpdateCharacter(
            "Assets/_Project/Data/Characters/Hero_Mage.asset",
            "Pháp Sư Merlin",
            true,
            "Assets/_Project/Art/Characters/Hero_Mage.png",
            110,
            90,
            22,
            5,
            14,
            new List<SkillDataSO> { fireStormSkill, holyLightSkill }
        );

        CharacterDataSO goblinData = CreateOrUpdateCharacter(
            "Assets/_Project/Data/Characters/Monster_Goblin.asset",
            "Yêu Tinh Goblin",
            false,
            "Assets/_Project/Art/Characters/Monster_Goblin.png",
            95,
            20,
            18,
            4,
            15,
            new List<SkillDataSO> { slashSkill }
        );

        CharacterDataSO orcData = CreateOrUpdateCharacter(
            "Assets/_Project/Data/Characters/Monster_Orc.asset",
            "Chiến Binh Orc",
            false,
            "Assets/_Project/Art/Characters/Monster_Orc.png",
            180,
            30,
            28,
            8,
            9,
            new List<SkillDataSO> { slashSkill }
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject knightPrefab = CreateOrUpdateUnitPrefab("Assets/_Project/Prefabs/Characters/Hero_Knight.prefab", knightData);
        GameObject magePrefab = CreateOrUpdateUnitPrefab("Assets/_Project/Prefabs/Characters/Hero_Mage.prefab", mageData);
        GameObject goblinPrefab = CreateOrUpdateUnitPrefab("Assets/_Project/Prefabs/Characters/Monster_Goblin.prefab", goblinData);
        GameObject orcPrefab = CreateOrUpdateUnitPrefab("Assets/_Project/Prefabs/Characters/Monster_Orc.prefab", orcData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        CreateBattleArenaScene(knightPrefab, magePrefab, goblinPrefab, orcPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TurnBasedRPG Setup] Đã khởi tạo xong toàn bộ Data, Prefab và Scene BattleArena!");
    }

    private static void EnsureDirectories()
    {
        string[] dirs =
        {
            "Assets/_Project/Art/Characters",
            "Assets/_Project/Art/Environment",
            "Assets/_Project/Art/UI",
            "Assets/_Project/Data/Characters",
            "Assets/_Project/Data/Skills",
            "Assets/_Project/Prefabs/Characters",
            "Assets/_Project/Scenes"
        };

        bool createdAny = false;
        foreach (string dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                createdAny = true;
            }
        }

        if (createdAny)
        {
            AssetDatabase.Refresh();
        }
    }

    private static void ConfigureAllSprites()
    {
        string[] spritePaths =
        {
            "Assets/_Project/Art/Characters/Hero_Knight.png",
            "Assets/_Project/Art/Characters/Hero_Mage.png",
            "Assets/_Project/Art/Characters/Monster_Goblin.png",
            "Assets/_Project/Art/Characters/Monster_Orc.png",
            "Assets/_Project/Art/UI/Icon_Slash.png",
            "Assets/_Project/Art/UI/Icon_Fireball.png",
            "Assets/_Project/Art/UI/Icon_Heal.png",
            "Assets/_Project/Art/UI/WhitePixel.png",
            "Assets/_Project/Art/Environment/Arena_Background.png"
        };

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in spritePaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool isPixelArt = !path.Contains("Arena_Background");
                float targetPpu = path.Contains("WhitePixel") ? 16f : (isPixelArt ? 64f : 100f);
                FilterMode targetFilter = isPixelArt ? FilterMode.Point : FilterMode.Bilinear;

                // Chỉ reimport khi thông số Texture chưa đúng chuẩn để tránh giật Project Window
                bool needsReimport =
                    importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    !Mathf.Approximately(importer.spritePixelsPerUnit, targetPpu) ||
                    importer.filterMode != targetFilter ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed;

                if (needsReimport)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = targetPpu;
                    importer.filterMode = targetFilter;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    private static SkillDataSO CreateOrUpdateSkill(
        string assetPath,
        string skillNameValue,
        string descValue,
        string iconPath,
        int manaCostValue,
        SkillCategory categoryValue,
        SkillTargetType targetTypeValue,
        float multiplierValue,
        int flatBonusValue)
    {
        SkillDataSO skill = AssetDatabase.LoadAssetAtPath<SkillDataSO>(assetPath);
        if (skill == null)
        {
            skill = ScriptableObject.CreateInstance<SkillDataSO>();
            AssetDatabase.CreateAsset(skill, assetPath);
        }

        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

        SerializedObject so = new SerializedObject(skill);
        SetString(so, "skillName", skillNameValue);
        SetString(so, "description", descValue);
        SetObject(so, "icon", iconSprite);
        SetInt(so, "manaCost", manaCostValue);
        SetEnum(so, "category", (int)categoryValue);
        SetEnum(so, "targetType", (int)targetTypeValue);
        SetFloat(so, "damageMultiplier", multiplierValue);
        SetInt(so, "flatBonusPower", flatBonusValue);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(skill);
        return skill;
    }

    private static CharacterDataSO CreateOrUpdateCharacter(
        string assetPath,
        string charNameValue,
        bool isHero,
        string spritePath,
        int maxHPValue,
        int maxMPValue,
        int baseATKValue,
        int baseDEFValue,
        int speedValue,
        List<SkillDataSO> skillList)
    {
        CharacterDataSO character = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(assetPath);
        if (character == null)
        {
            character = ScriptableObject.CreateInstance<CharacterDataSO>();
            AssetDatabase.CreateAsset(character, assetPath);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

        SerializedObject so = new SerializedObject(character);
        SetString(so, "characterName", charNameValue);
        SetBool(so, "isPlayerHero", isHero);
        SetObject(so, "characterSprite", sprite);
        SetObject(so, "portraitIcon", sprite);
        SetInt(so, "maxHP", maxHPValue);
        SetInt(so, "maxMP", maxMPValue);
        SetInt(so, "baseATK", baseATKValue);
        SetInt(so, "baseDEF", baseDEFValue);
        SetInt(so, "speed", speedValue);

        SerializedProperty skillsProp = so.FindProperty("skills");
        if (skillsProp != null && skillList != null)
        {
            skillsProp.ClearArray();
            for (int i = 0; i < skillList.Count; i++)
            {
                skillsProp.InsertArrayElementAtIndex(i);
                skillsProp.GetArrayElementAtIndex(i).objectReferenceValue = skillList[i];
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(character);
        return character;
    }

    private static GameObject CreateOrUpdateUnitPrefab(string prefabPath, CharacterDataSO data)
    {
        Sprite whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/WhitePixel.png");

        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        if (data != null)
        {
            sr.sprite = data.CharacterSprite;
        }
        sr.sortingOrder = 10;

        HealthSystem health = root.AddComponent<HealthSystem>();
        ManaSystem mana = root.AddComponent<ManaSystem>();
        BattleUnit unit = root.AddComponent<BattleUnit>();

        GameObject hitPoint = new GameObject("HitPoint");
        hitPoint.transform.SetParent(root.transform, false);
        hitPoint.transform.localPosition = new Vector3(0f, 0.3f, 0f);

        GameObject barsRoot = new GameObject("OverheadBars");
        barsRoot.transform.SetParent(root.transform, false);
        barsRoot.transform.localPosition = new Vector3(0f, 1.35f, 0f);

        bool isHero = data != null && data.IsPlayerHero;

        Transform hpFill = CreateBarHierarchy(
            barsRoot.transform,
            "HPBar",
            new Vector3(0f, 0.14f, 0f),
            new Vector2(1.4f, 0.16f),
            whiteSprite,
            new Color(0.15f, 0.15f, 0.15f, 0.9f),
            isHero ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.9f, 0.25f, 0.25f, 1f)
        );

        Transform mpFill = CreateBarHierarchy(
            barsRoot.transform,
            "MPBar",
            new Vector3(0f, -0.05f, 0f),
            new Vector2(1.4f, 0.10f),
            whiteSprite,
            new Color(0.12f, 0.12f, 0.18f, 0.9f),
            new Color(0.25f, 0.65f, 0.95f, 1f)
        );

        SerializedObject unitSO = new SerializedObject(unit);
        SetObject(unitSO, "characterData", data);
        SetObject(unitSO, "spriteRenderer", sr);
        SetObject(unitSO, "healthSystem", health);
        SetObject(unitSO, "manaSystem", mana);
        SetObject(unitSO, "hpBarFill", hpFill);
        SetObject(unitSO, "mpBarFill", mpFill);
        SetObject(unitSO, "hitPoint", hitPoint.transform);
        unitSO.ApplyModifiedPropertiesWithoutUndo();

        if (data != null)
        {
            health.Initialize(data.MaxHP);
            mana.Initialize(data.MaxMP);
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return savedPrefab;
    }

    private static Transform CreateBarHierarchy(
        Transform parent,
        string prefix,
        Vector3 localPos,
        Vector2 size,
        Sprite whiteSprite,
        Color bgColor,
        Color fillColor)
    {
        GameObject bgObj = new GameObject($"{prefix}_BG");
        bgObj.transform.SetParent(parent, false);
        bgObj.transform.localPosition = localPos;
        bgObj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = whiteSprite;
        bgSr.color = bgColor;
        bgSr.sortingOrder = 20;

        GameObject fillObj = new GameObject($"{prefix}_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = Vector3.one;

        SpriteRenderer fillSr = fillObj.AddComponent<SpriteRenderer>();
        fillSr.sprite = whiteSprite;
        fillSr.color = fillColor;
        fillSr.sortingOrder = 21;

        return fillObj.transform;
    }

    private static void CreateBattleArenaScene(
        GameObject knightPrefab,
        GameObject magePrefab,
        GameObject goblinPrefab,
        GameObject orcPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.11f, 0.18f, 1f);
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        camObj.AddComponent<AudioListener>();

        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Environment/Arena_Background.png");
        if (bgSprite != null)
        {
            GameObject bgObj = new GameObject("Arena_Background");
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingOrder = -10;
            bgObj.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
        }

        GameObject heroTeamRoot = new GameObject("HeroParty");
        BattleUnit knightUnit = SpawnPrefabUnit(knightPrefab, heroTeamRoot.transform, "Hero_Knight", new Vector3(-3.2f, -0.4f, 0f));
        BattleUnit mageUnit = SpawnPrefabUnit(magePrefab, heroTeamRoot.transform, "Hero_Mage", new Vector3(-5.2f, -1.2f, 0f));

        GameObject enemyTeamRoot = new GameObject("MonsterParty");
        BattleUnit goblinUnit = SpawnPrefabUnit(goblinPrefab, enemyTeamRoot.transform, "Monster_Goblin", new Vector3(3.2f, -0.4f, 0f));
        BattleUnit orcUnit = SpawnPrefabUnit(orcPrefab, enemyTeamRoot.transform, "Monster_Orc", new Vector3(5.2f, -1.2f, 0f));

        GameObject sandboxObj = new GameObject("BattleStateMachine");
        BattleStateMachine stateMachine = sandboxObj.AddComponent<BattleStateMachine>();
        stateMachine.SetParties(
            new List<BattleUnit> { knightUnit, mageUnit },
            new List<BattleUnit> { goblinUnit, orcUnit }
        );
        EditorUtility.SetDirty(stateMachine);

        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/BattleArena.unity");
    }

    [MenuItem("TurnBasedRPG/Setup/2. Nâng cấp Scene sang BattleStateMachine (Phase 3)", priority = 2)]
    public static void UpgradeSceneToPhase3()
    {
        Selection.objects = System.Array.Empty<Object>();

        Phase2BattleSandbox oldSandbox = Object.FindFirstObjectByType<Phase2BattleSandbox>();
        if (oldSandbox != null)
        {
            Object.DestroyImmediate(oldSandbox.gameObject);
        }

        BattleStateMachine fsm = Object.FindFirstObjectByType<BattleStateMachine>();
        if (fsm == null)
        {
            GameObject fsmObj = new GameObject("BattleStateMachine");
            fsm = fsmObj.AddComponent<BattleStateMachine>();
        }

        List<BattleUnit> heroes = new List<BattleUnit>();
        GameObject heroParty = GameObject.Find("HeroParty");
        if (heroParty != null)
        {
            heroParty.GetComponentsInChildren(true, heroes);
        }

        List<BattleUnit> enemies = new List<BattleUnit>();
        GameObject monsterParty = GameObject.Find("MonsterParty");
        if (monsterParty != null)
        {
            monsterParty.GetComponentsInChildren(true, enemies);
        }

        fsm.SetParties(heroes, enemies);
        EditorUtility.SetDirty(fsm);

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene);
        }

        Debug.Log("[TurnBasedRPG Setup] Đã nâng cấp Scene sang BattleStateMachine (Phase 3) thành công!");
    }

    [MenuItem("TurnBasedRPG/Setup/3. Khởi tạo Canvas Battle HUD & Damage Text Pool (Phase 5)", priority = 3)]
    public static void UpgradeSceneToPhase5HUD()
    {
        Selection.objects = System.Array.Empty<Object>();

        if (!Directory.Exists("Assets/_Project/Prefabs/Combat"))
        {
            Directory.CreateDirectory("Assets/_Project/Prefabs/Combat");
            AssetDatabase.Refresh();
        }

        // 1. Đảm bảo Scene đã có BattleStateMachine
        BattleStateMachine fsm = Object.FindFirstObjectByType<BattleStateMachine>();
        if (fsm == null)
        {
            UpgradeSceneToPhase3();
            fsm = Object.FindFirstObjectByType<BattleStateMachine>();
        }

        if (fsm != null)
        {
            fsm.SetShowDebugHUD(false);
            EditorUtility.SetDirty(fsm);
        }

        // 2. Tạo Prefab FloatingDamageText trong Assets/_Project/Prefabs/Combat/
        GameObject damageTextPrefab = CreateOrUpdateDamageTextPrefab("Assets/_Project/Prefabs/Combat/FloatingDamageText.prefab");

        // 3. Tạo ObjectPoolManager và DamageTextSpawner trong Scene
        ObjectPoolManager poolManager = Object.FindFirstObjectByType<ObjectPoolManager>();
        if (poolManager == null)
        {
            GameObject poolObj = new GameObject("ObjectPoolManager");
            poolManager = poolObj.AddComponent<ObjectPoolManager>();
        }
        poolManager.SetPrewarmItem(damageTextPrefab, 8);
        EditorUtility.SetDirty(poolManager);

        DamageTextSpawner spawner = Object.FindFirstObjectByType<DamageTextSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("DamageTextSpawner");
            spawner = spawnerObj.AddComponent<DamageTextSpawner>();
        }
        spawner.SetupSpawner(fsm, poolManager, damageTextPrefab);
        EditorUtility.SetDirty(spawner);

        // 4. Tạo EventSystem tương thích với Unity 6 Input System
        EnsureEventSystem();

        // 5. Tạo hoặc làm mới BattleHUD_Canvas
        BattleHUDUI existingHud = Object.FindFirstObjectByType<BattleHUDUI>();
        if (existingHud != null)
        {
            Object.DestroyImmediate(existingHud.gameObject);
        }

        CreateBattleHUDCanvas(fsm);

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[TurnBasedRPG Setup] Đã khởi tạo xong Canvas Battle HUD, Smooth Bars & Damage Text Pool (Phase 5)!");
    }

    private static GameObject CreateOrUpdateDamageTextPrefab(string prefabPath)
    {
        GameObject root = new GameObject("FloatingDamageText");

        TextMesh tm = root.AddComponent<TextMesh>();
        tm.text = "-25";
        tm.fontSize = 48;
        tm.characterSize = 0.09f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.3f, 0.2f, 1f);

        MeshRenderer mr = root.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 50;
        }

        PooledObject pooled = root.AddComponent<PooledObject>();
        BattleFloatingDamageText dmgText = root.AddComponent<BattleFloatingDamageText>();

        SerializedObject so = new SerializedObject(dmgText);
        SetObject(so, "textMesh", tm);
        SetObject(so, "meshRenderer", mr);
        SetObject(so, "pooledObject", pooled);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();

            System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                esObj.AddComponent(inputSystemModuleType);
            }
            else
            {
                esObj.AddComponent<StandaloneInputModule>();
            }
        }
    }

    private static Font GetDefaultUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }

    private static void CreateBattleHUDCanvas(BattleStateMachine fsm)
    {
        Font uiFont = GetDefaultUIFont();

        GameObject canvasObj = new GameObject("BattleHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        BattleHUDUI hud = canvasObj.AddComponent<BattleHUDUI>();

        // 1. Top Banner
        RectTransform topBanner = CreateUIPanel(
            canvasObj.transform,
            "TopBanner",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            new Vector2(-24f, 68f),
            new Color(0.08f, 0.1f, 0.16f, 0.92f)
        );

        Text roundStateTxt = CreateUIText(
            topBanner,
            "RoundAndStateText",
            uiFont,
            15,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            new Color(0.55f, 0.85f, 1f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(0.35f, 1f),
            new Vector2(16f, -2f),
            new Vector2(-8f, -4f),
            "HIỆP 1 | TRẠNG THÁI: BattleStart"
        );

        Text statusTxt = CreateUIText(
            topBanner,
            "BattleStatusText",
            uiFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            new Color(1f, 0.9f, 0.35f, 1f),
            new Vector2(0.35f, 0.5f),
            new Vector2(1f, 1f),
            new Vector2(8f, -2f),
            new Vector2(-16f, -4f),
            "Trận đấu bắt đầu!"
        );

        Text queueTxt = CreateUIText(
            topBanner,
            "TurnOrderQueueText",
            uiFont,
            14,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(0.88f, 0.92f, 0.96f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0.5f),
            new Vector2(16f, 4f),
            new Vector2(-16f, -2f),
            "Hàng đợi lượt kế tiếp: ..."
        );

        // 2. Unit Status Cards (2 Hero bên trái, 2 Quái bên phải)
        List<UnitStatusPanelUI> statusPanels = new List<UnitStatusPanelUI>();
        if (fsm != null)
        {
            for (int i = 0; i < fsm.HeroUnits.Count; i++)
            {
                BattleUnit hero = fsm.HeroUnits[i];
                if (hero == null) continue;

                UnitStatusPanelUI card = CreateUnitStatusCard(
                    canvasObj.transform,
                    uiFont,
                    hero,
                    isLeftAligned: true,
                    verticalIndex: i
                );
                statusPanels.Add(card);
            }

            for (int i = 0; i < fsm.EnemyUnits.Count; i++)
            {
                BattleUnit enemy = fsm.EnemyUnits[i];
                if (enemy == null) continue;

                UnitStatusPanelUI card = CreateUnitStatusCard(
                    canvasObj.transform,
                    uiFont,
                    enemy,
                    isLeftAligned: false,
                    verticalIndex: i
                );
                statusPanels.Add(card);
            }
        }

        // 3. Bottom Command Panel
        RectTransform cmdPanel = CreateUIPanel(
            canvasObj.transform,
            "CommandPanel",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 10f),
            new Vector2(-24f, 118f),
            new Color(0.08f, 0.1f, 0.16f, 0.94f)
        );

        Text heroHeaderTxt = CreateUIText(
            cmdPanel,
            "ActiveHeroHeaderText",
            uiFont,
            15,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            new Color(0.45f, 0.95f, 0.6f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(18f, -18f),
            new Vector2(-36f, 26f),
            "LỆNH CỦA: HIỆP SĨ ARTHUR"
        );
        heroHeaderTxt.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Nhóm nút lệnh chính (Đánh thường, Phòng thủ, Kỹ năng)
        GameObject mainGroup = new GameObject("MainActionButtonsGroup", typeof(RectTransform));
        mainGroup.transform.SetParent(cmdPanel, false);
        RectTransform mainGroupRect = mainGroup.GetComponent<RectTransform>();
        mainGroupRect.anchorMin = Vector2.zero;
        mainGroupRect.anchorMax = Vector2.one;
        mainGroupRect.offsetMin = new Vector2(16f, 12f);
        mainGroupRect.offsetMax = new Vector2(-16f, -36f);

        Button atkBtn = CreateUIButton(
            mainGroup.transform,
            "BasicAttackButton",
            uiFont,
            "Đánh Thường",
            new Color(0.75f, 0.24f, 0.22f, 1f),
            new Vector2(110f, 0f),
            new Vector2(210f, 52f),
            out _
        );

        Button defBtn = CreateUIButton(
            mainGroup.transform,
            "DefendButton",
            uiFont,
            "Phòng Thủ (+MP)",
            new Color(0.22f, 0.48f, 0.78f, 1f),
            new Vector2(332f, 0f),
            new Vector2(210f, 52f),
            out _
        );

        List<Button> skillBtns = new List<Button>();
        List<Text> skillLabels = new List<Text>();
        for (int i = 0; i < 3; i++)
        {
            float posX = 575f + i * 268f;
            Button sBtn = CreateUIButton(
                mainGroup.transform,
                $"SkillButton_{i + 1}",
                uiFont,
                $"Kỹ Năng {i + 1}",
                new Color(0.52f, 0.26f, 0.72f, 1f),
                new Vector2(posX, 0f),
                new Vector2(256f, 52f),
                out Text sLabel
            );
            skillBtns.Add(sBtn);
            skillLabels.Add(sLabel);
        }

        // Nhóm nút chọn mục tiêu (Target Selection)
        GameObject targetGroup = new GameObject("TargetSelectionGroup", typeof(RectTransform));
        targetGroup.transform.SetParent(cmdPanel, false);
        RectTransform targetGroupRect = targetGroup.GetComponent<RectTransform>();
        targetGroupRect.anchorMin = Vector2.zero;
        targetGroupRect.anchorMax = Vector2.one;
        targetGroupRect.offsetMin = new Vector2(16f, 12f);
        targetGroupRect.offsetMax = new Vector2(-16f, -36f);

        List<Button> targetBtns = new List<Button>();
        List<Text> targetLabels = new List<Text>();
        for (int i = 0; i < 3; i++)
        {
            float posX = 135f + i * 275f;
            Button tBtn = CreateUIButton(
                targetGroup.transform,
                $"TargetButton_{i + 1}",
                uiFont,
                $"Mục Tiêu {i + 1}",
                new Color(0.8f, 0.42f, 0.16f, 1f),
                new Vector2(posX, 0f),
                new Vector2(260f, 52f),
                out Text tLabel
            );
            targetBtns.Add(tBtn);
            targetLabels.Add(tLabel);
        }

        Button cancelBtn = CreateUIButton(
            targetGroup.transform,
            "CancelTargetButton",
            uiFont,
            "Quay Lại (Hủy)",
            new Color(0.35f, 0.38f, 0.44f, 1f),
            new Vector2(980f, 0f),
            new Vector2(190f, 52f),
            out _
        );
        targetGroup.SetActive(false);

        // 4. End Battle Overlay Panel
        RectTransform endPanel = CreateUIPanel(
            canvasObj.transform,
            "EndBattlePanel",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(540f, 210f),
            new Color(0.06f, 0.08f, 0.14f, 0.96f)
        );

        Text endTitleTxt = CreateUIText(
            endPanel,
            "EndBattleTitleText",
            uiFont,
            22,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.85f, 0.25f, 1f),
            new Vector2(0f, 0.45f),
            new Vector2(1f, 1f),
            new Vector2(20f, 0f),
            new Vector2(-20f, -15f),
            "CHIẾN THẮNG!"
        );

        Button restartBtn = CreateUIButton(
            endPanel,
            "RestartBattleButton",
            uiFont,
            "Chơi Lại Trận Đấu (Restart)",
            new Color(0.2f, 0.68f, 0.36f, 1f),
            new Vector2(270f, -45f),
            new Vector2(280f, 52f),
            out _
        );
        endPanel.gameObject.SetActive(false);

        hud.ConfigureHUDReferences(
            fsm,
            roundStateTxt,
            statusTxt,
            queueTxt,
            statusPanels,
            cmdPanel.gameObject,
            heroHeaderTxt,
            mainGroup,
            atkBtn,
            defBtn,
            skillBtns,
            skillLabels,
            targetGroup,
            targetBtns,
            targetLabels,
            cancelBtn,
            endPanel.gameObject,
            endTitleTxt,
            restartBtn
        );

        EditorUtility.SetDirty(hud);
    }

    private static UnitStatusPanelUI CreateUnitStatusCard(
        Transform canvasTransform,
        Font uiFont,
        BattleUnit unit,
        bool isLeftAligned,
        int verticalIndex)
    {
        float anchorX = isLeftAligned ? 0f : 1f;
        float posX = isLeftAligned ? 14f : -14f;
        float posY = -84f - verticalIndex * 80f;

        RectTransform cardRect = CreateUIPanel(
            canvasTransform,
            $"StatusCard_{unit.name}",
            new Vector2(anchorX, 1f),
            new Vector2(anchorX, 1f),
            new Vector2(anchorX, 1f),
            new Vector2(posX, posY),
            new Vector2(276f, 72f),
            new Color(0.12f, 0.15f, 0.22f, 0.92f)
        );

        Image bgImg = cardRect.GetComponent<Image>();

        GameObject iconObj = new GameObject("PortraitIcon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(cardRect, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        iconRect.sizeDelta = new Vector2(52f, 52f);

        Image portraitImg = iconObj.GetComponent<Image>();
        if (unit.CharacterData != null)
        {
            portraitImg.sprite = unit.CharacterData.PortraitIcon;
        }

        Text nameTxt = CreateUIText(
            cardRect,
            "UnitNameText",
            uiFont,
            13,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            Color.white,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(68f, -22f),
            new Vector2(-8f, -2f),
            $"{unit.UnitName} (SPD {unit.Speed})"
        );

        Color hpColor = unit.IsPlayerUnit
            ? new Color(0.22f, 0.82f, 0.36f, 1f)
            : new Color(0.88f, 0.26f, 0.24f, 1f);

        SmoothBarUI hpBar = CreateSmoothBar(
            cardRect,
            "HPBar",
            uiFont,
            "HP",
            hpColor,
            new Vector2(68f, -42f),
            unit.Health != null ? unit.Health.CurrentHP : 100,
            unit.Health != null ? unit.Health.MaxHP : 100
        );

        SmoothBarUI mpBar = CreateSmoothBar(
            cardRect,
            "MPBar",
            uiFont,
            "MP",
            new Color(0.24f, 0.62f, 0.95f, 1f),
            new Vector2(68f, -62f),
            unit.Mana != null ? unit.Mana.CurrentMP : 50,
            unit.Mana != null ? unit.Mana.MaxMP : 50
        );

        UnitStatusPanelUI statusPanel = cardRect.gameObject.AddComponent<UnitStatusPanelUI>();
        statusPanel.ConfigurePanelElements(unit, bgImg, portraitImg, nameTxt, hpBar, mpBar);
        EditorUtility.SetDirty(statusPanel);
        return statusPanel;
    }

    private static SmoothBarUI CreateSmoothBar(
        Transform parent,
        string barName,
        Font uiFont,
        string prefix,
        Color fillColor,
        Vector2 offsetBottomLeft,
        int currentVal,
        int maxVal)
    {
        GameObject barBgObj = new GameObject(barName, typeof(RectTransform), typeof(Image));
        barBgObj.transform.SetParent(parent, false);
        RectTransform bgRect = barBgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 0f);
        bgRect.anchoredPosition = offsetBottomLeft;
        bgRect.sizeDelta = new Vector2(198f, 16f);

        Image bgImg = barBgObj.GetComponent<Image>();
        bgImg.color = new Color(0.06f, 0.07f, 0.1f, 0.95f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgRect, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = fillColor;

        Text valTxt = CreateUIText(
            bgRect,
            "ValueText",
            uiFont,
            11,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            $"{prefix}: {currentVal}/{maxVal}"
        );

        SmoothBarUI smoothBar = barBgObj.AddComponent<SmoothBarUI>();
        smoothBar.ConfigureReferences(fillRect, valTxt, prefix);
        EditorUtility.SetDirty(smoothBar);
        return smoothBar;
    }

    private static RectTransform CreateUIPanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        Color bgColor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Image img = obj.GetComponent<Image>();
        img.color = bgColor;
        return rect;
    }

    private static Text CreateUIText(
        Transform parent,
        string name,
        Font font,
        int fontSize,
        FontStyle style,
        TextAnchor alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string content)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text txt = obj.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = alignment;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    private static Button CreateUIButton(
        Transform parent,
        string name,
        Font font,
        string labelText,
        Color btnColor,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        out Text labelComponent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        Image img = obj.GetComponent<Image>();
        img.color = btnColor;

        Button btn = obj.GetComponent<Button>();
        btn.targetGraphic = img;

        labelComponent = CreateUIText(
            rect,
            "Label",
            font,
            13,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            Color.white,
            Vector2.zero,
            Vector2.one,
            new Vector2(6f, 2f),
            new Vector2(-6f, -2f),
            labelText
        );

        return btn;
    }

    private static BattleUnit SpawnPrefabUnit(GameObject prefab, Transform parent, string objName, Vector3 pos)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            instance = Object.Instantiate(prefab, parent);
        }

        instance.name = objName;
        instance.transform.position = pos;
        return instance.GetComponent<BattleUnit>();
    }

    private static void SetString(SerializedObject so, string propName, string value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.stringValue = value;
    }

    private static void SetInt(SerializedObject so, string propName, int value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.intValue = value;
    }

    private static void SetFloat(SerializedObject so, string propName, float value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.floatValue = value;
    }

    private static void SetBool(SerializedObject so, string propName, bool value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.boolValue = value;
    }

    private static void SetEnum(SerializedObject so, string propName, int enumIndex)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.enumValueIndex = enumIndex;
    }

    private static void SetObject(SerializedObject so, string propName, Object value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null) p.objectReferenceValue = value;
    }
}
#endif
