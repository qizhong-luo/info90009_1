using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    public interface ICameraFeed : IDisposable
    {
        string[] Devices { get; }
        Texture Texture { get; }
        bool Updated { get; }
        int Rotation { get; }
        bool Mirrored { get; }
        void Start(int index);
    }
    public sealed class WebcamFeed : ICameraFeed
    {
        WebCamTexture webcam;
        public string[] Devices => Array.ConvertAll(WebCamTexture.devices, d => d.name);
        public Texture Texture => webcam;
        public bool Updated => webcam != null && webcam.didUpdateThisFrame && webcam.width > 16;
        public int Rotation => webcam == null ? 0 : webcam.videoRotationAngle;
        public bool Mirrored => webcam != null && webcam.videoVerticallyMirrored;
        public void Start(int index)
        {
            Dispose();
            var devices = Devices;
            if (devices.Length == 0) throw new InvalidOperationException("No camera device found.");
            webcam = new WebCamTexture(devices[index % devices.Length], 1280, 720, 30);
            webcam.Play();
        }
        public void Dispose()
        {
            if (webcam == null) return;
            webcam.Stop();
            UnityEngine.Object.Destroy(webcam);
            webcam = null;
        }
    }

    public sealed class CameraCompanion : MonoBehaviour
    {
        public RawImage preview;
        public RectTransform previewFrame;
        public GameObject testBackground;
        public Text status;
        public Button placeButton, switchButton;
        public PetController pet;
        public GameObject placementMarker;
        public SleepetDemo app;
        [Min(1)] public float connectionTimeout = 10;
        public string State { get; private set; } = "Closed";
        public int FrameCount { get; private set; }
        public bool IsLive => State == "Live";
        public bool IsTestMode => State == "Test";
        public Func<ICameraFeed> FeedFactory { get; set; } = () => new WebcamFeed();
        public bool SkipPermissionForTests { get; set; }
        ICameraFeed feed;
        Coroutine opening;
        int index;
        bool resumeAfterFocus;
        float lastFrameAt;
        int lastAppliedFrame = -1;

        public void StartCamera()
        {
            CloseFeed();
            FrameCount = 0; lastAppliedFrame = -1;
            pet.gameObject.SetActive(false);
            placementMarker.SetActive(false);
            testBackground.SetActive(false);
            placeButton.interactable = switchButton.interactable = false;
            State = "Opening";
            status.text = "Opening camera...";
            app?.Logger?.Log("CAMERA_OPEN_REQUESTED", "", "desktop overlay; no spatial tracking");
            opening = StartCoroutine(Open());
        }
        IEnumerator Open()
        {
            if (!SkipPermissionForTests)
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
                if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
                { Fail("Camera permission denied. Enable camera access in Windows settings, then Retry."); yield break; }
            }
            string error = null;
            try
            {
                feed = FeedFactory();
                if (feed.Devices.Length == 0) error = "No camera found. Connect a webcam, then Retry; or use the test background.";
                else { switchButton.interactable = feed.Devices.Length > 1; feed.Start(index); }
            }
            catch (Exception) { error = "Camera unavailable or busy. Close other camera apps, then Retry."; }
            if (error != null) { Fail(error); yield break; }
            float until = Time.realtimeSinceStartup + connectionTimeout;
            while (Time.realtimeSinceStartup < until)
            {
                if (feed != null && feed.Updated)
                {
                    State = "Live";
                    lastFrameAt = Time.realtimeSinceStartup;
                    ApplyFrame();
                    preview.gameObject.SetActive(true);
                    placementMarker.SetActive(true);
                    placeButton.interactable = true;
                    status.text = "Live camera | tap Place Mocha, then drag to reposition.\nScreen overlay only; no room or plane tracking.";
                    app?.Logger?.Log("CAMERA_STARTED", "", "live frames received; no recording");
                    opening = null;
                    yield break;
                }
                yield return null;
            }
            Fail("No camera frames received. Check Windows camera privacy or whether another app is using it.");
        }
        void Update()
        {
            if (!IsLive || feed == null) return;
            if (!feed.Updated)
            {
                if (Time.realtimeSinceStartup - lastFrameAt > connectionTimeout)
                    Fail("Camera stream stopped. Reconnect the device, then Retry.");
                return;
            }
            ApplyFrame();
        }
        void ApplyFrame()
        {
            if (lastAppliedFrame == Time.frameCount) return;
            lastAppliedFrame = Time.frameCount;
            lastFrameAt = Time.realtimeSinceStartup;
            FrameCount++;
            preview.texture = feed.Texture;
            preview.uvRect = new Rect(0, feed.Mirrored ? 1 : 0, 1, feed.Mirrored ? -1 : 1);
            var rt = preview.rectTransform;
            rt.localEulerAngles = new Vector3(0, 0, -feed.Rotation);
            if (feed.Texture == null) return;
            float ratio = (float)feed.Texture.width / feed.Texture.height;
            bool rotated = feed.Rotation % 180 != 0;
            float maxW = rotated ? previewFrame.rect.height : previewFrame.rect.width;
            float maxH = rotated ? previewFrame.rect.width : previewFrame.rect.height;
            float w = Mathf.Min(maxW, maxH * ratio);
            rt.sizeDelta = new Vector2(w, w / ratio);
        }
        public void UseTestBackground()
        {
            CloseFeed();
            State = "Test";
            testBackground.SetActive(true);
            placementMarker.SetActive(true);
            pet.gameObject.SetActive(false);
            placeButton.interactable = true;
            switchButton.interactable = false;
            status.text = "Test background | no camera is active.\nPlace and drag Mocha to test the interaction.";
            app?.Logger?.Log("CAMERA_TEST_BACKGROUND_SELECTED");
        }
        public void PlacePet()
        {
            if (!IsLive && !IsTestMode) return;
            pet.gameObject.SetActive(true);
            ((RectTransform)pet.transform).anchoredPosition = new Vector2(0, -30);
            placementMarker.SetActive(false);
            pet.Tap();
            app?.Logger?.Log("PET_PLACED", "Mocha", IsLive ? "webcam screen overlay" : "explicit test background");
        }
        public void SwitchCamera() { index++; StartCamera(); }
        public void TapPet()
        {
            pet.Tap();
            app?.Logger?.Log("PET_TAPPED", "Mocha", "camera overlay");
        }
        void Fail(string message)
        {
            feed?.Dispose(); feed = null;
            preview.texture = null;
            preview.gameObject.SetActive(false);
            State = "Unavailable";
            status.text = message;
            placeButton.interactable = false;
            opening = null;
            app?.Logger?.Log("CAMERA_UNAVAILABLE", message);
        }
        void CloseFeed()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null;
            feed?.Dispose(); feed = null;
            if (preview != null) { preview.texture = null; preview.gameObject.SetActive(false); }
        }
        void OnDisable()
        {
            CloseFeed();
            if (State != "Closed") app?.Logger?.Log("CAMERA_CLOSED");
            State = "Closed"; resumeAfterFocus = false;
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && (IsLive || State == "Opening"))
            {
                resumeAfterFocus = true;
                CloseFeed(); State = "Paused";
                status.text = "Camera paused while the app is in the background.";
            }
            else if (focused && resumeAfterFocus && gameObject.activeInHierarchy)
            { resumeAfterFocus = false; StartCamera(); }
        }
        void OnDestroy() { CloseFeed(); }
    }
}
