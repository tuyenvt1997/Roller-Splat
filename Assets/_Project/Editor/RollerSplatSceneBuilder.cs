using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace RollerSplat.EditorTools
{
    /// <summary>
    /// Tools → Roller Splat → Build Sample Scene: generates sprites, tiles, two level prefabs
    /// and a fully wired Game scene. Safe to run again; it overwrites the generated assets.
    /// </summary>
    public static partial class RollerSplatSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string SpritesDir = Root + "/Sprites";
        const string TilesDir = Root + "/Tiles";
        const string LevelsDir = Root + "/Prefabs/Levels";
        const string ScenePath = Root + "/Scenes/Game.unity";

        static readonly Color WallColor = new Color32(0x1E, 0x2A, 0x44, 0xFF);
        static readonly Color BallColor = new Color(1f, 0.45f, 0.7f);
        static readonly Color BackgroundColor = new Color32(0x10, 0x14, 0x24, 0xFF);

        // '#' = wall, '.' = floor, 'S' = floor + start point. First row is the top of the level.
        // The game is portrait, so keep maps taller than they are wide.
        static readonly (string name, string[] map)[] Levels =
        {
            ("Level_01", new[]
            {
                "#####",
                "#S..#",
                "#.#.#",
                "#.#.#",
                "#.#.#",
                "#...#",
                "#####",
            }),
            ("Level_02", new[]
            {
                "#######",
                "#S#...#",
                "#.#.#.#",
                "#...#.#",
                "#.###.#",
                "#.###.#",
                "#.....#",
                "#######",
            }),
            ("Level_03", new[]
            {
                "#######",
                "#.....#",
                "#.#.#.#",
                "#.#.S.#",
                "#.#.#.#",
                "#.#.#.#",
                "#.#.#.#",
                "#.....#",
                "#######",
            }),
            ("Level_04", new[]
            {
                "########",
                "#......#",
                "#.##.#.#",
                "#.##.#.#",
                "#....#.#",
                "#.#..#.#",
                "#.#.##.#",
                "#..S...#",
                "########",
            }),
            ("Level_05", new[]
            {
                "########",
                "#...####",
                "#.#....#",
                "#.#.##.#",
                "#.#.#S.#",
                "#......#",
                "#.#.#.##",
                "#.###.##",
                "#.....##",
                "########",
            }),
            ("Level_06", new[]
            {
                "#########",
                "#.....###",
                "#.###.###",
                "#.......#",
                "#.###.#.#",
                "#.###.#.#",
                "#..S....#",
                "#.#.#.#.#",
                "#.......#",
                "#########",
            }),
            ("Level_07", new[]
            {
                "#########",
                "#.......#",
                "#.##.##.#",
                "#.##....#",
                "#....S#.#",
                "#.##..###",
                "#.##..###",
                "#.###.###",
                "#.###.###",
                "#......##",
                "#########",
            }),
            ("Level_08", new[]
            {
                "#########",
                "#.....###",
                "#.##.####",
                "#.##.####",
                "#.##.####",
                "#.##.#..#",
                "#.......#",
                "####.##.#",
                "#.#S.##.#",
                "#.#..##.#",
                "#.......#",
                "#########",
            }),
            ("Level_09", new[]
            {
                "##########",
                "#........#",
                "#.######.#",
                "#.######.#",
                "#.######.#",
                "#.######.#",
                "#........#",
                "#.####.#.#",
                "#..###.S.#",
                "##.###...#",
                "#........#",
                "##########",
            }),
            ("Level_10", new[]
            {
                "##########",
                "#....##..#",
                "#.#.#....#",
                "#.#.#.#..#",
                "#.#.#....#",
                "#.#.#.##.#",
                "#.#.#.##.#",
                "#........#",
                "#.#.#.####",
                "#.#.#.####",
                "#.#.#.####",
                "#...#S####",
                "##########",
            }),
        };

        [MenuItem("Tools/Roller Splat/Build Sample Scene")]
        public static void Build()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                EditorUtility.DisplayDialog("Roller Splat",
                    "Đã import TMP Essential Resources.\nĐợi Unity import xong rồi bấm lại Tools → Roller Splat → Build Sample Scene.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Roller Splat",
                    $"{ScenePath} đã tồn tại. Tạo lại (ghi đè Scene, tile và prefab level)?", "Ghi đè", "Huỷ"))
                return;

            foreach (var dir in new[] { SpritesDir, TilesDir, LevelsDir, Path.GetDirectoryName(ScenePath) })
                Directory.CreateDirectory(dir);

            var square = CreateSprite("Square", 64, circle: false);
            var circle = CreateSprite("Circle", 128, circle: true);
            var floorTile = CreateTile("FloorTile", CreateCellSprite(), Color.white);
            var wallTile = CreateTile("WallTile", square, WallColor);

            var levelPaths = new string[Levels.Length];
            for (int i = 0; i < Levels.Length; i++)
                levelPaths[i] = CreateLevelPrefab(Levels[i].name, Levels[i].map, floorTile, wallTile);

            BuildScene(levelPaths, square, circle);

            AssetDatabase.SaveAssets();
            Debug.Log($"Roller Splat: built {ScenePath} with {levelPaths.Length} levels. Press Play!");
        }

        /// <summary>
        /// Regenerates only the level prefabs (with the existing Floor/Wall tiles, so custom sprites are kept)
        /// and refreshes the LevelLoader list in the open scene. The scene itself is not rebuilt.
        /// </summary>
        [MenuItem("Tools/Roller Splat/Rebuild Levels")]
        public static void RebuildLevels()
        {
            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TilesDir}/FloorTile.asset");
            var wallTile = AssetDatabase.LoadAssetAtPath<Tile>($"{TilesDir}/WallTile.asset");
            var loader = Object.FindFirstObjectByType<LevelLoader>();
            if (floorTile == null || wallTile == null || loader == null)
            {
                Debug.LogError("Roller Splat: need FloorTile/WallTile assets and a LevelLoader in the open scene. Run Build Sample Scene first.");
                return;
            }

            Directory.CreateDirectory(LevelsDir);
            var so = new SerializedObject(loader);
            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = Levels.Length;
            for (int i = 0; i < Levels.Length; i++)
            {
                string path = CreateLevelPrefab(Levels[i].name, Levels[i].map, floorTile, wallTile);
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Level>();
            }
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(loader.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Roller Splat: rebuilt {Levels.Length} levels. Save the scene to keep the LevelLoader list.");
        }

        /// <summary>
        /// Upgrades the open Game scene's UI in place: replaces the old win panel with the kit popups and
        /// adds the HUD clock. Levels, tiles and the rest of the scene are left alone.
        /// </summary>
        [MenuItem("Tools/Roller Splat/Rebuild Popups")]
        public static void RebuildPopups()
        {
            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            var gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (ui == null || gameManager == null)
            {
                Debug.LogError("Roller Splat: open the Game scene (needs a UIManager and a GameManager) first.");
                return;
            }

            var so = new SerializedObject(ui);
            foreach (var field in new[] { "winPanel", "losePanel" })
            {
                var old = so.FindProperty(field).objectReferenceValue as GameObject;
                if (old == null) continue;
                // The kit popups live on their own canvas; drop that whole canvas, otherwise just the panel.
                var target = old.transform.parent != null && old.transform.parent.name == "PopupCanvas"
                    ? old.transform.parent.gameObject : old;
                Object.DestroyImmediate(target);
            }
            // Settings popup and its HUD button are rebuilt too.
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && (t.name == "SettingsCanvas" && t.parent == null || t.name == "SettingsButton" && t.parent == ui.transform))
                    Object.DestroyImmediate(t.gameObject);

            var existingTime = so.FindProperty("timeText").objectReferenceValue as TMP_Text;
            if (existingTime != null) Object.DestroyImmediate(existingTime.gameObject);
            Assign(ui, ("timeText", CreateTimeText(ui.transform)));

            BuildPopups(ui, gameManager);
            RestyleHud(ui); // the clock was recreated above
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
            Debug.Log("Roller Splat: popups rebuilt with the GUI kit and the scene saved.");
        }

        /// <summary>Restyles the open Game scene's HUD buttons and labels like the GUI kit, in place.</summary>
        [MenuItem("Tools/Roller Splat/Restyle HUD")]
        public static void RestyleHudMenu()
        {
            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogError("Roller Splat: open the Game scene (needs a UIManager) first.");
                return;
            }
            RestyleHud(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
            Debug.Log("Roller Splat: HUD restyled with the GUI kit and the scene saved.");
        }

        /// <summary>
        /// Buttons on the HUD canvas become kit buttons (green); a label sitting in its own box gets a kit
        /// button frame (level: purple, progress: sky). Plain labels and the clock just take the kit font.
        /// </summary>
        static void RestyleHud(UIManager ui)
        {
            if (!KitInstalled) return;
            foreach (var button in ui.GetComponentsInChildren<Button>(true))
            {
                var image = button.GetComponent<Image>();
                // Buttons that already use kit art (e.g. the settings gear) keep their look.
                if (image != null && image.sprite != null && AssetDatabase.GetAssetPath(image.sprite).StartsWith(KitRoot)) continue;
                ApplyKitButtonStyle(image, "Green");
            }

            var so = new SerializedObject(ui);
            foreach (var (field, color) in new[] { ("levelText", "Purple"), ("progressText", "Sky"), ("timeText", null) })
            {
                var text = so.FindProperty(field).objectReferenceValue as TMP_Text;
                if (text == null) continue;
                var box = text.transform.parent != null && text.transform.parent != ui.transform
                    ? text.transform.parent.GetComponent<Image>() : null;
                if (box != null && color != null && box.GetComponent<Button>() == null)
                {
                    ApplyKitButtonStyle(box, color);
                    PinToTopCorner(box.rectTransform, ui.GetComponent<CanvasScaler>());
                    continue;
                }
                var font = KitFont("Sen_Line_s_Black SDF");
                if (font == null) continue;
                Undo.RecordObject(text, "Restyle HUD");
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
        }

        /// <summary>
        /// A centre-anchored HUD box drifts under the progress bar on tall screens; re-anchor it to the
        /// nearest top corner, keeping its place at the reference resolution.
        /// </summary>
        static void PinToTopCorner(RectTransform rt, CanvasScaler scaler)
        {
            if (scaler == null || rt.anchorMin != new Vector2(0.5f, 0.5f) || rt.anchorMax != rt.anchorMin) return;
            var size = scaler.referenceResolution;
            var anchor = new Vector2(rt.anchoredPosition.x < 0f ? 0f : 1f, 1f);
            Undo.RecordObject(rt, "Restyle HUD");
            var pos = rt.anchoredPosition + new Vector2((0.5f - anchor.x) * size.x, (0.5f - anchor.y) * size.y);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
        }

        // ---------- Assets ----------

        static Sprite CreateSprite(string name, int size, bool circle)
        {
            string path = $"{SpritesDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    if (circle)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                        a = Mathf.Clamp01(r - d); // 1px anti-aliased edge
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 1 world unit = 1 cell
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// White square with a darker grid-line border. The tile tint (floor / paint colour) multiplies it,
        /// so every cell reads as its own square with a slightly darker outline of the same colour.
        /// </summary>
        public static Sprite CreateCellSprite(int size = 128, int border = 2, float lineShade = 0.6f)
        {
            string path = $"{SpritesDir}/FloorCell.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var line = new Color(lineShade, lineShade, lineShade, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = x < border || y < border || x >= size - border || y >= size - border;
                tex.SetPixel(x, y, edge ? line : Color.white);
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // 1 world unit = 1 cell
            importer.filterMode = FilterMode.Point; // keep the grid lines crisp
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Tile CreateTile(string name, Sprite sprite, Color color)
        {
            string path = $"{TilesDir}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static string CreateLevelPrefab(string name, string[] map, Tile floorTile, Tile wallTile)
        {
            var root = new GameObject(name);
            var gridGo = new GameObject("Grid", typeof(Grid));
            gridGo.transform.SetParent(root.transform, false);
            var floor = CreateTilemap("Floor", gridGo.transform, 0);
            var walls = CreateTilemap("Walls", gridGo.transform, 1);
            var start = new GameObject("StartPoint").transform;
            start.SetParent(root.transform, false);

            int rows = map.Length;
            for (int r = 0; r < rows; r++)
            for (int x = 0; x < map[r].Length; x++)
            {
                var cell = new Vector3Int(x, rows - 1 - r, 0);
                switch (map[r][x])
                {
                    case '#':
                        walls.SetTile(cell, wallTile);
                        break;
                    case 'S':
                        floor.SetTile(cell, floorTile);
                        start.localPosition = floor.GetCellCenterLocal(cell);
                        break;
                    case '.':
                        floor.SetTile(cell, floorTile);
                        break;
                }
            }

            var level = root.AddComponent<Level>();
            level.Init(floor, walls, start);

            string path = $"{LevelsDir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
        }

        static Tilemap CreateTilemap(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        // ---------- Scene ----------

        static void BuildScene(string[] levelPaths, Sprite square, Sprite circle)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            var input = new GameObject("Input").AddComponent<SwipeInput>();
            var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            var grid = new GameObject("GridManager").AddComponent<GridManager>();
            grid.gameObject.AddComponent<PaintCellEffects>();
            var loader = new GameObject("LevelLoader").AddComponent<LevelLoader>();

            var ballGo = new GameObject("Ball");
            ballGo.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            var ballSprite = ballGo.AddComponent<SpriteRenderer>();
            ballSprite.sprite = circle;
            ballSprite.color = BallColor;
            ballSprite.sortingOrder = 10;
            var ball = ballGo.AddComponent<BallController>();
            Assign(ballGo.AddComponent<BallEffects>(), ("splashSprite", circle));

            var ui = BuildUI(square, gameManager);

            Assign(ball, ("input", input), ("grid", grid));
            Assign(loader, ("grid", grid), ("ball", ball), ("targetCamera", cam));
            Assign(new GameObject("BoardVisuals").AddComponent<BoardVisuals>(),
                ("loader", loader), ("grid", grid), ("targetCamera", cam));
            var loaderSo = new SerializedObject(loader);
            var levelsProp = loaderSo.FindProperty("levels");
            levelsProp.arraySize = levelPaths.Length;
            // Load after NewScene: opening a scene unloads the prefab objects created earlier.
            for (int i = 0; i < levelPaths.Length; i++)
                levelsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(levelPaths[i]).GetComponent<Level>();
            loaderSo.ApplyModifiedPropertiesWithoutUndo();
            Assign(gameManager, ("grid", grid), ("loader", loader), ("ball", ball), ("ui", ui));
            Assign(gameManager.gameObject.AddComponent<WinFireworks>(),
                ("loader", loader), ("targetCamera", cam), ("sparkSprite", circle));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AddSceneToBuildSettings();
        }

        static UIManager BuildUI(Sprite square, GameManager gameManager)
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGo.transform;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var levelText = CreateText("LevelText", canvas, "Level 1", 64, TextAlignmentOptions.Left,
                new Vector2(0, 1), new Vector2(40, -40), new Vector2(500, 100));
            var progressText = CreateText("ProgressText", canvas, "0%", 64, TextAlignmentOptions.Right,
                new Vector2(1, 1), new Vector2(-40, -40), new Vector2(300, 100));

            var progressBg = CreateImage("ProgressBg", canvas, square, new Color(1f, 1f, 1f, 0.15f));
            Place(progressBg.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(1000, 24));
            var progressFill = CreateImage("ProgressFill", progressBg.transform, square, BallColor);
            Stretch(progressFill.rectTransform);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount = 0f;

            var restart = CreateButton("RestartButton", canvas, square, "Restart",
                new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(400, 120));
            UnityEventTools.AddPersistentListener(restart.onClick, gameManager.OnRestartPressed);

            var timeText = CreateTimeText(canvas);

            var ui = canvasGo.AddComponent<UIManager>();
            Assign(ui,
                ("levelText", levelText), ("progressText", progressText), ("progressFill", progressFill),
                ("timeText", timeText));
            BuildPopups(ui, gameManager);
            RestyleHud(ui);
            return ui;
        }

        /// <summary>HUD clock, centred just under the progress bar.</summary>
        static TMP_Text CreateTimeText(Transform canvas)
        {
            var timeText = CreateText("TimeText", canvas, "1:00", 72, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1), new Vector2(0, -200), new Vector2(300, 100));
            timeText.fontStyle = FontStyles.Bold;
            return timeText;
        }

        /// <summary>Win / lose popups from the GUI kit, on their own canvas above the HUD.</summary>
        static void BuildPopups(UIManager ui, GameManager gameManager)
        {
            if (!KitInstalled)
            {
                Debug.LogWarning($"Roller Splat: GUI kit not found at {KitRoot}; the scene has no win/lose popups.");
                return;
            }
            var canvas = CreateKitCanvas("PopupCanvas", 10);

            // Win: CLEAR! + three stars + "LEVEL N" + Next. The kit's rewards / EXP bar / ad button are unused.
            var win = InstantiateKitPanel("PopupDim_Play_StageClear", canvas);
            Hide(win, "Slider_Level01_l_Blue", "ItemFrame02_Basic", "Button01_AdClaim");
            // The kit lays this out for tall screens; pull it together so it also fits 16:9.
            foreach (var (path, y) in new[]
                     {
                         ("Image_Text_Clear", 620f), ("Group_Stars", 330f), ("Group_ImageEffect", 340f),
                         ("Title_Line01", 40f), ("Button01_Claim", -200f),
                     })
            {
                var rt = Child<RectTransform>(win, path);
                if (rt != null) rt.anchoredPosition = new Vector2(path == "Group_Stars" ? rt.anchoredPosition.x : 0f, y);
            }
            var winText = Child<TMP_Text>(win, "Title_Line01/Text_Title");
            if (winText != null) winText.text = "LEVEL 1";
            var next = Child<RectTransform>(win, "Button01_Claim");
            MakeKitButton(next, "Next", gameManager.OnNextPressed);
            win.gameObject.SetActive(false);

            var so = new SerializedObject(ui);
            var starsProp = so.FindProperty("stars");
            string[] starPaths = { "Group_Stars/Icon_Star (1)", "Group_Stars/Icon_Star", "Group_Stars/Icon_Star (2)" };
            starsProp.arraySize = starPaths.Length;
            for (int i = 0; i < starPaths.Length; i++)
                starsProp.GetArrayElementAtIndex(i).objectReferenceValue = Child<Image>(win, starPaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Lose: DEFEAT ribbon + "TIME'S UP!" + Retry.
            var lose = InstantiateKitPanel("PopupDim_Play_Result_Defeat", canvas);
            Hide(lose, "ItemFrame02_Basic");
            var reason = Child<TMP_Text>(lose, "Title_Line01/Text_Title");
            if (reason != null) reason.text = "TIME'S UP!";
            var retry = Child<RectTransform>(lose, "Button_124_Blue");
            if (retry != null) retry.anchoredPosition = new Vector2(0f, -250f);
            MakeKitButton(retry, "Retry", gameManager.OnRestartPressed);
            lose.gameObject.SetActive(false);

            Assign(ui,
                ("winPanel", win.gameObject), ("winText", winText),
                ("starOn", KitSprite("Demo/Demo_Image/Image_Star_On.png")),
                ("starOff", KitSprite("Demo/Demo_Image/Image_Star_Off.png")),
                ("losePanel", lose.gameObject));

            // Settings: gear button at the top of the HUD (between the level and progress boxes); pauses the game.
            var settings = BuildSettingsPopup(inGame: true);
            var gear = CreateSettingsButton(ui.transform, settings);
            if (gear != null) Place(gear, new Vector2(0.5f, 1f), new Vector2(0, -90), gear.sizeDelta);
            Assign(gameManager, ("settings", settings));
        }

        // ---------- UI helpers ----------

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size,
            TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 rectSize)
        {
            var rt = CreateRect(name, parent);
            Place(rt, anchor, pos, rectSize);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var img = CreateRect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        static Button CreateButton(string name, Transform parent, Sprite sprite, string label,
            Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = CreateImage(name, parent, sprite, BallColor);
            Place(img.rectTransform, anchor, pos, size);
            var button = img.gameObject.AddComponent<Button>();
            var text = CreateText("Text", img.transform, label, 56, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            text.color = BackgroundColor;
            return button;
        }

        // ---------- Wiring ----------

        static void Assign(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"Roller Splat builder: field '{field}' not found on {target.GetType().Name}.");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puts the built scenes first, in play order: Loading, Menu, Game.</summary>
        static void AddSceneToBuildSettings()
        {
            var ordered = new[] { LoadingScenePath, MenuScenePath, ScenePath };
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => System.Array.IndexOf(ordered, s.path) >= 0);
            int index = 0;
            foreach (var path in ordered)
                if (File.Exists(path)) scenes.Insert(index++, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
