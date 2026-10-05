using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RollerSplat.EditorTools
{
    /// <summary>
    /// Tools → Roller Splat → Build Menu Scene: title, "LEVEL N", PLAY and a settings button, with the
    /// kit's Settings popup (music / SFX volume, reset progress). The same popup is added to the Game scene.
    /// </summary>
    public static partial class RollerSplatSceneBuilder
    {
        const string MenuScenePath = Root + "/Scenes/Menu.unity";
        const string KitButtons = KitRoot + "/Prefabs/Prefabs_Component_Buttons";

        [MenuItem("Tools/Roller Splat/Build Menu Scene")]
        public static void BuildMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(MenuScenePath) && !EditorUtility.DisplayDialog("Roller Splat",
                    $"{MenuScenePath} đã tồn tại. Tạo lại?", "Ghi đè", "Huỷ"))
                return;
            BuildMenuSceneNoPrompt();
        }

        /// <summary>Same as the menu item but without any dialog (overwrites; unsaved scene changes are lost).</summary>
        public static void BuildMenuSceneNoPrompt()
        {
            if (!KitInstalled)
            {
                Debug.LogError($"Roller Splat: GUI kit not found at {KitRoot}.");
                return;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvas = CreateKitCanvas("Canvas", 0);
            var background = CreateImage("Background", canvas, KitSprite("Demo/Demo_Background/Background_Sample02.png"), Color.white);
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            var title = CreateGameTitle(canvas);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(980, 520));

            var levelText = CreateText("LevelText", canvas, "LEVEL 1", 80, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(700, 120));
            var font = KitFont("Sen_Line_s_Black SDF");
            if (font != null) levelText.font = font;

            var menu = canvas.gameObject.AddComponent<MainMenu>();

            var play = CloneKitElement($"{KitPanels}/Lobby.prefab", "Middle/Button_Play", canvas);
            if (play != null)
            {
                Place(play, new Vector2(0.5f, 0.5f), new Vector2(0, -420), play.sizeDelta);
                MakeKitButton(play, "PLAY", menu.OnPlayPressed);
            }

            var settings = BuildSettingsPopup(inGame: false);
            var gear = CreateSettingsButton(canvas, settings);
            if (gear != null) Place(gear, new Vector2(1f, 1f), new Vector2(-110, -110), gear.sizeDelta);

            Assign(menu, ("levelText", levelText), ("settings", settings));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"Roller Splat: built {MenuScenePath}. Loading now opens the menu.");
        }

        /// <summary>
        /// The kit's Settings panel trimmed to music / SFX and a Reset Progress button (plus Home in game),
        /// on its own canvas above everything else, with a confirmation popup for the reset.
        /// </summary>
        static SettingsPanel BuildSettingsPopup(bool inGame)
        {
            var canvas = CreateKitCanvas("SettingsCanvas", 20);
            var settings = canvas.gameObject.AddComponent<SettingsPanel>();

            var panel = InstantiateKitPanel("Settings", canvas);
            const string list = "Popup08_Topbar_Divided/Middle/Group_List";
            const string frame = "Popup08_Topbar_Divided";
            Hide(panel, $"{list}/Language", $"{list}/Notification", $"{list}/Screenshake",
                $"{frame}/Button_Help", $"{frame}/Button_Privacy", $"{frame}/Button_TermsOfService",
                $"{frame}/Button_DeleteAccount");

            // The kit frame is sized for five rows; shrink it to fit two. Middle stretches with the frame
            // (521 px of top bar + bottom area), and the list must shrink with it or it overflows upwards.
            var frameRect = Child<RectTransform>(panel, frame);
            if (frameRect != null) frameRect.sizeDelta = new Vector2(frameRect.sizeDelta.x, 830f);
            var listRect = Child<RectTransform>(panel, list);
            if (listRect != null) listRect.sizeDelta = new Vector2(listRect.sizeDelta.x, 230f);
            var close = Child<RectTransform>(panel, "Button_Close03");
            if (close != null) Place(close, new Vector2(0.5f, 0.5f), new Vector2(0, -510), close.sizeDelta);

            var music = Child<Slider>(panel, $"{list}/Music/Slider_Handle_Pink");
            var sfx = Child<Slider>(panel, $"{list}/SFX/Slider_Handle_Pink");
            foreach (var slider in new[] { music, sfx })
            {
                if (slider == null) continue;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.value = GameSettings.DefaultVolume;
            }

            var buttonParent = Child(panel, frame);
            var reset = CreateKitTextButton("Button_ResetProgress", "Red", buttonParent, "Reset Progress", settings.OnResetPressed);
            if (reset != null) PlaceBottom(reset, inGame ? -218f : 0f);
            if (inGame)
            {
                var home = CreateKitTextButton("Button_Home", "Blue", buttonParent, "Home", settings.OnHomePressed);
                if (home != null) PlaceBottom(home, 215f);
            }
            MakeKitButton(close, null, settings.Close);

            // Confirmation: the kit's warning popup with Yes / No added.
            var confirm = InstantiateKitPanel("PopupDim_Warning", canvas);
            var info = Child<TMP_Text>(confirm, "Text_Info");
            if (info != null) info.text = "Reset progress?\n<color=#b8b9d7><size=70%>You will start again from Level 1</size></color>";
            var yes = CreateKitTextButton("Button_Yes", "Red", confirm, "Reset", settings.ConfirmReset);
            if (yes != null) Place(yes, new Vector2(0.5f, 0.5f), new Vector2(200, -330), new Vector2(330, 124));
            var no = CreateKitTextButton("Button_No", "Gray", confirm, "Cancel", settings.CancelReset);
            if (no != null) Place(no, new Vector2(0.5f, 0.5f), new Vector2(-200, -330), new Vector2(330, 124));

            var so = new SerializedObject(settings);
            so.FindProperty("pauseGame").boolValue = inGame;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assign(settings, ("panel", panel.gameObject), ("musicSlider", music), ("sfxSlider", sfx),
                ("confirmPopup", confirm.gameObject));
            // SettingsPanel hides these in Awake; keep them hidden in the editor too.
            panel.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);
            return settings;
        }

        static void PlaceBottom(RectTransform rt, float x)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 210f);
            rt.sizeDelta = new Vector2(380f, 124f);
        }

        /// <summary>The kit's round dark button with a gear icon, opening <paramref name="settings"/>.</summary>
        static RectTransform CreateSettingsButton(Transform parent, SettingsPanel settings)
        {
            var gear = CloneKitElement($"{KitPanels}/Lobby.prefab", "Topbar/Button_Menu", parent);
            if (gear == null) return null;
            gear.name = "SettingsButton";
            Hide(gear, "Alert_Dot_Red");
            var icon = Child<Image>(gear, "Icon");
            var gearSprite = KitSprite("Demo/Demo_Icon/Icon_Setting.png");
            if (icon != null && gearSprite != null)
            {
                icon.sprite = gearSprite;
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(80f, 80f);
            }
            MakeKitButton(gear, null, settings.Open);
            return gear;
        }

        /// <summary>An instance of the kit's small text button <c>Button01_s_BtnText_{color}</c>.</summary>
        static RectTransform CreateKitTextButton(string name, string color, Transform parent, string label,
            UnityEngine.Events.UnityAction onClick)
        {
            if (parent == null) return null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitButtons}/Button01_s_BtnText_{color}.prefab");
            if (prefab == null)
            {
                Debug.LogError($"Roller Splat builder: kit button '{color}' not found.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var rt = (RectTransform)go.transform;
            MakeKitButton(rt, label, onClick);
            return rt;
        }

        /// <summary>Copies one element out of a kit panel prefab (fully unpacked) into <paramref name="parent"/>.</summary>
        static RectTransform CloneKitElement(string prefabPath, string childPath, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Roller Splat builder: kit prefab not found at {prefabPath}.");
                return null;
            }
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(temp, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var child = temp.transform.Find(childPath);
            if (child != null) child.SetParent(parent, false);
            else Debug.LogError($"Roller Splat builder: '{childPath}' not found in {prefabPath}.");
            Object.DestroyImmediate(temp);
            return child as RectTransform;
        }

        /// <summary>The game title in the kit's outlined font (shared by the loading and menu screens).</summary>
        static TextMeshProUGUI CreateGameTitle(Transform parent)
        {
            var title = CreateText("Title", parent, GameTitle, 190, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 520));
            var titleFont = KitFont("Cairo_Line_Navy SDF");
            if (titleFont != null) title.font = titleFont;
            title.lineSpacing = -75f; // Cairo has a tall line height
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 80f;
            title.fontSizeMax = 190f;
            return title;
        }
    }
}
