using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // All controls and presets are authored in the Setting scene.
    public sealed class SleepetProfileEditor : MonoBehaviour
    {
        public SleepetHighFi app;
        public GameObject sheet;
        public Image avatar, preview;
        public Text displayName, error;
        public InputField nameInput;
        public Sprite[] presets;
        public Image[] selectionBorders;
        int selected;

        void Awake() { nameInput.onValidateInput = ValidateLetter; }
        public static char ValidateLetter(string text, int index, char added)
        { return (added >= 'A' && added <= 'Z') || (added >= 'a' && added <= 'z') ? added : '\0'; }
        public void Refresh()
        {
            var p = app.demo.Store.Preferences;
            displayName.text = p.profileName;
            avatar.sprite = presets[Mathf.Clamp(p.profileAvatar, 0, presets.Length - 1)];
        }
        public void Open()
        {
            app.CloseTimeSheet(); app.ClosePetSheet();
            var p = app.demo.Store.Preferences;
            nameInput.SetTextWithoutNotify(p.profileName);
            selected = Mathf.Clamp(p.profileAvatar, 0, presets.Length - 1);
            error.text = "";
            Select(selected);
            sheet.SetActive(true);
        }
        public void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, presets.Length - 1);
            preview.sprite = presets[selected];
            for (int i = 0; i < selectionBorders.Length; i++)
                selectionBorders[i].color = i == selected ? Color.white : new Color(1, 1, 1, 0.15f);
        }
        public void Cancel() { if (sheet) sheet.SetActive(false); }
        public void Save()
        {
            if (!SleepetStore.ValidProfileName(nameInput.text))
            { error.text = "Use 1–10 English letters (A–Z)."; return; }
            var p = JsonUtility.FromJson<UserPreferences>(JsonUtility.ToJson(app.demo.Store.Preferences));
            p.profileName = nameInput.text;
            p.profileAvatar = selected;
            if (!app.demo.Store.SavePreferences(p)) { error.text = app.demo.Store.Error; return; }
            Refresh(); Cancel();
            SleepetSceneSession.Instance?.MarkStateChanged();
        }
    }
}
