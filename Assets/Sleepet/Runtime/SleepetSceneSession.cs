using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sleepet
{
    // One backend instance survives every authored UI scene. The counter is a
    // diagnostic guard: scene navigation must never recreate the data store.
    public sealed class SleepetSceneSession : MonoBehaviour
    {
        public static SleepetSceneSession Instance { get; private set; }
#if UNITY_EDITOR
        public static string TestDataDirectoryOverride;
#endif
        public SleepetDemo Demo { get; private set; }
        public HighFiPage CurrentPage { get; private set; } = HighFiPage.Home;
        public HighFiPage ReturnPage { get; private set; } = HighFiPage.Home;
        public DateTime SelectedReportDate { get; private set; } = DateTime.Today;
        [SerializeField] int sceneTransitionCount;
        [SerializeField] int stateRevision;
        public int SceneTransitionCount => sceneTransitionCount;
        public int StateRevision => stateRevision;
        public Guid RuntimeSessionId { get; private set; }

        static readonly string[] SceneNames =
        {
            "Sleepet_Home", "Sleepet_Sleep", "Sleepet_Me", "Sleepet_Routine",
            "Sleepet_Daily", "Sleepet_Weekly", "Sleepet_AR", "Sleepet_DayDetail"
        };

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Demo = GetComponent<SleepetDemo>();
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(TestDataDirectoryOverride)) Demo.dataDirectoryOverride = TestDataDirectoryOverride;
#endif
            RuntimeSessionId = Guid.NewGuid();
            DontDestroyOnLoad(gameObject);
        }

        public void AdoptPage(HighFiPage page)
        {
            if (CurrentPage != page) sceneTransitionCount++;
            CurrentPage = page;
        }

        public void Navigate(HighFiPage target)
        {
            if (target == CurrentPage) return;
            if (target == HighFiPage.AR) ReturnPage = CurrentPage;
            if (CurrentPage == HighFiPage.AR && target != HighFiPage.AR) Demo.CloseCamera();
            CurrentPage = target;
            sceneTransitionCount++;
            SceneManager.LoadScene(SceneNames[(int)target], LoadSceneMode.Single);
        }

        public void MarkStateChanged() { stateRevision++; }

        public void SelectReportDate(DateTime date)
        {
            SelectedReportDate = date.Date;
            Navigate(HighFiPage.DayDetail);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
