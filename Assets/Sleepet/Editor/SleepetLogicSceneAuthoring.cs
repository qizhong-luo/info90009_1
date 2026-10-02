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
    // Authored scene maintenance, never a runtime UI builder.
    public static class SleepetLogicSceneAuthoring
    {
        static readonly Color Ink = new Color(.027f, .157f, .329f), Card = new Color(.906f, .953f, .988f), Navy = new Color(.012f, .271f, .475f);
        static Sprite panel;
        [MenuItem("Sleepet/Apply Weekly Figma Layout")]
        public static void ApplyWeekly()
        {
            panel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-22.png");
            var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_Weekly.unity");
            Weekly(Object.FindFirstObjectByType<SleepetHighFi>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Sleepet/Apply Audited Figma Interactions")]
        public static void Apply()
        {
            panel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-22.png");
            foreach (var name in new[] { "Home", "Sleep", "Me", "Routine", "Daily", "Weekly", "AR", "DayDetail" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_" + name + ".unity");
                var ui = Object.FindFirstObjectByType<SleepetHighFi>();
                foreach (var status in ui.GetComponentsInChildren<Transform>(true).Where(t => t.name == "iOS status time" || t.name == "iOS status indicators").ToArray())
                    Object.DestroyImmediate(status.gameObject);
                ui.goldenPet = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/pet-appearance.png");
                if (name == "Me") SleepetSettingsSceneAuthoring.ApplyTo(ui);
                if (name == "Home")
                {
                    var home = ui.pages[0].transform;
                    ui.homeGreeting = home.Find("Greeting").GetComponent<Text>();
                    ui.homeReadyLabel = home.Find("ReadyToSleep").GetComponentInChildren<Text>();
                    ui.homeARLabel = home.Find("MeetMochaAR").GetComponentInChildren<Text>();
                    ui.homeTapHint = home.Find("Tap hint").GetComponent<Text>();
                }
                if (ui.timeSheet) Time(ui);
                if (ui.petSheet) Pet(ui);
                if (name == "Weekly") Weekly(ui);
                EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets(); Debug.Log("Audited Figma interactions saved to all eight authored scenes.");
        }
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
        static T Component<T>(GameObject go) where T : Component { return go.GetComponent<T>() ?? go.AddComponent<T>(); }
        static Image Image(Transform p, string name, float x, float y, float w, float h, Color color, Sprite sprite = null)
        {
            var go = Child(p, name); Box(go.transform, x, y, w, h); var im = Component<Image>(go); im.color = color;
            im.sprite = sprite; im.type = sprite == panel ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            im.raycastTarget = false; return im;
        }
        static Text Text(Transform p, string name, float x, float y, float w, float h, string value, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = Child(p, name); Box(go.transform, x, y, w, h); var t = Component<Text>(go);
            t.text = value; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.color = color;
            t.alignment = anchor; t.raycastTarget = false; return t;
        }
        static Button Button(Transform p, string name, float x, float y, float w, float h, string label, UnityAction action, bool pill = false)
        {
            var im = Image(p, name, x, y, w, h, pill ? Card : Color.clear, pill ? panel : null); im.raycastTarget = true;
            var b = Component<Button>(im.gameObject); b.targetGraphic = im; b.onClick = new Button.ButtonClickedEvent();
            if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
            if (label != "") Text(im.transform, "Label", 0, 0, w, h, label, pill ? 15 : 23, pill ? Ink : Color.white, TextAnchor.MiddleCenter);
            return b;
        }
        static InputField Input(Transform p, string name, float x, float y, float w, float h, int size, int limit, bool number = true)
        {
            var im = Image(p, name, x, y, w, h, Card, panel); im.raycastTarget = true;
            var field = Component<InputField>(im.gameObject); field.targetGraphic = im;
            field.textComponent = Text(im.transform, "Value", 4, 0, w - 8, h, "", size, Ink, TextAnchor.MiddleCenter);
            field.characterLimit = limit; field.contentType = number ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
            field.keyboardType = number ? TouchScreenKeyboardType.NumberPad : TouchScreenKeyboardType.Default;
            return field;
        }
        static Transform Body(GameObject sheet, float height, UnityAction cancel)
        {
            foreach (Transform t in sheet.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(t.gameObject);
            Button(sheet.transform, "Dismiss", 0, 0, 402, 874, "", cancel).GetComponent<Image>().color = new Color(0, 0, 0, .3f);
            var im = Image(sheet.transform, "Sheet", 0, 874 - height, 402, height, Navy, panel); im.raycastTarget = true;
            sheet.SetActive(false); return im.transform;
        }
        static void Time(SleepetHighFi ui)
        {
            var body = Body(ui.timeSheet, 423, ui.CloseTimeSheet);
            var picker = Component<SleepetTimePicker>(ui.gameObject); ui.timePicker = picker;
            Text(body, "Bedtime label", 36, 10, 300, 23, "BEDTIME", 17, Color.white);
            Text(body, "Wake label", 36, 126, 300, 23, "WAKE UP", 17, Color.white);
            Text(body, "Wind label", 36, 242, 300, 23, "WIND-DOWN", 17, Color.white);
            picker.bedHour = Input(body, "Bedtime hour", 36, 38, 110, 76, 49, 2);
            picker.bedMinute = Input(body, "Bedtime minute", 171, 38, 110, 76, 49, 2);
            picker.wakeHour = Input(body, "Wake hour", 36, 154, 110, 76, 49, 2);
            picker.wakeMinute = Input(body, "Wake minute", 171, 154, 110, 76, 49, 2);
            picker.windMinute = Input(body, "Wind minutes", 36, 270, 110, 76, 49, 3);
            picker.windSecond = Input(body, "Wind seconds", 171, 270, 110, 76, 49, 2);
            for (int i = 0; i < 3; i++) Text(body, "Colon " + i, 148, 38 + i * 116, 22, 76, ":", 49, Color.white, TextAnchor.MiddleCenter);
            picker.bedAM = Button(body, "Bedtime AM", 304, 38, 62, 38, "AM", picker.SetBedAM, true).GetComponent<Image>();
            picker.bedPM = Button(body, "Bedtime PM", 304, 76, 62, 38, "PM", picker.SetBedPM, true).GetComponent<Image>();
            picker.wakeAM = Button(body, "Wake AM", 304, 154, 62, 38, "AM", picker.SetWakeAM, true).GetComponent<Image>();
            picker.wakePM = Button(body, "Wake PM", 304, 192, 62, 38, "PM", picker.SetWakePM, true).GetComponent<Image>();
            Text(body, "Minutes unit", 36, 347, 110, 22, "Minutes", 12, Color.white, TextAnchor.MiddleCenter);
            Text(body, "Seconds unit", 171, 347, 110, 22, "Seconds", 12, Color.white, TextAnchor.MiddleCenter);
            Button(body, "Cancel", 36, 380, 90, 30, "Cancel", ui.CloseTimeSheet, true);
            Button(body, "Save", 276, 380, 90, 30, "Save", ui.SaveTimeSheet, true);
            ui.timeBedtimeInput = picker.bedHour; ui.timeWakeInput = picker.wakeHour; ui.timeWindInput = picker.windMinute;
            picker.SetDraft("20:10", "09:52", 20, 0);
        }
        static void Pet(SleepetHighFi ui)
        {
            var body = Body(ui.petSheet, 461, ui.ClosePetSheet);
            Text(body, "Title", 20, 10, 362, 30, "My Sleepet", 22, Color.white, TextAnchor.MiddleCenter);
            ui.petSelection = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Button(body, "Pet preset " + i, 51 + i * 77, 47, 65, 65, "", null, true);
                ui.petSelection[i] = b.GetComponent<Image>(); UnityEventTools.AddIntPersistentListener(b.onClick, ui.SelectPet, i);
                var art = Image(b.transform, "Preset", 5, 5, 55, 55, Color.white, ui.petOptions[i]); art.preserveAspect = true;
            }
            Button(body, "More pets", 291, 58, 44, 44, "+", ui.MorePets);
            Text(body, "Name label", 40, 130, 90, 32, "Name", 18, Color.white);
            ui.petNameInput = Input(body, "Pet name", 146, 130, 204, 34, 18, 20, false); ui.petNameInput.SetTextWithoutNotify("Mocha");
            Text(body, "Appearance label", 40, 175, 110, 75, "Appearance", 18, Color.white);
            ui.petPreview = Image(body, "Appearance preview", 207, 176, 105, 85, Color.white, ui.petOptions[0]); ui.petPreview.preserveAspect = true;
            Button(body, "Previous appearance", 158, 196, 44, 44, "‹", ui.ChangePetAppearance);
            Button(body, "Next appearance", 314, 196, 44, 44, "›", ui.ChangePetAppearance);
            Text(body, "Pose label", 40, 274, 110, 80, "Movements/\nPoses", 18, Color.white);
            ui.petPosePreview = Image(body, "Pose preview", 207, 274, 105, 85, Color.white, ui.petOptions[0]); ui.petPosePreview.preserveAspect = true;
            Button(body, "Previous pose", 158, 294, 44, 44, "‹", ui.ChangePetPose);
            Button(body, "Next pose", 314, 294, 44, 44, "›", ui.ChangePetPose);
            Button(body, "Preview", 42, 392, 110, 36, "Preview", ui.PreviewPet, true);
            Button(body, "Save", 250, 392, 110, 36, "Save", ui.SavePet, true);
        }
        static void Weekly(SleepetHighFi ui)
        {
            var page = ui.pages[(int)HighFiPage.Weekly].transform;
            var routine = page.Find("Routine card"); Box(routine, 24, 296, 354, 112);
            var bedtime = page.Find("Bedtime card"); Box(bedtime, 24, 422, 354, 248);
            var rhythm = page.Find("Rhythm card"); Box(rhythm, 24, 684, 354, 114);
            foreach (var card in new[] { routine, bedtime, rhythm }) { var im = card.GetComponent<Image>(); im.sprite = panel; im.type = UnityEngine.UI.Image.Type.Sliced; im.color = Card; }
            Box(routine.Find("Title"), 19, 12, 310, 23);
            Box(ui.weeklyCount.transform, 19, 40, 55, 39); ui.weeklyCount.fontSize = 30;
            Text(routine, "Nights denominator", 47, 55, 110, 16, "/ 7 nights", 12, Ink);
            Text(routine, "Bedtime goal caption", 19, 83, 150, 18, "Asleep by your goal.", 12, Ink);
            Image(routine, "Divider", 177, 44, 1, 53, new Color(.7f, .83f, .89f));
            ui.weeklyWakeCount = Text(routine, "Wake goal count", 196, 40, 55, 39, "—", 30, Ink);
            Text(routine, "Mornings denominator", 225, 55, 104, 16, "/ 7 mornings", 12, Ink);
            Text(routine, "Wake goal caption", 196, 83, 140, 18, "Up by your goal.", 12, Ink);
            Box(bedtime.Find("Title"), 19, 12, 314, 23);
            ui.weeklyBedtimeLabel = Text(bedtime, "Average bedtime label", 19, 44, 200, 18, "Average bedtime", 12, Ink);
            Box(ui.weeklyAverage.transform, 19, 64, 200, 33); ui.weeklyAverage.fontSize = 25;
            string[] axis = { "11pm", "12am", "08am", "09am" };
            for (int i = 0; i < 4; i++)
            {
                Text(bedtime, "Axis " + i, 19, 94 + 35 * i, 44, 16, axis[i], 12, Ink);
                Image(bedtime, "Grid " + i, 63, 102 + 35 * i, 270, 1, new Color(.7f, .82f, .87f));
            }
            for (int i = 0; i < 7; i++)
            {
                var b = ui.weeklyDayButtons[i]; Box(b.transform, 63 + 38 * i, 102, 36, 143); b.transform.SetAsLastSibling();
                var label = b.GetComponentInChildren<Text>(); Box(label.transform, 0, 108, 36, 16); label.fontSize = 11; label.alignment = TextAnchor.MiddleCenter;
                var bar = b.transform.Find("Bar").GetComponent<Image>(); bar.raycastTarget = false;
            }
            ui.weeklyAverageLine = Image(bedtime, "Average bedtime reference", 63, 140, 270, 1.5f, new Color(.03f, .25f, .4f, .65f)).rectTransform;
            ui.weeklyDataNote = Text(page, "Report data source", 24, 270, 354, 18, "", 10, Color.white, TextAnchor.MiddleCenter);
            Text(rhythm, "Keep using", 19, 44, 215, 18, "Keep using Sleepet everyday.", 13, Ink);
            ui.weeklyAverageSleep = Text(rhythm, "Average sleep", 19, 66, 226, 19, "No sessions this week", 13, Ink);
            var old = rhythm.GetComponentsInChildren<Text>().FirstOrDefault(t => t.text.Contains("daily sleep report")); if (old) Object.DestroyImmediate(old.gameObject);
            Text(bedtime, "Date hint", 19, 227, 314, 17, "Tap any day to see your daily sleep report", 12, Ink);
            Text(rhythm, "Health description", 248, 52, 90, 48, "Open Health\nfor details", 12, Ink);
            WeeklyStyle(ui, page, routine, bedtime, rhythm);
        }

        static void WeeklyStyle(SleepetHighFi ui, Transform page, Transform routine, Transform bedtime, Transform rhythm)
        {
            var secondary = new Color(73 / 255f, 107 / 255f, 134 / 255f);
            var deep = new Color(2 / 255f, 69 / 255f, 122 / 255f);
            foreach (var card in new[] { routine, bedtime, rhythm })
            {
                foreach (var t in card.GetComponentsInChildren<Text>()) t.color = secondary;
                var title = card.Find("Title").GetComponent<Text>();
                Box(title.transform, 19, 17, 314, 23); title.fontSize = 17; title.fontStyle = FontStyle.Bold; title.color = Ink;
            }
            foreach (var t in new[] { ui.weeklyCount, ui.weeklyWakeCount }) { t.color = deep; t.fontStyle = FontStyle.Bold; }
            ui.weeklyAverage.color = Ink; ui.weeklyAverage.fontStyle = FontStyle.Bold;
            routine.Find("Divider").GetComponent<Image>().color = new Color(214 / 255f, 232 / 255f, 238 / 255f);
            for (int i = 0; i < 4; i++) bedtime.Find("Grid " + i).GetComponent<Image>().color = new Color(214 / 255f, 232 / 255f, 238 / 255f);
            var smallPanel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-5.png");
            for (int i = 0; i < 7; i++)
            {
                var button = ui.weeklyDayButtons[i];
                var label = button.GetComponentInChildren<Text>(); label.fontSize = 12;
                label.color = i == 6 ? Ink : secondary; label.fontStyle = i == 6 ? FontStyle.Bold : FontStyle.Normal;
                var bar = button.transform.Find("Bar").GetComponent<Image>(); bar.sprite = smallPanel; bar.type = UnityEngine.UI.Image.Type.Sliced;
                Box(bar.transform, 7, 0, 22, new[] { 87, 99, 77, 87, 87, 99, 99 }[i]);
                bar.color = i == 6 ? Ink : new Color(.20f, .66f, .80f);
                label.text = new[] { "M", "T", "W", "T", "F", "S", "S" }[i];
                foreach (var effect in bar.GetComponents<Shadow>()) Object.DestroyImmediate(effect);
                if (i == 6) Image(button.transform, "Selected day underline", 13, 123, 10, 1, Ink);
            }
            ui.weeklyAverageLine.GetComponent<Image>().color = Color.clear;
            for (int i = 0; i < 34; i++) Image(ui.weeklyAverageLine, "Dash " + i, i * 8, 0, Mathf.Min(4, 270 - i * 8), 1.5f, deep);
            var keep = rhythm.Find("Keep using").GetComponent<Text>(); Box(keep.transform, 19, 47, 220, 16); keep.fontSize = 14; keep.color = deep;
            Box(ui.weeklyAverageSleep.transform, 19, 68, 229, 18); ui.weeklyAverageSleep.fontSize = 14; ui.weeklyAverageSleep.color = deep;
            var health = rhythm.Find("Health description").GetComponent<Text>(); Box(health.transform, 236, 48, 117, 53); health.text = "Click to see more\ninformation"; health.fontSize = 13;
            var healthButton = rhythm.Find("Open health details");
            if (healthButton) { Box(healthButton, 236, 10, 112, 94); foreach (var t in healthButton.GetComponentsInChildren<Text>()) t.text = ""; }
            Image(rhythm, "Watch icon", 248, 15, 24, 28, Color.white, WeeklyIcon("weekly-watch.png"));
            Image(rhythm, "Open details chevron", 305, 20, 20, 20, Color.white, WeeklyIcon("weekly-chevron.png"));
            var message = page.Find("Mocha message"); Box(message, 28, 133, 354, 61); message.GetComponent<Image>().sprite = panel; message.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Sliced;
            var messageText = message.GetComponentInChildren<Text>(); if (messageText) { Box(messageText.transform, 16, 10, 208, 40); messageText.fontSize = 16; messageText.alignment = TextAnchor.MiddleCenter; }
            var mascot = page.GetComponentsInChildren<Transform>().First(t => t.name == "Sleeping mascot");
            Box(mascot, mascot.parent == message ? 228 : 256, mascot.parent == message ? -7.5f : 125.5f, 118, 76);
            var period = page.Find("Weekly period display"); Box(period, 24, 214, 369, 46);
            var rim = period.GetComponent<Image>(); rim.sprite = panel; rim.type = UnityEngine.UI.Image.Type.Sliced; rim.color = new Color(214 / 255f, 232 / 255f, 238 / 255f);
            var inside = Image(period, "Period navy inset", 4, 4, 361, 38, new Color(0, 27 / 255f, 72 / 255f), panel); inside.transform.SetAsFirstSibling();
            // The range label is a page sibling in the authored scene.
            Box(ui.weeklyRange.transform, ui.weeklyRange.transform.parent == period ? 8 : 32, ui.weeklyRange.transform.parent == period ? 4 : 218, 353, 38); ui.weeklyRange.fontSize = 20;
            Box(ui.weeklyDataNote.transform, 24, 270, 292, 18); ui.weeklyDataNote.fontSize = 9; ui.weeklyDataNote.alignment = TextAnchor.MiddleLeft;
            Text(page, "Nights in period", 326, 271, 54, 15, "7 nights", 12, Color.white, TextAnchor.MiddleRight);
            foreach (var card in new[] { routine, bedtime, rhythm, message })
            {
                var outline = Component<Outline>(card.gameObject); outline.effectColor = Color.white; outline.effectDistance = new Vector2(.5f, -.5f);
            }
            ui.navActive = new Color(1, 1, 1, .14f);
            for (int i = 0; i < ui.navHighlights.Length; i++)
            {
                var highlight = ui.navHighlights[i]; if (!highlight) continue;
                highlight.sprite = panel; highlight.type = UnityEngine.UI.Image.Type.Sliced;
                highlight.pixelsPerUnitMultiplier = 22f / 14f;
                highlight.color = i == 1 ? ui.navActive : Color.clear;
                Box(highlight.transform, new[] { 24, 156, 264 }[i], 5, 112, 56);
            }
        }

        static Sprite WeeklyIcon(string name)
        {
            var path = "Assets/Sleepet/Art/Figma/" + name;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
