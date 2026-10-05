using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RollerSplat.EditorTools
{
    /// <summary>
    /// Tools → Roller Splat → Build Loading Scene: the kit's Title_Loading screen with the game title,
    /// a ball rolling along the kit's progress bar, then the Game scene. Safe to run again.
    /// </summary>
    public static partial class RollerSplatSceneBuilder
    {
        const string LoadingScenePath = Root + "/Scenes/Loading.unity";
        const string GameTitle = "ROLLER\nSPLAT";

        static readonly Color PaintOrange = new Color32(0xFF, 0x8A, 0x2B, 0xFF);

        [MenuItem("Tools/Roller Splat/Build Loading Scene")]
        public static void BuildLoadingScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(LoadingScenePath) && !EditorUtility.DisplayDialog("Roller Splat",
                    $"{LoadingScenePath} đã tồn tại. Tạo lại?", "Ghi đè", "Huỷ"))
                return;
            BuildLoadingSceneNoPrompt();
        }

        /// <summary>Same as the menu item but without any dialog (overwrites; unsaved scene changes are lost).</summary>
        public static void BuildLoadingSceneNoPrompt()
        {
            if (!KitInstalled)
            {
                Debug.LogError($"Roller Splat: GUI kit not found at {KitRoot}.");
                return;
            }
            Directory.CreateDirectory(SpritesDir);
            var circle = CreateSprite("Circle", 128, circle: true); // reuses the PNG if it already exists
            var ballSprite = FindSprite($"{SpritesDir}/ball_red_large.png");
            if (ballSprite == null) ballSprite = circle;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var canvas = CreateKitCanvas("Canvas", 0);
            var panel = InstantiateKitPanel("Title_Loading", canvas);

            // The kit's sample art shows another game's characters; use its plain scenery instead.
            var background = Child<Image>(panel, "Background");
            var scenery = KitSprite("Demo/Demo_Background/Background_Sample02.png");
            if (background != null && scenery != null) background.sprite = scenery;

            // Replace the kit logo with the game title in the kit's outlined font, in the same spot.
            var logo = Child<RectTransform>(panel, "ImageTitle");
            var title = CreateGameTitle(panel);
            if (logo != null)
            {
                var rt = title.rectTransform;
                rt.anchorMin = logo.anchorMin;
                rt.anchorMax = logo.anchorMax;
                rt.pivot = logo.pivot;
                rt.anchoredPosition = logo.anchoredPosition;
                rt.SetSiblingIndex(logo.GetSiblingIndex() + 1);
                logo.gameObject.SetActive(false);
            }

            var spinner = Child<RectTransform>(panel, "Loading/Icon");

            var slider = Child<Slider>(panel, "Slider_Basic04_Orange");
            TMP_Text percent = null;
            RectTransform overlay = null;
            Image drop = null, ball = null;
            if (slider != null)
            {
                slider.interactable = false;
                slider.transition = Selectable.Transition.None;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 0f;
                percent = Child<TMP_Text>(slider.transform, "Text (TMP)");

                // Ball and drops ride on an unmasked overlay over the bar so the fill mask doesn't clip them.
                overlay = CreateRect("TrackOverlay", slider.transform);
                Stretch(overlay);
                overlay.offsetMin = new Vector2(12f, 0f);
                overlay.offsetMax = new Vector2(-12f, 0f);
                drop = CreateImage("Drop", overlay, circle, PaintOrange);
                Place(drop.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));
                drop.raycastTarget = false;
                ball = CreateImage("Ball", overlay, ballSprite, Color.white);
                ball.preserveAspect = true;
                ball.raycastTarget = false;
                Place(ball.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
                if (percent != null) percent.transform.SetAsLastSibling(); // keep the % readable over the ball
            }

            var version = Child<TMP_Text>(panel, "Text_Ver");
            if (version != null) version.text = $"Ver. {PlayerSettings.bundleVersion}";

            var screen = new GameObject("LoadingScreen").AddComponent<LoadingScreen>();
            Assign(screen, ("track", overlay), ("slider", slider), ("ball", ball != null ? ball.rectTransform : null),
                ("percentText", percent), ("spinner", spinner), ("dropTemplate", drop));
            // Boot into the menu when it exists, otherwise straight into the game.
            var screenSo = new SerializedObject(screen);
            screenSo.FindProperty("sceneToLoad").stringValue = File.Exists(MenuScenePath) ? "Menu" : "Game";
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), LoadingScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"Roller Splat: built {LoadingScenePath}. It is now the first scene in Build Settings.");
        }
    }
}
