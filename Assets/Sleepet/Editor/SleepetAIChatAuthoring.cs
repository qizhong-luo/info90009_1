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
    // Creates serialized editable controls, not a runtime UI overlay.
    public static class SleepetAIChatAuthoring
    {
        static readonly Color Ink = new Color(.06f, .16f, .32f), Pale = new Color(.92f, .96f, .99f);
        [MenuItem("Sleepet/Apply Refined AI Chat")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_AR.unity");
            var ui = Object.FindFirstObjectByType<SleepetHighFi>();
            var root = ui.arChatControls.transform;
            Rect(root, 0, 310, 402, 564);
            var background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-22.png");
            background.type = Image.Type.Sliced; background.color = new Color(.96f, .98f, 1); background.raycastTarget = true;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                button.gameObject.SetActive(false);
            if (ui.arDataLabel) ui.arDataLabel.transform.parent.gameObject.SetActive(false);
            ui.arDataLabel = null;
            ui.arSkillButtons = new Button[0];
            ui.arModelLabel = Button(root, "AI provider", 20, 20, 164, 36, "Local model  ↔", ui.CycleARModel).GetComponentInChildren<Text>();
            ui.arAIStatus = Label(root, "AI status", 200, 20, 182, 36, "Here with you", 13);
            ui.arAIStatus.alignment = TextAnchor.MiddleRight;
            var viewport = Child(root, "Conversation viewport"); Rect(viewport, 20, 80, 362, 328);
            var mask = viewport.GetComponent<RectMask2D>() ?? viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var scroll = viewport.GetComponent<ScrollRect>() ?? viewport.gameObject.AddComponent<ScrollRect>();
            ui.arChat.transform.SetParent(viewport, false);
            var content = ui.arChat.rectTransform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(-12, 328);
            ui.arChat.fontSize = 16; ui.arChat.lineSpacing = 1.2f; ui.arChat.color = Ink; ui.arChat.supportRichText = false;
            ui.arChat.alignment = TextAnchor.UpperLeft; ui.arChat.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.arChat.verticalOverflow = VerticalWrapMode.Overflow; ui.arChat.raycastTarget = false;
            var fit = ui.arChat.GetComponent<ContentSizeFitter>() ?? ui.arChat.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = (RectTransform)viewport; scroll.content = content; scroll.horizontal = false;
            scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; ui.arChatScroll = scroll;
            ui.arInput.gameObject.SetActive(true); Rect(ui.arInput.transform, 20, 426, 302, 48);
            ui.arInput.characterLimit = 2000; ui.arInput.textComponent.supportRichText = false;
            ui.arInput.textComponent.fontSize = 16;
            if (ui.arInput.placeholder is Text placeholder) { placeholder.text = "Write a message..."; placeholder.fontSize = 16; placeholder.fontStyle = FontStyle.Normal; }
            ui.arSendButton.gameObject.SetActive(true); Rect(ui.arSendButton.transform, 334, 426, 48, 48);
            Style(ui.arSendButton, true);
            ui.arSendButton.GetComponentInChildren<Text>().fontSize = 26;
            ui.arModelNotice = Label(root, "Model notice", 20, 480, 362, 24, "On your device. No Internet needed.", 11);
            ui.arModelNotice.color = new Color(.35f, .42f, .5f);
            ui.arRetryButton = Button(root, "Retry AI", 20, 516, 96, 32, "Try again", ui.RetryARMessage);
            ui.arCancelButton = Button(root, "Cancel AI", 20, 516, 96, 32, "Stop", ui.CancelARMessage);
            Button(root, "Close chat", 270, 516, 112, 32, "Close chat", ui.CloseAR);
            Rect(ui.arPlaceControls.transform, 0, 664, 402, 210);
            foreach (var button in ui.arPlaceControls.GetComponentsInChildren<Button>(true))
            {
                string method = button.onClick.GetPersistentMethodNameSafe();
                if (method == "ARTestBackground") Rect(button.transform, 24, 8, 172, 32);
                if (method == "ARSwitchCamera") Rect(button.transform, 206, 8, 172, 32);
            }
            Rect(ui.arPlaceButton.transform, 24, 58, 354, 48); Style(ui.arPlaceButton, true);
            var meetLabel = ui.arPlaceButton.GetComponentInChildren<Text>(); meetLabel.text = "Meet the Sleepet in AR"; meetLabel.fontSize = 18;
            ui.arChatEntryButton = Button(ui.arPlaceControls.transform, "Chat with the Sleepet", 24, 120, 354, 48, "Chat with the Sleepet", ui.OpenSleepetChat);
            ui.arChatEntryButton.GetComponentInChildren<Text>().fontSize = 18;
            Rect(ui.arPet.transform, 141, 168, 120, 126);
            Rect(ui.arStatus.transform, 24, 106, 354, 52); ui.arStatus.fontSize = 25; ui.arStatus.color = Ink;
            EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Sleepet independent AR and chat controls saved.");
        }
        public static void ApplyAndBuild() { Apply(); SleepetWindowsBuild.Build(); }
        static void Style(Button button, bool primary)
        {
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-5.png");
            image.type = Image.Type.Sliced;
            image.color = primary ? new Color(.06f, .13f, .29f) : new Color(.89f, .93f, .96f);
            var text = button.GetComponentInChildren<Text>(); if (text) text.color = primary ? Color.white : Ink;
        }
        static string GetPersistentMethodNameSafe(this Button.ButtonClickedEvent action)
        { return action.GetPersistentEventCount() > 0 ? action.GetPersistentMethodName(0) : ""; }
        static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child) return child;
            child = new GameObject(name, typeof(RectTransform)).transform; child.SetParent(parent, false); return child;
        }
        static void Rect(Transform t, float x, float y, float w, float h)
        {
            var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
        static Text Label(Transform parent, string name, float x, float y, float w, float h, string value, int size)
        {
            var t = Child(parent, name); Rect(t, x, y, w, h);
            var label = t.GetComponent<Text>() ?? t.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size;
            label.color = Ink; label.text = value; label.supportRichText = false; label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleLeft; return label;
        }
        static Button Button(Transform parent, string name, float x, float y, float w, float h, string value, UnityAction callback)
        {
            var t = Child(parent, name); t.gameObject.SetActive(true); Rect(t, x, y, w, h);
            var image = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>(); image.color = Pale;
            var button = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick = new Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick, callback);
            Label(t, "Label", 6, 0, w - 12, h, value, 13).alignment = TextAnchor.MiddleCenter;
            Style(button, false);
            return button;
        }
    }
}
#endif
