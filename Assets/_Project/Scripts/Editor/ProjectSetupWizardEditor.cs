#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
