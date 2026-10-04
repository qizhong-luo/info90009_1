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
    // Targeted, repeatable changes to editable scene controls.
    public static class SleepetSceneIssueFixes
    {
        static readonly Color Ink = new Color(.06f, .16f, .32f), Pale = new Color(.90f, .95f, .98f);
        static Sprite Rounded => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-22.png");
        static Sprite Check => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        [MenuItem("Sleepet/Fix Reported Scene Interactions")]
        public static void Apply()
        {
            foreach (string name in new[] { "PersonalisePet", "MorningTodos", "MorningEnd", "Routine", "AR" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_" + name + ".unity");
                var flow = Object.FindFirstObjectByType<SleepetFlow>();
                var ui = Object.FindFirstObjectByType<SleepetHighFi>();
                if (name == "PersonalisePet") { Dropdown(flow.breed); Dropdown(flow.colour); }
                if (name == "MorningTodos")
                {
                    var toggle = flow.notify;
                    toggle.interactable = true;
                    var hit = toggle.GetComponent<Image>() ?? toggle.gameObject.AddComponent<Image>();
                    hit.color = Color.clear; hit.raycastTarget = true;
                    var background = toggle.targetGraphic as Image;
                    if (background) { background.color = Color.white; Box(background.transform, 0, 4, 22, 22); }
                    var check = toggle.graphic as Image;
                    check.sprite = Check; check.color = Ink; check.raycastTarget = false;
                    check.rectTransform.anchorMin = check.rectTransform.anchorMax = check.rectTransform.pivot = Vector2.one * .5f;
                    check.rectTransform.anchoredPosition = Vector2.zero; check.rectTransform.sizeDelta = new Vector2(16, 16);
                }
                if (name == "MorningEnd")
                {
                    flow.primary.onClick = new Button.ButtonClickedEvent();
                    flow.primary.transition = Selectable.Transition.None;
                    var hit = flow.GetComponent<Image>() ?? flow.gameObject.AddComponent<Image>();
                    hit.color = Color.clear; hit.raycastTarget = true;
                    flow.transition.raycastTarget = false;
                }
                if (name == "Routine") Routine(ui);
                if (name == "AR") Chat(ui);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SleepetProjectPaths.HomeScenePath);
            Debug.Log("SCENE_ISSUE_FIXES_SAVED");
        }
        static RectTransform Box(Transform t, float x, float y, float w, float h)
        {
            var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        static GameObject Node(Transform p, string name, float x, float y, float w, float h)
        {
            var t = p.Find(name);
            var go = t ? t.gameObject : new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(p, false); Box(go.transform, x, y, w, h); return go;
        }
        static Image Panel(Transform p, string name, float x, float y, float w, float h, Color color)
        {
            var go = Node(p, name, x, y, w, h); var im = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            im.sprite = Rounded; im.type = Image.Type.Sliced; im.color = color; return im;
        }
        static Text Label(Transform p, string name, float x, float y, float w, float h, string value, int size = 18)
        {
            var go = Node(p, name, x, y, w, h); var text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Sleepet/Art/Fonts/Arial.ttf");
            text.text = value; text.fontSize = size; text.color = Ink; text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleLeft; text.supportRichText = false; return text;
        }
        static Button Button(Transform p, string name, float x, float y, float w, float h, string title, UnityAction callback)
        {
            var image = Panel(p, name, x, y, w, h, Pale);
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent();
            if (callback != null) UnityEventTools.AddPersistentListener(button.onClick, callback);
            Label(button.transform, "Label", 6, 0, w - 12, h, title).alignment = TextAnchor.MiddleCenter;
            return button;
        }
        static void Dropdown(Dropdown dropdown)
        {
            var template = dropdown.template;
            template.sizeDelta = new Vector2(0, 200);
            template.GetComponent<Image>().color = Color.white;
            var item = template.GetComponentInChildren<Toggle>(true);
            var itemRect = (RectTransform)item.transform; itemRect.sizeDelta = new Vector2(0, 40);
            var content = (RectTransform)item.transform.parent; content.sizeDelta = new Vector2(0, 40);
            var text = dropdown.itemText;
            text.font = dropdown.captionText.font; text.color = Ink; text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(34, 2); text.rectTransform.offsetMax = new Vector2(-8, -2);
            var mark = item.graphic as Image; mark.sprite = Check; mark.color = Ink;
            mark.rectTransform.sizeDelta = new Vector2(16, 16);
            var arrow = dropdown.transform.Find("Arrow"); if (arrow) arrow.gameObject.SetActive(false);
        }
        static RectTransform Scroll(Transform parent, string name, float x, float y, float width, float height)
        {
            var view = Panel(parent, name, x, y, width, height, Color.clear);
            if (!view.GetComponent<RectMask2D>()) view.gameObject.AddComponent<RectMask2D>();
            var content = (RectTransform)Node(view.transform, "Content", 0, 0, width, height).transform;
            var scroll = view.GetComponent<ScrollRect>() ?? view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view.rectTransform; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; return content;
        }
        static InputField DateField(Transform parent, string name, float x, float width, int limit)
        {
            Label(parent, name + " caption", x, 64, width, 25, name, 15);
            var im = Panel(parent, name, x, 94, width, 48, Color.white);
            var field = im.GetComponent<InputField>() ?? im.gameObject.AddComponent<InputField>();
            field.targetGraphic = im; field.textComponent = Label(im.transform, "Value", 10, 0, width - 20, 48, "");
            field.contentType = InputField.ContentType.IntegerNumber; field.characterLimit = limit; return field;
        }
        static void Routine(SleepetHighFi ui)
        {
            var page = ui.pages[(int)HighFiPage.Routine].transform;
            var card = page.Find("Events card");
            Box(card, 22, 571, 357, 168);
            ui.routineEvents.gameObject.SetActive(false);
            card.Find("Clear short event").gameObject.SetActive(false);
            Box(card.Find("Add event"), 12, 130, 230, 30);
            ui.customEventContent = Scroll(page, "Custom event choices", 24, 509, 354, 56);
            if (!ui.customEventTemplate)
            {
                ui.customEventTemplate = Object.Instantiate(ui.routineGroceries, ui.customEventContent);
                ui.customEventTemplate.name = "Custom event template";
            }
            Box(ui.customEventTemplate.transform, 0, 0, 350, 32);
            ui.customEventTemplate.onValueChanged = new Toggle.ToggleEvent();
            Box(ui.customEventTemplate.GetComponentInChildren<Text>(true).transform, 28, 0, 318, 32);
            ui.customEventTemplate.gameObject.SetActive(false);
            ui.selectedEventContent = Scroll(card, "Selected events", 10, 53, 337, 74);
            var row = Button(ui.selectedEventContent, "Selected event template", 0, 0, 337, 36, "", null);
            row.GetComponent<Image>().raycastTarget = false;
            var label = row.GetComponentInChildren<Text>(); Box(label.transform, 4, 0, 291, 36); label.alignment = TextAnchor.MiddleLeft; label.fontSize = 15;
            var cross = Panel(row.transform, "Delete hit area", 301, 1, 34, 34, Color.clear);
            Label(cross.transform, "Delete icon", 0, 0, 34, 34, "×", 24).alignment = TextAnchor.MiddleCenter;
            row.targetGraphic = cross; ui.selectedEventTemplate = row; row.gameObject.SetActive(false);
            ui.eventInput.characterLimit = 120;
            ui.dateSheet = Node(ui.transform, "Routine date sheet", 0, 0, 402, 874);
            var backdrop = Button(ui.dateSheet.transform, "Dismiss", 0, 0, 402, 874, "", ui.CloseDateSheet);
            backdrop.GetComponent<Image>().color = new Color(0, .04f, .12f, .45f);
            var sheet = Panel(ui.dateSheet.transform, "Sheet", 12, 550, 378, 300, Pale);
            Label(sheet.transform, "Title", 24, 18, 330, 32, "Choose a date", 23);
            ui.dateYear = DateField(sheet.transform, "Year", 24, 112, 4);
            ui.dateMonth = DateField(sheet.transform, "Month", 150, 90, 2);
            ui.dateDay = DateField(sheet.transform, "Day", 254, 100, 2);
            ui.dateError = Label(sheet.transform, "Error", 24, 152, 330, 36, "", 14);
            Button(sheet.transform, "Cancel", 24, 211, 150, 48, "Cancel", ui.CloseDateSheet);
            var save = Button(sheet.transform, "Save date", 190, 211, 164, 48, "Save date", ui.SaveRoutineDate);
            save.GetComponent<Image>().color = Ink; save.GetComponentInChildren<Text>().color = Color.white;
            ui.dateSheet.SetActive(false);
        }
        static void Chat(SleepetHighFi ui)
        {
            if (ui.arModelNotice) ui.arModelNotice.gameObject.SetActive(false);
            if (ui.arAIStatus) ui.arAIStatus.text = "";
            Box(ui.arInput.transform, 20, 428, 362, 64);
            var image = ui.arInput.GetComponent<Image>(); image.sprite = Rounded; image.type = Image.Type.Sliced; image.color = Color.white;
            var outline = ui.arInput.GetComponent<Outline>(); if (outline) Object.DestroyImmediate(outline);
            var colors = ui.arInput.colors; colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white; ui.arInput.colors = colors;
            ui.arInput.lineType = InputField.LineType.MultiLineNewline;
            foreach (var text in ui.arInput.GetComponentsInChildren<Text>(true))
            {
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(16, 13); text.rectTransform.offsetMax = new Vector2(-62, -13);
                text.fontSize = 16; text.alignment = TextAnchor.MiddleLeft;
            }
            Box(ui.arSendButton.transform, 330, 440, 40, 40);
            var send = ui.arSendButton.GetComponent<Image>(); send.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Flow/ui-circle.png"); send.type = Image.Type.Simple; send.color = Color.white;
            colors = ui.arSendButton.colors; colors.normalColor = Ink; colors.highlightedColor = new Color(.12f,.32f,.52f); colors.selectedColor = Ink; colors.disabledColor = new Color(.65f,.74f,.82f); ui.arSendButton.colors = colors;
            ui.arSendButton.transform.SetAsLastSibling();
            var arrow = ui.arSendButton.GetComponentInChildren<Text>(); arrow.text = "↑"; arrow.fontSize = 25; arrow.color = Color.white;
        }
    }
}
#endif
