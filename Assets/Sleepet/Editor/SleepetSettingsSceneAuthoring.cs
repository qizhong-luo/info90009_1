#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sleepet.Editor
{
    // Permanent authoring tool. Controls are saved in the Me scene and never created at runtime.
    public static class SleepetSettingsSceneAuthoring
    {
        const string Art = "Assets/Sleepet/Art/Figma/";
        static Sprite panel, small, circle;
        static readonly Color Ink = new Color(0.031f, 0.169f, 0.357f);
        static readonly Color Card = new Color(0.906f, 0.953f, 0.988f);
        [MenuItem("Sleepet/Apply Web Settings Reference")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_Me.unity", OpenSceneMode.Single);
            var ui = Object.FindFirstObjectByType<SleepetHighFi>();
            ApplyTo(ui);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Web Setting layout, draggable dial and profile editor saved to Sleepet_Me.");
        }
        static Sprite Sprite(string file)
        {
            var path = Art + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool wholeImage = file == "ui-circle.png" || file == "setting-web-dial-base.png" || file == "setting-bedtime-handle.png" || file == "setting-wake-handle.png";
            if (importer.textureType != TextureImporterType.Sprite || importer.textureCompression != TextureImporterCompression.Uncompressed || (wholeImage && importer.spriteImportMode != SpriteImportMode.Single))
            { if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; }
              if (wholeImage) importer.spriteImportMode = SpriteImportMode.Single;
              importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Transform Find(Transform root, string name) { return root.GetComponentsInChildren<Transform>(true).First(t => t.name == name); }
        static T Component<T>(GameObject go) where T : Component { return go.GetComponent<T>() ?? go.AddComponent<T>(); }
        static RectTransform Box(Transform t, float x, float y, float w, float h)
        {
            var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); r.localScale = Vector3.one; return r;
        }
        static GameObject Child(Transform parent, string name)
        {
            var t = parent.Find(name); if (t) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go;
        }
        static Image Image(Transform parent, string name, float x, float y, float w, float h, Sprite sprite, Color color)
        {
            var go = Child(parent, name); Box(go.transform, x, y, w, h);
            var image = Component<Image>(go); image.sprite = sprite; image.color = color;
            image.type = sprite == panel || sprite == small ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = false; return image;
        }
        static Text Label(Transform parent, string name, float x, float y, float w, float h, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = Child(parent, name); Box(go.transform, x, y, w, h);
            var label = Component<Text>(go); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = size; label.color = color; label.alignment = align;
            label.raycastTarget = false; return label;
        }
        static Button Button(Transform parent, string name, float x, float y, float w, float h, string text, UnityAction action, bool pill = false)
        {
            var image = Image(parent, name, x, y, w, h, pill ? panel : null, pill ? new Color(0.831f, 0.906f, 0.929f) : Color.clear);
            image.raycastTarget = true;
            var button = Component<Button>(image.gameObject); button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick, action);
            if (!string.IsNullOrEmpty(text)) Label(image.transform, "Label", 0, 0, w, h, text, pill ? 15 : 23, Ink, TextAnchor.MiddleCenter);
            return button;
        }
        static void Border(Image image, float width)
        {
            var outline = Component<Outline>(image.gameObject); outline.effectColor = new Color(1, 1, 1, 0.82f);
            outline.effectDistance = new Vector2(width, -width);
        }
        static void Hide(Transform root, string name) { var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name); if (t) t.gameObject.SetActive(false); }
        public static void ApplyTo(SleepetHighFi ui)
        {
            panel = Sprite("rounded-panel-22.png"); small = Sprite("rounded-panel-5.png");
            circle = CircleSprite();
            var page = ui.pages[(int)HighFiPage.Me].transform;
            var content = Find(page, "Editable Me content");
            Hide(content, "Save preferences");
            var scroll = page.GetComponentInChildren<ScrollRect>(true);
            Box(content, 0, 0, 402, 1032);
            // Web scroll area starts at y=62; Unity reserves 60 points for the native safe area.
            Box(Find(content, "Name"), 103, 7, 244, 31);
            Box(Find(content, "Email"), 103, 38, 244, 24);
            Box(Find(content, "My Plan"), 23, 86, 356, 30);
            var plan = Find(content, "Plan card"); Box(plan, 23, 118, 356, 360); Border(plan.GetComponent<Image>(), 1);
            var planImage = plan.GetComponent<Image>(); planImage.sprite = panel; planImage.color = Card;
            var bedTitle = plan.GetComponentsInChildren<Text>(true).First(t => t.text == "BEDTIME");
            Box(bedTitle.transform, 15, 10, 78, 17); bedTitle.alignment = TextAnchor.MiddleCenter;
            Box(ui.meBedtime.transform, 15, 28, 78, 24); ui.meBedtime.alignment = TextAnchor.MiddleCenter; ui.meBedtime.fontSize = 20;
            Box(Find(plan, "WIND-DOWN"), 136, 10, 86, 17); Find(plan, "WIND-DOWN").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            Box(ui.meWinddown.transform, 136, 28, 86, 24); ui.meWinddown.alignment = TextAnchor.MiddleCenter; ui.meWinddown.fontSize = 20;
            Box(Find(plan, "WAKE UP"), 262, 10, 78, 17); Find(plan, "WAKE UP").GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            Box(ui.meWake.transform, 262, 28, 78, 24); ui.meWake.alignment = TextAnchor.MiddleCenter; ui.meWake.fontSize = 20;
            Box(Find(plan, "Edit plan"), 0, 0, 356, 61);
            var dialBase = Find(plan, "Plan dial").GetComponent<Image>();
            Box(dialBase.transform, 34, 61, 288, 288); dialBase.sprite = Sprite("setting-web-dial-base.png"); dialBase.color = Color.white;
            dialBase.raycastTarget = false; Hide(dialBase.transform, "Dial marks");
            var dialGo = Child(plan, "Interactive schedule arc"); Box(dialGo.transform, 34, 61, 288, 288);
            Component<CanvasRenderer>(dialGo);
            var dial = Component<SleepetScheduleDial>(dialGo); ui.scheduleDial = dial; dial.app = ui; dial.raycastTarget = false;
            dial.bedtimeHandle = Handle(dial, true, scroll, out var bedTip); dial.bedtimeTooltip = bedTip;
            dial.wakeHandle = Handle(dial, false, scroll, out var wakeTip); dial.wakeTooltip = wakeTip;
            dial.Refresh("20:10", "09:52");
            Box(Find(content, "My Sleepet"), 23, 488, 356, 30);
            var petCircle = Find(content, "Pet avatar circle").GetComponent<Image>(); Box(petCircle.transform, 23, 520, 70, 70);
            petCircle.sprite = circle; petCircle.color = new Color(0.761f, 0.882f, 0.976f);
            Box(ui.mePet.transform, 0, 0, 70, 70); ui.mePet.preserveAspect = true;
            Box(ui.mePetName.transform, 103, 520, 230, 27);
            Box(Find(content, "Change Pet"), 103, 554, 106, 30);
            Box(Find(content, "Preferences"), 23, 600, 356, 30);
            var pref = Find(content, "Preferences card"); Box(pref, 23, 632, 356, 113); pref.GetComponent<Image>().color = Card;
            Box(Find(pref, "Sound"), 14, 10, 137, 30); Box(ui.meSound.transform, 190, 10, 118, 30);
            Box(Find(pref, "Volume"), 14, 41.5f, 130, 30); Box(ui.meVolumePercent.transform, 144, 41.5f, 49, 30);
            Box(Find(pref, "Mode"), 14, 73, 137, 30); Box(ui.meMode.transform, 190, 73, 118, 30);
            Box(Find(pref, "divider 1"), 14, 40, 328, 1.5f); Box(Find(pref, "divider 2"), 14, 71.5f, 328, 1.5f);
            Hide(pref, "Change sound"); Hide(pref, "Change mode"); Hide(pref, "Mode previous indicator"); Hide(pref, "Mode next indicator");
            Button(pref, "Previous sound", 155, 7, 32, 36, "‹", ui.PreviousSound);
            Button(pref, "Next sound", 313, 7, 32, 36, "›", ui.CycleSound);
            Button(pref, "Previous mode", 155, 70, 32, 36, "‹", ui.PreviousMode);
            Button(pref, "Next mode", 313, 70, 32, 36, "›", ui.CycleMode);
            Box(ui.meVolume.transform, 201, 43, 141, 28);
            var sliderBg = Find(ui.meVolume.transform, "Background").GetComponent<Image>(); sliderBg.sprite = panel; sliderBg.type = UnityEngine.UI.Image.Type.Sliced; sliderBg.color = new Color(.82f, .87f, .90f);
            var fill = Find(ui.meVolume.transform, "Fill").GetComponent<Image>(); fill.color = new Color(.012f, .271f, .475f);
            var sliderHandle = Find(ui.meVolume.transform, "Handle").GetComponent<Image>(); sliderHandle.sprite = circle;
            sliderHandle.rectTransform.sizeDelta = new Vector2(16, 0);
            sliderHandle.rectTransform.anchorMin = new Vector2(.5f, 0);
            sliderHandle.rectTransform.anchorMax = new Vector2(.5f, 1);
            sliderHandle.rectTransform.anchoredPosition = Vector2.zero;
            var handleArea = (RectTransform)sliderHandle.transform.parent;
            handleArea.anchorMin = new Vector2(0, .5f); handleArea.anchorMax = new Vector2(1, .5f);
            handleArea.anchoredPosition = Vector2.zero; handleArea.sizeDelta = new Vector2(-16, 16);
            var backgroundRect = sliderBg.rectTransform;
            backgroundRect.anchorMin = new Vector2(0, .5f); backgroundRect.anchorMax = new Vector2(1, .5f);
            backgroundRect.anchoredPosition = Vector2.zero; backgroundRect.sizeDelta = new Vector2(0, 6);
            var fillArea = (RectTransform)fill.transform.parent;
            fillArea.anchorMin = new Vector2(0, .5f); fillArea.anchorMax = new Vector2(1, .5f);
            fillArea.anchoredPosition = Vector2.zero; fillArea.sizeDelta = new Vector2(-16, 6);
            fill.rectTransform.sizeDelta = Vector2.zero;
            // Group support items in one rounded card, as on the reference webpage.
            Box(Find(content, "Setting & Support"), 23, 756, 356, 30);
            var support = Image(content, "Web support card", 23, 788, 356, 176, panel, Card);
            string[] rows = { "Privacy & data", "Accessibility", "Connected Devices", "Notifications", "Help & Q&A" };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Find(content, "Support " + rows[i]); row.SetParent(support.transform, false); Box(row, 14, 10 + i * 31.5f, 328, 30);
                row.GetComponent<Image>().color = Color.clear;
                var label = row.GetComponentsInChildren<Text>().First();
                label.fontSize = 15; label.color = Ink; label.alignment = TextAnchor.MiddleLeft; label.text = rows[i]; Box(label.transform, 0, 0, 294, 30);
                Label(row, "Chevron", 298, 0, 30, 30, "›", 20, Ink, TextAnchor.MiddleRight);
                if (i < 4) Image(support.transform, "Divider " + i, 14, 40 + i * 31.5f, 328, 1.5f, null, new Color(0, .106f, .29f, .44f));
            }
            Profile(ui, content);
            EditorUtility.SetDirty(ui);
        }
        static Sprite CircleSprite()
        {
            const string path = Art + "ui-circle.png";
            if (!System.IO.File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(63.5f - Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)))));
                texture.Apply(); System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            return Sprite("ui-circle.png");
        }
        static RectTransform Handle(SleepetScheduleDial dial, bool bed, ScrollRect scroll, out Text tip)
        {
            string name = bed ? "Bedtime handle" : "Wake handle";
            var hit = Image(dial.transform, name, 0, 0, 44, 44, null, Color.clear); hit.raycastTarget = true;
            var rect = hit.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            Image(hit.transform, "Handle circle", 8, 8, 28, 28, circle, bed ? new Color(.012f, .271f, .475f) : new Color(0, .541f, .733f));
            Image(hit.transform, "Handle icon", 14.5f, 14.5f, 15, 15, Sprite(bed ? "setting-bedtime-handle.png" : "setting-wake-handle.png"), Color.white);
            var bubble = Image(hit.transform, "Time tooltip", -13, -28, 70, 24, panel, new Color(.831f, .906f, .929f));
            tip = Label(bubble.transform, "Time", 0, 0, 70, 24, "", 12, Ink, TextAnchor.MiddleCenter); bubble.gameObject.SetActive(false);
            var drag = Component<SleepetScheduleHandle>(hit.gameObject); drag.dial = dial; drag.bedtime = bed; drag.tooltip = bubble.gameObject; drag.scroll = scroll;
            return rect;
        }
        static void Profile(SleepetHighFi ui, Transform content)
        {
            var editor = Component<SleepetProfileEditor>(ui.gameObject); ui.profileEditor = editor; editor.app = ui;
            editor.presets = ui.petOptions.Concat(new[] { Sprite("setting-profile-figma.png") }).ToArray();
            editor.displayName = Find(content, "Name").GetComponent<Text>();
            var backing = Image(content, "Profile avatar backing", 23, 4, 70, 70, circle, new Color(.761f, .882f, .976f));
            Component<Mask>(backing.gameObject).showMaskGraphic = true;
            var avatar = Find(content, "Profile art").GetComponent<Image>(); avatar.transform.SetParent(backing.transform, false);
            Box(avatar.transform, 0, 0, 70, 70); avatar.sprite = editor.presets[0]; avatar.preserveAspect = true; avatar.raycastTarget = false; editor.avatar = avatar;
            Button(content, "Edit profile avatar", 23, 4, 70, 70, "", editor.Open);
            Button(content, "Edit profile name", 103, 4, 244, 36, "", editor.Open);
            var sheet = Child(ui.transform, "Profile editor sheet"); Box(sheet.transform, 0, 0, 402, 874); editor.sheet = sheet;
            Button(sheet.transform, "Dismiss profile editor", 0, 0, 402, 874, "", editor.Cancel).GetComponent<Image>().color = new Color(0, 0, 0, .3f);
            var body = Image(sheet.transform, "Profile editor panel", 0, 434, 402, 440, panel, new Color(.012f, .271f, .475f)); body.raycastTarget = true;
            Label(body.transform, "Title", 25, 17, 352, 32, "Edit profile", 22, Color.white, TextAnchor.MiddleCenter);
            var previewCircle = Image(body.transform, "Avatar preview circle", 165, 60, 72, 72, circle, new Color(.761f, .882f, .976f)); Component<Mask>(previewCircle.gameObject);
            editor.preview = Image(previewCircle.transform, "Avatar preview", 0, 0, 72, 72, editor.presets[0], Color.white); editor.preview.preserveAspect = true;
            Label(body.transform, "Preset label", 25, 140, 352, 24, "Choose a preset picture", 15, Color.white, TextAnchor.MiddleCenter);
            editor.selectionBorders = new Image[editor.presets.Length];
            for (int i = 0; i < editor.presets.Length; i++)
            {
                var border = Image(body.transform, "Avatar selection " + i, 43 + i * 82, 174, 66, 66, circle, new Color(1, 1, 1, .15f)); editor.selectionBorders[i] = border;
                var choice = Image(border.transform, "Avatar preset " + i, 3, 3, 60, 60, circle, new Color(.761f, .882f, .976f)); choice.raycastTarget = true; Component<Mask>(choice.gameObject);
                var art = Image(choice.transform, "Preset art", 0, 0, 60, 60, editor.presets[i], Color.white); art.preserveAspect = true;
                var button = Component<Button>(choice.gameObject); button.targetGraphic = choice; button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(button.onClick, editor.Select, i);
            }
            Label(body.transform, "Name label", 30, 258, 342, 24, "Name · 1–10 English letters", 15, Color.white);
            var field = Image(body.transform, "Profile name input", 30, 289, 342, 44, small, Card); field.raycastTarget = true;
            var input = Component<InputField>(field.gameObject); editor.nameInput = input; input.targetGraphic = field;
            input.textComponent = Label(field.transform, "Value", 14, 3, 314, 38, "Sally", 20, Ink);
            input.characterLimit = 10; input.lineType = InputField.LineType.SingleLine; input.contentType = InputField.ContentType.Standard;
            input.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            editor.error = Label(body.transform, "Validation message", 30, 334, 342, 24, "", 13, new Color(1, .82f, .76f));
            Button(body.transform, "Cancel profile", 30, 377, 140, 38, "Cancel", editor.Cancel, true);
            Button(body.transform, "Save profile", 232, 377, 140, 38, "Save", editor.Save, true);
            sheet.SetActive(false);
            // Notices must still render above the profile sheet when necessary.
            if (ui.noticeSheet) ui.noticeSheet.transform.SetAsLastSibling();
            EditorUtility.SetDirty(editor);
        }
    }
}
#endif
