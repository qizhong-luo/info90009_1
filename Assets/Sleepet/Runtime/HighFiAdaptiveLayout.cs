using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // Keeps the 402-point Figma coordinate system while extending the viewport
    // and pinning controls that belong at the bottom of a portrait display.
    public sealed class HighFiAdaptiveLayout : MonoBehaviour
    {
        public SleepetHighFi app;
        const float ReferenceHeight = 874f;
        readonly Dictionary<RectTransform, Vector2> sheetPositions = new Dictionary<RectTransform, Vector2>();
        float lastHeight = -1, lastInset = -1;

        void OnEnable() { lastHeight = -1; lastInset = -1; }

        void LateUpdate()
        {
            if (app == null || app.pages == null || app.pages.Length == 0) return;
            bool landscapePreview = Screen.width > Screen.height;
            var scaler = GetComponent<CanvasScaler>();
            float match = landscapePreview ? 1f : 0f;
            if (!Mathf.Approximately(scaler.matchWidthOrHeight, match))
            {
                scaler.matchWidthOrHeight = match;
                Canvas.ForceUpdateCanvases();
            }
            var canvasRect = (RectTransform)transform;
            float height = canvasRect.rect.height;
            if (height < 100) return;
            float scale = Mathf.Max(0.01f, landscapePreview ? Screen.height / 874f : Screen.width / 402f);
            float bottomInset = landscapePreview ? 0 : Screen.safeArea.yMin / scale;
            float extra = Mathf.Max(0, bottomInset - 34f);
            var activePage = app.pages[(int)app.scenePage];
            if (Mathf.Abs(height - lastHeight) < 0.1f && Mathf.Abs(extra - lastInset) < 0.1f &&
                activePage != null && Mathf.Abs(activePage.GetComponent<RectTransform>().anchoredPosition.x - (canvasRect.rect.width - 402f) / 2f) < 0.1f) return;
            lastHeight = height; lastInset = extra;
            ApplyLayout(height, extra);
        }

        public void ApplyLayout(float height, float extra)
        {
            float delta = height - ReferenceHeight;
            float x = (((RectTransform)transform).rect.width - 402f) / 2f;
            foreach (var page in app.pages)
            {
                if (page == null) continue;
                var rect = (RectTransform)page.transform;
                rect.sizeDelta = new Vector2(402, height);
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                var background = page.transform.Find("Figma gradient") ?? page.transform.Find("Lake and moon");
                if (background != null) ((RectTransform)background).sizeDelta = new Vector2(402, height);
            }
            Bottom(app.bottomNavigation != null ? app.bottomNavigation.transform as RectTransform : null, 0, extra);
            if (app.bottomNavigation != null)
            {
                ((RectTransform)app.bottomNavigation.transform).sizeDelta = new Vector2(402, 66 + extra);
                var nav = (RectTransform)app.bottomNavigation.transform;
                nav.anchoredPosition = new Vector2(x, nav.anchoredPosition.y);
            }

            Bottom(Find(HighFiPage.Home, "ReadyToSleep"), 163, extra);
            Bottom(Find(HighFiPage.Home, "MeetMochaAR"), 99, extra);
            Bottom(Find(HighFiPage.Home, "Tap hint"), 231, extra);
            Bottom(Find(HighFiPage.Home, "TapMocha"), 258, extra);
            Bottom(Find(HighFiPage.Home, "Mocha"), 258, extra);
            Bottom(Find(HighFiPage.Home, "Mocha message bubble"), 445, extra);
            Bottom(Find(HighFiPage.Home, "Mocha message"), 452, extra);
            Bottom(Find(HighFiPage.Sleep, "Slide to end sleep"), 80, extra);
            Bottom(Find(HighFiPage.Sleep, "Charging tip"), 47, extra);
            Bottom(Find(HighFiPage.Sleep, "Start sleep"), 172, extra);
            Bottom(Find(HighFiPage.Routine, "Save"), 63, extra);
            Bottom(Find(HighFiPage.Routine, "Save status"), 29, extra);
            Bottom(Find(HighFiPage.AR, "Place controls"), 0, extra);
            Bottom(Find(HighFiPage.AR, "Chat controls"), 0, extra);
            var meViewport = Find(HighFiPage.Me, "Me scroll viewport");
            if (meViewport != null) meViewport.sizeDelta = new Vector2(402, height - 60 - 66 - extra);
            var arRoom = Find(HighFiPage.AR, "Figma room illustration");
            if (arRoom != null) arRoom.sizeDelta = new Vector2(402, height - 241 - extra);
            var camera = Find(HighFiPage.AR, "Live camera");
            if (camera != null) camera.sizeDelta = new Vector2(402, height - 241 - extra);
            Bottom(Find(HighFiPage.Daily, "Watch card"), 77, extra);
            Bottom(Find(HighFiPage.DayDetail, "Watch card"), 77, extra);
            Bottom(Find(HighFiPage.Weekly, "Rhythm card"), 78, extra);
            float compact = Mathf.Min(0, delta);
            TopShift(Find(HighFiPage.Daily, "Duration card"), 294, compact);
            TopShift(Find(HighFiPage.Daily, "Stages card"), 519, compact);
            TopShift(Find(HighFiPage.DayDetail, "Duration card"), 294, compact);
            TopShift(Find(HighFiPage.DayDetail, "Stages card"), 519, compact);
            TopShift(Find(HighFiPage.Weekly, "Routine card"), 296, compact);
            TopShift(Find(HighFiPage.Weekly, "Bedtime card"), 422, compact);
            foreach (var sheet in new[] { app.timeSheet, app.petSheet, app.eventSheet, app.noticeSheet, app.profileEditor ? app.profileEditor.sheet : null })
            {
                if (sheet == null) continue;
                var sheetRect = (RectTransform)sheet.transform;
                sheetRect.sizeDelta = new Vector2(402, height);
                sheetRect.anchoredPosition = new Vector2(x, sheetRect.anchoredPosition.y);
                foreach (RectTransform child in sheet.transform)
                {
                    if (child.GetComponent<Button>() && child.sizeDelta.y >= ReferenceHeight)
                    {
                        child.anchoredPosition = Vector2.zero;
                        child.sizeDelta = new Vector2(402, height);
                        continue;
                    }
                    if (!sheetPositions.TryGetValue(child, out var basePosition))
                    {
                        basePosition = child.anchoredPosition;
                        sheetPositions[child] = basePosition;
                    }
                    child.anchoredPosition = new Vector2(basePosition.x, basePosition.y - delta);
                }
            }
        }

        RectTransform Find(HighFiPage page, string name)
        {
            var root = (int)page < app.pages.Length ? app.pages[(int)page] : null;
            return root != null ? root.transform.Find(name) as RectTransform : null;
        }

        static void Bottom(RectTransform rect, float bottom, float extra)
        {
            if (rect == null) return;
            var x = rect.anchoredPosition.x;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = new Vector2(x, bottom + extra);
        }

        static void TopShift(RectTransform rect, float top, float shift)
        {
            if (rect == null) return;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -top - shift);
        }
    }
}
