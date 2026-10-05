using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RollerSplat.EditorTools
{
    /// <summary>Helpers for placing "GUI Pro - SuperCasual" (Layer Lab) prefabs into the generated scenes.</summary>
    public static partial class RollerSplatSceneBuilder
    {
        const string KitRoot = "Assets/Layer Lab/GUI Pro-SuperCasual";
        const string KitPanels = KitRoot + "/Prefabs/Prefabs_DemoScene_Panels";
        const string KitSprites = KitRoot + "/ResourcesData/Sprites";
        const string KitFonts = KitRoot + "/ResourcesData/Fonts";

        static bool KitInstalled => AssetDatabase.IsValidFolder(KitRoot);

        /// <summary>Canvas with the kit's own scaler settings, so its panels lay out as designed.</summary>
        static Transform CreateKitCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1048, 2048);
            scaler.matchWidthOrHeight = 0f;
            return go.transform;
        }

        /// <summary>Instantiates a kit panel as a prefab instance stretched over <paramref name="parent"/>.</summary>
        static RectTransform InstantiateKitPanel(string prefabName, Transform parent)
        {
            string path = $"{KitPanels}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Roller Splat builder: kit prefab not found at {path}.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            // The kit's demo panel switcher throws on enable/disable when its 'otherPanels' list is unset.
            // (Matched by name: the kit's scripts compile into Assembly-CSharp, which this assembly can't reference.)
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && behaviour.GetType().Name == "PanelSuperCasual")
                    Object.DestroyImmediate(behaviour);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        /// <summary>Finds a descendant by slash-separated path, logging when the kit layout changed.</summary>
        static Transform Child(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null) Debug.LogError($"Roller Splat builder: '{root.name}/{path}' not found in kit prefab.");
            return t;
        }

        static T Child<T>(Transform root, string path) where T : Component
        {
            var t = Child(root, path);
            return t != null ? t.GetComponent<T>() : null;
        }

        static void Hide(Transform root, params string[] paths)
        {
            foreach (var p in paths)
            {
                var t = Child(root, p);
                if (t != null) t.gameObject.SetActive(false);
            }
        }

        /// <summary>Kit buttons are plain images; this makes one clickable, relabels it and wires the click.</summary>
        static Button MakeKitButton(Transform buttonRoot, string label, UnityAction onClick)
        {
            if (buttonRoot == null) return null;
            var button = buttonRoot.GetComponent<Button>();
            if (button == null) button = buttonRoot.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRoot.GetComponent<Image>();
            var text = buttonRoot.GetComponentInChildren<TMP_Text>(true);
            if (text != null && label != null) text.text = label;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, onClick);
            return button;
        }

        /// <summary>
        /// Restyles an existing image (and its button / label, if any) like the kit's small text button
        /// <c>Button01_s_BtnText_{color}</c>. Position and size are kept, so hand-made layouts survive.
        /// </summary>
        static void ApplyKitButtonStyle(Image target, string color)
        {
            if (target == null) return;
            string path = $"{KitRoot}/Prefabs/Prefabs_Component_Buttons/Button01_s_BtnText_{color}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"Roller Splat builder: kit button not found at {path}.");
                return;
            }

            var src = prefab.GetComponent<Image>();
            Undo.RecordObject(target, "Restyle HUD");
            target.sprite = src.sprite;
            target.type = src.type;
            target.pixelsPerUnitMultiplier = src.pixelsPerUnitMultiplier;
            target.color = src.color;

            var srcButton = prefab.GetComponent<Button>();
            var button = target.GetComponent<Button>();
            if (button != null && srcButton != null)
            {
                Undo.RecordObject(button, "Restyle HUD");
                button.transition = srcButton.transition;
                button.colors = srcButton.colors;
                button.targetGraphic = target;
            }

            var srcText = prefab.GetComponentInChildren<TMP_Text>(true);
            var text = target.GetComponentInChildren<TMP_Text>(true);
            if (text != null && srcText != null) ApplyKitTextStyle(text, srcText, (RectTransform)srcText.transform);
        }

        static void ApplyKitTextStyle(TMP_Text text, TMP_Text src, RectTransform srcRect)
        {
            Undo.RecordObject(text, "Restyle HUD");
            Undo.RecordObject(text.rectTransform, "Restyle HUD");
            text.font = src.font;
            text.fontSharedMaterial = src.fontSharedMaterial;
            text.color = src.color;
            text.fontStyle = src.fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = src.fontSize;
            // Fill the button with the kit's padding (its art has a thicker bottom edge).
            var rt = text.rectTransform;
            rt.anchorMin = srcRect.anchorMin;
            rt.anchorMax = srcRect.anchorMax;
            rt.pivot = srcRect.pivot;
            rt.offsetMin = srcRect.offsetMin;
            rt.offsetMax = srcRect.offsetMax;
        }

        static Sprite KitSprite(string relativePath)
        {
            var sprite = FindSprite($"{KitSprites}/{relativePath}");
            if (sprite == null) Debug.LogError($"Roller Splat builder: kit sprite '{relativePath}' not found.");
            return sprite;
        }

        static TMP_FontAsset KitFont(string name) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{KitFonts}/{name}.asset");

        static Sprite FindSprite(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) return sprite;
            return null;
        }
    }
}
