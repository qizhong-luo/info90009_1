#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sleepet.Editor
{
    // Applies source Figma measurements to serialized, individually editable scene objects.
    public static class SleepetFigmaSceneAuthoring
    {
        const string Art = "Assets/Sleepet/Art/Figma/";
        static Sprite panel, panelSmall, timeline, ring, settingClock, profile, petCircle, settingGradient;

        [MenuItem("Sleepet/Apply Daily Without Watch And Setting Layout")]
        public static void Apply()
        {
            panel = SpriteAt("rounded-panel-22.png");
            panelSmall = SpriteAt("rounded-panel-5.png");
            timeline = SpriteAt("report-stage-timeline-figma.png");
            ring = SpriteAt("report-stage-ring-mask.png");
            settingClock = SpriteAt("setting-clock-figma.png");
            profile = SpriteAt("setting-profile-figma.png");
            petCircle = SpriteAt("setting-pet-circle-figma.png");
            settingGradient = SpriteAt("setting-gradient-figma.png");
            foreach (var name in new[] { "Daily", "DayDetail", "Me" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_" + name + ".unity", OpenSceneMode.Single);
                var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SleepetHighFi>(true)).Single();
                if (name == "Me") ApplySetting(ui);
                else ApplyDaily(ui);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Sleepet Figma Daily without Watch and Setting layouts saved as editable Scene objects.");
        }

        static Sprite SpriteAt(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);
            if (!sprite) throw new InvalidOperationException("Missing Figma sprite: " + file);
            return sprite;
        }

        static Color C(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        static Transform Named(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        }

        static RectTransform Rect(Transform transform, float x, float y, float width, float height)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        static Image EditableImage(Transform parent, string name, float x, float y, float width, float height,
            Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            var child = parent.Find(name);
            if (!child) child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
            child.SetParent(parent, false);
            Rect(child, x, y, width, height);
            var image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        static Text EditableText(Transform parent, string name, float x, float y, float width, float height,
            string value, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var child = parent.Find(name);
            if (!child) child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).transform;
            child.SetParent(parent, false);
            Rect(child, x, y, width, height);
            var label = child.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.color = color;
            label.text = value;
            label.raycastTarget = false;
            return label;
        }

        static void Panel(Transform root, string name, float x, float y, float width, float height, Color color)
        {
            var item = Named(root, name);
            Rect(item, x, y, width, height);
            var image = item.GetComponent<Image>();
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        static void ApplyDaily(SleepetHighFi ui)
        {
            var root = ui.pages[(int)ui.scenePage].transform;
            var duration = Named(root, "Duration card");
            var stages = Named(root, "Stages card");
            var watch = Named(root, "Watch card");
            Panel(root, "Duration card", 24, 294, 354, 211, C("#E7F3FC"));
            Panel(root, "Stages card", 24, 519, 354, 205, C("#E7F3FC"));
            Panel(root, "Watch card", 21, 746, 354, 51, C("#3675A5"));
            Rect(Named(root, "Daily period display"), 24, 214, 369, 46);
            var period = Named(root, "Daily period display").GetComponent<Image>();
            period.sprite = panel;
            period.type = Image.Type.Sliced;
            var selectedDay = Named(period.transform, "Day Selected").GetComponent<Image>();
            selectedDay.sprite = panel;
            selectedDay.type = Image.Type.Sliced;
            ui.dailyDateCaption = EditableText(root, "Report date caption", 24, 270, 251, 19,
                "SUNDAY, SEPTEMBER 13", 11, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            ui.dailyRelativeDate = EditableText(root, "Report relative date", 295, 270, 83, 19,
                "Last night", 11, Color.white, TextAnchor.MiddleRight);
            var watchOutline = watch.GetComponent<Outline>();
            if (!watchOutline) watchOutline = watch.gameObject.AddComponent<Outline>();
            watchOutline.effectColor = Color.white;
            watchOutline.effectDistance = new Vector2(1, -1);
            Named(Named(root, "Mocha message"), "Message").GetComponent<Text>().text = "You rested well.";
            Rect(Named(duration, "Duration"), 19, 36, 238, 52);
            ui.dailyDuration.fontSize = 40;
            Rect(Named(duration, "Goal"), 19, 90, 315, 20);
            ui.dailyGoal.fontSize = 16;
            var durationHeading = Named(duration, "SESSION DURATION").GetComponent<Text>();
            durationHeading.text = "TIME ASLEEP";
            Rect(durationHeading.transform, 19, 17, 195, 17);
            var oldTimeline = Named(duration, "Timeline").GetComponent<Image>();
            Rect(oldTimeline.transform, 19, 104, 314, 51);
            oldTimeline.sprite = timeline;
            oldTimeline.type = Image.Type.Simple;
            oldTimeline.color = Color.white;
            oldTimeline.raycastTarget = false;
            ui.dailyStageTimeline = oldTimeline;
            Named(duration, "Timeline scope").gameObject.SetActive(false);
            var badgeBackground = EditableImage(duration, "Goal badge background", 236, 19, 97, 29,
                panel, C("#D6E8EE"), Image.Type.Sliced);
            ui.dailyGoalBadge = EditableText(duration, "Goal badge", 242, 21, 85, 24, "Goal Met", 11,
                C("#018ABE"), TextAnchor.MiddleCenter);
            ui.dailyStart = EditableText(duration, "Sleep start", 19, 155, 95, 18, "11:50 pm", 12, C("#072854"));
            ui.dailyEnd = EditableText(duration, "Sleep end", 248, 155, 85, 18, "8:00 am", 12,
                C("#072854"), TextAnchor.MiddleRight);
            var stageHeading = Named(stages, "Title").GetComponent<Text>();
            Rect(stageHeading.transform, 19, 17, 245, 25);
            stageHeading.text = "Sleep stages";
            stageHeading.fontSize = 17;
            var visual = new System.Collections.Generic.List<GameObject>();
            var arcs = new Image[3];
            string[] stageNames = { "Deep", "Light", "REM" };
            Color[] colors = { C("#02457A"), C("#018ABE"), C("#97CADB") };
            for (int i = 0; i < 3; i++)
            {
                arcs[i] = EditableImage(stages, "Stage " + stageNames[i] + " Arc", 19, 48, 128, 128,
                    ring, colors[i], Image.Type.Filled);
                arcs[i].fillMethod = Image.FillMethod.Radial360;
                arcs[i].fillOrigin = (int)Image.Origin360.Top;
                arcs[i].fillClockwise = true;
                arcs[i].fillAmount = new[] { 110f, 260f, 120f }[i] / 490f - 0.012f;
                var arcRect = arcs[i].rectTransform;
                arcRect.pivot = new Vector2(0.5f, 0.5f);
                arcRect.anchoredPosition = new Vector2(83, -112);
                visual.Add(arcs[i].gameObject);
            }
            ui.dailyStageArcs = arcs;
            ui.dailyStageTotal = EditableText(stages, "Stage total", 36, 92, 94, 27, "8h 10m", 19,
                C("#072854"), TextAnchor.MiddleCenter, FontStyle.Bold);
            visual.Add(ui.dailyStageTotal.gameObject);
            visual.Add(EditableText(stages, "Stage asleep", 43, 122, 80, 18, "ASLEEP", 15,
                C("#496B86"), TextAnchor.MiddleCenter).gameObject);
            Text[] values = new Text[3];
            string[] samples = { "1h 50m", "4h 20m", "2h 00m" };
            for (int i = 0; i < 3; i++)
            {
                float y = 56 + i * 42;
                visual.Add(EditableImage(stages, "Stage " + stageNames[i] + " Dot", 175, y, 7, 7,
                    panel, colors[i], Image.Type.Simple).gameObject);
                visual.Add(EditableText(stages, "Stage " + stageNames[i] + " Label", 190, y - 5, 62, 18,
                    stageNames[i], 14, C("#496B86")).gameObject);
                values[i] = EditableText(stages, "Stage " + stageNames[i] + " Value", 262, y - 5, 73, 19,
                    samples[i], 14, C("#072854"));
                visual.Add(values[i].gameObject);
            }
            ui.dailyStageDeep = values[0];
            ui.dailyStageLight = values[1];
            ui.dailyStageRem = values[2];
            ui.dailyStageVisuals = visual.ToArray();
            ui.dailyStageNote.fontSize = 11;
            var heading = Named(watch, "Heading").GetComponent<Text>();
            heading.text = "Wonder more detail of sleep?\nConnect to smart watch now.       ›";
            heading.fontSize = 18;
            heading.fontStyle = FontStyle.Bold;
            heading.color = Color.white;
            Rect(heading.transform, 29, 4, 310, 43);
            Named(watch, "Watch status").gameObject.SetActive(false);
            var openHealth = Named(watch, "Open Health");
            Rect(openHealth, 0, 0, 354, 51);
            var oldLabel = openHealth.Find("Label");
            if (oldLabel) oldLabel.gameObject.SetActive(false);
        }

        static void ApplySetting(SleepetHighFi ui)
        {
            var root = ui.pages[(int)HighFiPage.Me].transform;
            var settingBackground = Named(root, "Figma gradient").GetComponent<Image>();
            settingBackground.sprite = settingGradient;
            settingBackground.color = Color.white;
            Panel(root, "Plan card", 22, 114, 357, 359, C("#E7F3FC"));
            var dial = Named(root, "Plan dial").GetComponent<Image>();
            Rect(dial.transform, 34, 63, 288, 288);
            dial.sprite = settingClock;
            dial.type = Image.Type.Simple;
            dial.color = Color.white;
            Named(dial.transform, "Dial marks").gameObject.SetActive(false);
            var profileArt = Named(root, "Profile art").GetComponent<Image>();
            profileArt.sprite = profile;
            profileArt.color = Color.white;
            Rect(profileArt.transform, 23, 2, 70, 70);
            Named(root, "Profile avatar").GetComponent<Image>().color = Color.clear;
            var petAvatar = Named(root, "Pet avatar");
            var petAvatarHost = petAvatar.parent.name == "Pet avatar circle" ? petAvatar.parent.parent : petAvatar.parent;
            var petCircleImage = EditableImage(petAvatarHost, "Pet avatar circle", 23, 511, 70, 70,
                petCircle, Color.white);
            var avatarMask = petCircleImage.GetComponent<Mask>();
            if (!avatarMask) avatarMask = petCircleImage.gameObject.AddComponent<Mask>();
            avatarMask.showMaskGraphic = true;
            petAvatar.SetParent(petCircleImage.transform, false);
            Rect(petAvatar, -20, -8, 110, 110);
            Rect(Named(root, "My Sleepet"), 22, 481, 244, 35);
            Rect(ui.mePetName.transform, 102, 512, 200, 26);
            Panel(root, "Preferences card", 22, 627, 357, 118, C("#E7F3FC"));
            var preferencesImage = Named(root, "Preferences card").GetComponent<Image>();
            preferencesImage.sprite = panelSmall;
            preferencesImage.type = Image.Type.Sliced;
            Rect(Named(root, "Preferences"), 22, 590, 230, 35);
            var pref = Named(root, "Preferences card");
            Rect(Named(pref, "Volume"), 18, 41, 98, 42);
            Rect(Named(pref, "Volume slider"), 195, 47, 146, 28);
            Rect(Named(pref, "divider 1"), 13, 42, 330, 2);
            Rect(Named(pref, "Mode"), 18, 75, 98, 42);
            Rect(Named(pref, "Mode value"), 206, 75, 116, 42);
            Rect(Named(pref, "Change mode"), 150, 75, 188, 42);
            EditableText(pref, "Mode previous indicator", 165, 80, 22, 31, "‹", 19,
                C("#001B4A"), TextAnchor.MiddleCenter);
            EditableText(pref, "Mode next indicator", 320, 80, 22, 31, "›", 19,
                C("#001B4A"), TextAnchor.MiddleCenter);
            Rect(Named(pref, "divider 2"), 13, 76, 330, 2);
            ui.meVolumePercent = EditableText(pref, "Volume percent", 142, 43, 47, 30,
                "50%", 15, C("#001B4A"), TextAnchor.MiddleCenter);
            var sliderBackground = Named(pref, "Volume slider").Find("Background");
            if (sliderBackground && sliderBackground.TryGetComponent(out Image background))
                background.color = C("#018ABE");
            var sliderFill = Named(pref, "Volume slider").GetComponentsInChildren<Image>(true)
                .FirstOrDefault(i => i.name == "Fill");
            if (sliderFill) sliderFill.color = C("#034579");
            var sliderHandle = Named(pref, "Volume slider").GetComponentsInChildren<Image>(true)
                .FirstOrDefault(i => i.name == "Handle");
            if (sliderHandle) sliderHandle.color = C("#034579");
            var changePet = Named(root, "Change Pet").GetComponent<Image>();
            changePet.sprite = panel;
            changePet.type = Image.Type.Sliced;
            changePet.color = C("#D4E7ED");
            Rect(changePet.transform, 102, 546, 103, 30);
            // The support menu remains below the fold in the same scrollable Setting scene.
            var support = Named(root, "Setting & Support");
            var supportRect = (RectTransform)support;
            float shift = Mathf.Abs(supportRect.anchoredPosition.y + 749f) < 1f ? 0f : 124f;
            supportRect.anchoredPosition += new Vector2(0, shift);
            foreach (var name in new[] { "Support Privacy & data", "Support Accessibility", "Support Connected Devices",
                         "Support Notifications", "Support Help & Q&A" })
            {
                var row = (RectTransform)Named(root, name);
                row.anchoredPosition += new Vector2(0, shift);
            }
            SleepetSettingsSceneAuthoring.ApplyTo(ui);
        }
    }
}
#endif
