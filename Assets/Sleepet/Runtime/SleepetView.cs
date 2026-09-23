using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // References point at authored objects in SleepetApp.prefab. No runtime UI construction.
    public sealed class SleepetView : MonoBehaviour
    {
        [Header("Pages - toggle to edit each page in Prefab Mode")]
        public GameObject homePage, sleepPage, mePage;
        public GameObject coverPanel;
        [Header("Authored overlays")]
        public GameObject chatPanel, resultPanel, debugPanel, cameraPanel, reminderPanel;
        [Header("Navigation")]
        public Button homeTab, sleepTab, meTab;
        public Color activeTab = new Color(0.22f, 0.39f, 0.34f);
        public Color inactiveTab = new Color(0.88f, 0.9f, 0.86f);
        public Text sessionBadge, logStatus;
        public PetController homePet, sleepPet, resultPet;
        [Header("Sleep")]
        public Text clock, sleepStatus, companionPrompt, sleepSettingsSummary;
        public Button startSleep, resumeMedia;
        public HoldToEnd holdToEnd;
        [Header("Chat")]
        public InputField chatInput;
        public Button sendChat;
        public Text chatStatus;
        public Text[] chatBubbles;
        [Header("Me - saved settings")]
        public Toggle reminderEnabled;
        public InputField reminderTime;
        public Dropdown sound, behaviour, companion;
        public Slider volume;
        public Text volumeLabel, settingsNotice, previewLabel;
        [Header("Me - paginated history")]
        public Toggle sampleHistory;
        public Text historySummary, historyPagination, emptyHistory;
        public Text[] historyRows;
        public Button previousHistory, nextHistory;
        public TextAsset sampleData;
        [Header("Session result")]
        public Text resultMessage, resultSummary;
        [Header("Reminder")]
        public Text reminderMessage;
    }
}
