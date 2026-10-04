using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Sleepet
{
    // All visual objects are serialized in individual scenes; this component only binds data and interaction.
    public sealed class SleepetFlow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static readonly string[] PageNames = { "OnboardingSplash", "Account", "ChooseCompanion", "PersonalisePet", "CreatingPet", "MeetPet", "WakeFeedback", "MorningRating", "MorningDrink", "MorningStretch", "MorningTodos", "MorningEnd" };
        public string page;
        public Text title, subtitle, error, value, speech, timerLabel;
        public InputField email, password, petName;
        public Dropdown breed, colour;
        public Image pet, bubble, shade, ring, photo, ratingLight;
        public Image[] selections, dots;
        public Image[] choiceIndicators, ratingLights;
        public Image animatedPet;
        public Sprite[] animationFrames;
        public GameObject videoFallback;
        public Image wakeBackground;
        public Sprite wakeStillScene, goldenPet;
        public Sprite[] pets;
        public Slider rating;
        public Toggle notify;
        public Button primary;
        public Text[] todoLabels;
        public GameObject dataPanel;
        public Text dataText;
        public VideoPlayer video;
        public RawImage videoImage;
        public bool removeWhiteBackground;
        public RectTransform[] splashLetters;
        public CanvasGroup content;
        public Image transition;
        [Tooltip("Real seconds. Set lower only for an explicit accelerated demo.")]
        public float stretchSeconds = 300;
        public bool Ready { get; private set; }
        public SleepSummary Record => Session?.Demo?.Store?.History.records.Find(r => r.sessionId == Session.MorningSessionId && !r.sample);
        SleepetSceneSession Session => SleepetSceneSession.Instance;
        UserPreferences Draft => Session.OnboardingDraft;
        float entered, remaining;
        bool leaving;
        Vector2 bubbleOrigin, dragStart;
        Vector2[] splashOrigins;
        RenderTexture videoTexture;
        Texture2D keyedFrame, photoTexture;
        Sprite photoSprite;
        float nextVideoFrame;
        float ratingTarget, ratingVisual = 3;
        float ratingChanged, shadeFrom, lightFrom;
        float[] lightsFrom;
        readonly float[] shadeLevels = { .6f, .47f, .33f, .18f, .04f };
        readonly float[] lightLevels = { .02f, .07f, .13f, .2f, .28f };
        readonly string[] ratings = { "Exhausted", "Tired", "Okay", "Refreshed", "Energetic" };

        IEnumerator Start()
        {
            while (Session == null || Session.Demo.Store == null) yield return null;
            Session.AdoptFlow();
            Session.EnsureDraft();
            entered = Time.unscaledTime;
            if (splashLetters != null) splashOrigins=splashLetters.Select(r=>r.anchoredPosition).ToArray();
            if (bubble) bubbleOrigin = bubble.rectTransform.anchoredPosition;
            if (email) email.SetTextWithoutNotify(Draft.accountEmail ?? "");
            if (petName) petName.SetTextWithoutNotify(Draft.petName);
            if (breed) { SetOption(breed, Draft.petBreed); breed.onValueChanged.AddListener(_ => CapturePet()); }
            if (colour) { SetOption(colour, Draft.petColour); colour.onValueChanged.AddListener(_ => CapturePet()); }
            if (petName) petName.onValueChanged.AddListener(_ => CapturePet());
            if (page == "ChooseCompanion") RefreshChoice();
            if (page == "PersonalisePet") LoadPhoto();
            string name = page == "MeetPet" ? Draft.petName : Session.Demo.Store.Preferences.petName;
            if (page == "MeetPet") { title.text = "Meet " + name; speech.text = "Hi, I’m\n" + name + "."; SetPet(Draft.petOption); }
            if (page == "CreatingPet" && Draft.petOption != 0) SetPet(Draft.petOption);
            var savedPet=Session.Demo.Store.Preferences;
            Sprite companion=savedPet.petOption==0 && savedPet.petAppearance==1 && goldenPet ? goldenPet : pets[Mathf.Clamp(savedPet.petOption,0,pets.Length-1)];
            bool useMocha=savedPet.petOption==0 && savedPet.petAppearance==0;
            if (page == "MorningTodos") pet.sprite=companion;
            if (!useMocha && (page == "MorningDrink" || page == "MorningStretch" || page == "WakeFeedback"))
            {
                if (video) video.clip=null;
                if (animatedPet) { animationFrames=Array.Empty<Sprite>();animatedPet.sprite=companion; }
                if (pet) { pet.sprite=companion;pet.gameObject.SetActive(true); }
                if (wakeBackground) wakeBackground.sprite=wakeStillScene;
            }
            if (page == "MorningEnd") subtitle.text = name + " will be here whenever\nyou feel like talking.";
            if (page == "WakeFeedback")
            {
                title.text = Record == null ? "A new morning\nwith your companion." : Record.result == "Calm" ? "A quiet moment,\na fresh start." : "Last night had a few\nrestless moments.";
                if (Record == null) { primary.interactable = false; Fail("No completed sleep session. Return Home to begin."); }
            }
            if (rating)
            {
                rating.SetValueWithoutNotify(Record != null && Record.morningRated ? Record.morningRating : 3);
                ratingVisual=rating.value;rating.onValueChanged.AddListener(RenderRating); RenderRating(rating.value);
            }
            if (page == "MorningTodos") RenderTodos();
            if (notify && Record != null) notify.SetIsOnWithoutNotify(Record.notifyLater);
            remaining = stretchSeconds;
            if (page == "MorningStretch") primary.interactable = false;
            if (videoImage) videoImage.enabled = false;
            if (video && video.clip)
            {
                var size = videoImage.rectTransform.sizeDelta;
                videoTexture = new RenderTexture(removeWhiteBackground ? 456 : Mathf.RoundToInt(size.x * 2), removeWhiteBackground ? 340 : Mathf.RoundToInt(size.y * 2), 0);
                video.targetTexture = videoTexture; videoImage.texture = videoTexture;
                videoImage.enabled = false;
                video.errorReceived += VideoError;
                video.prepareCompleted += VideoReady;
                video.Prepare();
            }
            Ready = true;
        }
        void VideoReady(VideoPlayer player) { player.Play(); }
        void VideoError(VideoPlayer player, string message) { videoImage.enabled = false; Debug.LogWarning("Flow video fallback: " + message); }
        void SetOption(Dropdown field, string option)
        {
            int index = field.options.FindIndex(o => o.text == option);
            field.SetValueWithoutNotify(Mathf.Max(0, index));
        }
        void SetPet(int index) { if (pet && pets != null && pets.Length > 0) pet.sprite = pets[Mathf.Clamp(index, 0, pets.Length - 1)]; }
        void Update()
        {
            if (!Ready) return;
            float elapsed = Time.unscaledTime - entered;
            if (content && !leaving) content.alpha = 1;
            if (page == "OnboardingSplash")
            {
                // SleepetSplashMotion evaluates the source CSS's shared four-second timeline.
                if (elapsed >= 4) Go("Account");
            }
            if (page == "CreatingPet")
            {
                for (int i = 0; i < dots.Length; i++) dots[i].color = Mathf.FloorToInt(elapsed * 5) % dots.Length == i ? new Color(.11f,.29f,.46f) : new Color(.8f,.84f,.87f);
                if (elapsed >= 1.6f) Go("MeetPet");
            }
            if (rating)
            {
                float blend=SleepetUIMotion.Bezier((Time.unscaledTime-ratingChanged)/.45f,.25f,.1f,.25f,1);
                if(shade)shade.color=new Color(1,1,1,Mathf.Lerp(shadeFrom,shadeLevels[(int)ratingTarget],blend));
                if(ratingLight)ratingLight.color=new Color(1,1,1,Mathf.Lerp(lightFrom,lightLevels[(int)ratingTarget],blend));
                if(ratingLights!=null)for(int i=0;i<ratingLights.Length;i++){
                    var light=ratingLights[i];float target=ratingTarget>=i+1?1:0;
                    light.color=new Color(1,1,1,Mathf.Lerp(lightsFrom[i],target,SleepetUIMotion.Bezier((Time.unscaledTime-ratingChanged)/.55f,.25f,.1f,.25f,1)));
                }
            }
            if (page == "MorningStretch")
            {
                remaining = Mathf.Max(0, stretchSeconds - elapsed);
                value.text = TimeSpan.FromSeconds(Mathf.Ceil(remaining)).ToString(@"mm\:ss");
                ring.fillAmount = stretchSeconds <= 0 ? 0 : remaining / stretchSeconds;
                primary.interactable = remaining <= 0;
                timerLabel.text = remaining <= 0 ? "WELL DONE" : "STRETCH TIME";
            }
            if (video && video.isPlaying && videoImage)
            {
                videoImage.enabled = true;
                if (videoFallback) videoFallback.SetActive(false);
                if (removeWhiteBackground && Time.unscaledTime >= nextVideoFrame) { nextVideoFrame = Time.unscaledTime + 1f / 15f; KeyVideoFrame(); }
            }
            if (animatedPet && animationFrames != null && animationFrames.Length > 0)
                animatedPet.sprite = animationFrames[Mathf.FloorToInt(elapsed * 9) % animationFrames.Length];
        }
        public void Go(string target) { if (leaving) return; leaving = true; Session.OpenFlow(target); }
        public void Home() { if (Session != null) { Session.Demo.OpenHome(); Session.Navigate(HighFiPage.Home); } }
        public void SubmitAccount()
        {
            if (!ValidateAccount()) return;
            Draft.accountEmail = email.text.Trim(); password.text = ""; Go("ChooseCompanion");
        }
        bool ValidateAccount()
        {
            string address = email.text.Trim();
            try { var parsed = new System.Net.Mail.MailAddress(address); if (parsed.Address != address || !address.Contains(".")) throw new FormatException(); }
            catch { Fail("Please enter a valid email address."); return false; }
            if (string.IsNullOrEmpty(password.text)) { Fail("Please enter your password."); return false; }
            return true;
        }
        public void LoginDemo() { if (ValidateAccount()) { password.text = ""; Home(); } }
        public void SelectPet(int index)
        {
            if (Draft.petOption != index) Draft.petBreed = index == 0 ? "Border Collie" : index == 1 ? "Domestic Shorthair" : "Other";
            Draft.petOption = Mathf.Clamp(index, 0, 2);
            Draft.petSpecies = new[] { "dog", "cat", "other" }[Draft.petOption];
            RefreshChoice();
        }
        void RefreshChoice()
        {
            for (int i = 0; i < selections.Length; i++)
            {
                selections[i].color = i == Draft.petOption ? new Color(.91f,.94f,.98f) : Color.white;
                if (choiceIndicators != null && i < choiceIndicators.Length) choiceIndicators[i].enabled = i == Draft.petOption;
            }
        }
        void CapturePet()
        {
            if (petName) Draft.petName = petName.text.Trim();
            if (breed) Draft.petBreed = breed.options[breed.value].text;
            if (colour) Draft.petColour = colour.options[colour.value].text;
        }
        public void CreatePet()
        {
            CapturePet();
            if (string.IsNullOrWhiteSpace(Draft.petName)) { Fail("Please give your companion a name."); return; }
            Go("CreatingPet");
        }
        public void ConfirmPet()
        {
            var next = Session.Demo.Store.CopyPreferences();
            next.accountEmail = Draft.accountEmail; next.onboardingComplete = true;
            next.petName = Draft.petName; next.petOption = Draft.petOption; next.petSpecies = Draft.petSpecies;
            next.petBreed = Draft.petBreed; next.petColour = Draft.petColour;
            next.petPhoto = Draft.petPhoto; next.petAppearance = 0; next.petPose = 0;
            if (!Session.Demo.Store.SavePreferences(next)) { Fail(Session.Demo.Store.Error); return; }
            Session.MarkStateChanged(); Home();
        }
        public void PickPhoto()
        {
            string path = SleepetPhotoPicker.Pick();
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length > 12 * 1024 * 1024) { Fail("Choose a JPG or PNG smaller than 12 MB."); return; }
                var texture = new Texture2D(2,2);
                if (!texture.LoadImage(bytes)) { Destroy(texture); Fail("Choose a JPG or PNG image."); return; }
                string folder = Path.Combine(Session.Demo.Store.DirectoryPath, "PetPhotos");
                Directory.CreateDirectory(folder);
                string name = Guid.NewGuid().ToString("N") + ".png";
                File.WriteAllBytes(Path.Combine(folder,name), texture.EncodeToPNG()); Destroy(texture);
                Draft.petPhoto = "PetPhotos/" + name; LoadPhoto();
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { Fail("Could not read that image."); }
        }
        void LoadPhoto()
        {
            if (!photo || string.IsNullOrEmpty(Draft.petPhoto)) return;
            string path = Path.GetFullPath(Path.Combine(Session.Demo.Store.DirectoryPath, Draft.petPhoto));
            if (!path.StartsWith(Path.GetFullPath(Session.Demo.Store.DirectoryPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                if (!File.Exists(path)) return;
                if (photoSprite) Destroy(photoSprite); if (photoTexture) Destroy(photoTexture);
                photoTexture = new Texture2D(2,2);
                if (!photoTexture.LoadImage(File.ReadAllBytes(path))) return;
                photoSprite = Sprite.Create(photoTexture,new Rect(0,0,photoTexture.width,photoTexture.height),Vector2.one*.5f);
                photo.sprite = photoSprite; photo.color = Color.white;
            }
            catch (IOException) { Fail("Saved photo unavailable. Choose another image."); }
        }
        public void RenderRating(float number)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(number),0,4);
            value.text = ratings[n];
            ratingTarget=n;
            ratingChanged=Time.unscaledTime;shadeFrom=shade?shade.color.a:0;lightFrom=ratingLight?ratingLight.color.a:0;
            lightsFrom=ratingLights==null?Array.Empty<float>():ratingLights.Select(im=>im.color.a).ToArray();
            if (bubble) bubble.color=Color.white;
        }
        bool Save(Action<SleepSummary> change)
        {
            if (!Session.Demo.Store.SaveMorning(Session.MorningSessionId, change)) { Fail(Session.Demo.Store.Error); return false; }
            Session.MarkStateChanged(); return true;
        }
        public void SubmitRating()
        {
            if (!Save(r => { r.morningRated = true; r.morningRating = Mathf.RoundToInt(rating.value); })) return;
            NextRoutine(false);
        }
        public void DrinkDone() { if (Save(r => r.waterStatus = "done")) NextRoutine(true); }
        public void DrinkSkip() { if (Save(r => r.waterStatus = "skipped")) NextRoutine(true); }
        public void StretchDone() { if (remaining <= 0 && Save(r => r.stretchStatus = "done")) Go("MorningTodos"); }
        public void StretchSkip() { if (Save(r => r.stretchStatus = "skipped")) Go("MorningTodos"); }
        void NextRoutine(bool afterWater)
        {
            var plan = Session.MorningPlan;
            if (plan == null) { Fail("Morning plan could not be loaded. Return Home and check the plan."); return; }
            if (!afterWater && plan.water) Go("MorningDrink");
            else if (plan.stretch) Go("MorningStretch");
            else Go("MorningTodos");
        }
        public void RenderTodos()
        {
            var p = Session.MorningPlan ?? Session.Demo.Store.ReadPlan();
            var events = new List<string>();
            if (p != null)
            {
                p.MigrateEvents();
                if (p.breakfast) events.Add("Make breakfast");
                if (p.groceries) events.Add("Go grocery shopping"); if (p.meeting) events.Add("Group meeting");
                if (p.gym) events.Add("Gym day"); if (p.call) events.Add("Call family or a friend");
                foreach (var item in p.events) if (item.selected) events.Add(item.title);
            }
            if (events.Count == 0) events.Add("A gentle start. Nothing extra planned.");
            if (events.Count > todoLabels.Length && todoLabels.Length > 0)
            {
                var labels = new List<Text>(todoLabels);
                var template = todoLabels[0].transform.parent;
                if (template.name == "Motion visual") template = template.parent;
                while (labels.Count < events.Count)
                {
                    var card = Instantiate(template, template.parent);
                    ((RectTransform)card).anchoredPosition = new Vector2(((RectTransform)template).anchoredPosition.x, -labels.Count * 73);
                    labels.Add(card.GetComponentInChildren<Text>(true));
                }
                todoLabels = labels.ToArray();
            }
            for (int i = 0; i < todoLabels.Length; i++) { var card=todoLabels[i].transform.parent; if(card.name=="Motion visual")card=card.parent;card.gameObject.SetActive(i < events.Count); if (i < events.Count) todoLabels[i].text = events[i]; }
            if (todoLabels.Length > 0) {var card=todoLabels[0].transform.parent;if(card.name=="Motion visual")card=card.parent;((RectTransform)card.parent).sizeDelta = new Vector2(348, Mathf.Max(288, events.Count * 73));}
        }
        public void FinishTodos()
        {
            if (!Save(r => r.notifyLater = notify.isOn)) return;
            Go("MorningEnd");
        }
        public void ShowData()
        {
            if (Record == null) { Fail("No session is available."); return; }
            var r = Record;
            dataText.text = "Your sleep session\n\n" + TimeSpan.FromSeconds(r.durationSeconds).ToString(@"hh\:mm\:ss") + "\n" + r.result + " feedback\n\n" + r.sound + "\n\nBased on app activity, not measured sleep stages.";
            dataPanel.SetActive(true);
        }
        public void CloseData() { dataPanel.SetActive(false); }
        public void FinishMorning()
        {
            if (leaving) return;
            if (!Save(r => { r.morningCompleted = true; r.morningCompletedAt = DateTime.UtcNow.ToString("O"); })) return;
            leaving = true; StartCoroutine(RevealHome());
        }
        IEnumerator RevealHome()
        {
            transition.gameObject.SetActive(true);
            float initial=transition.rectTransform.localScale.x;
            for (float t=0; t<.58f; t+=Time.unscaledDeltaTime) { transition.rectTransform.localScale = Vector3.one * Mathf.Lerp(initial, 15f,SleepetUIMotion.Bezier(t/.56f,.17f,.77f,.25f,1)); yield return null; }
            SleepetRevealOverlay.ContinueAcrossScene(transition);
            Home();
        }
        public void OnBeginDrag(PointerEventData e)
        {
            dragStart=e.position;
            if(page=="MorningEnd" && !leaving){transition.gameObject.SetActive(true);transition.rectTransform.localScale=Vector3.one*.05f;}
        }
        public void OnDrag(PointerEventData e)
        {
            if(page!="MorningEnd" || leaving)return;
            float distance=(e.position.y-dragStart.y)/GetComponentInParent<Canvas>().scaleFactor;
            transition.rectTransform.localScale=Vector3.one*Mathf.Clamp(distance/50f,.05f,6f);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(page!="MorningEnd" || leaving)return;
            Vector2 delta = (e.position-dragStart)/GetComponentInParent<Canvas>().scaleFactor;
            if(delta.y>=60 && delta.y>Mathf.Abs(delta.x))FinishMorning();
            else transition.gameObject.SetActive(false);
        }
        void Fail(string message) { if (error) error.text = message; }

        // Match the source's edge-connected white removal, preserving white fur inside the outline.
        void KeyVideoFrame()
        {
            if (!keyedFrame) keyedFrame = new Texture2D(videoTexture.width,videoTexture.height,TextureFormat.RGBA32,false);
            var previous = RenderTexture.active; RenderTexture.active = videoTexture;
            keyedFrame.ReadPixels(new Rect(0,0,videoTexture.width,videoTexture.height),0,0); RenderTexture.active = previous;
            var pixels = keyedFrame.GetPixels32(); int w=keyedFrame.width,h=keyedFrame.height;
            var seen = new bool[pixels.Length]; var queue = new Queue<int>();
            Action<int> add = index => { if (seen[index]) return; seen[index]=true; var c=pixels[index]; if (Math.Min(c.r,Math.Min(c.g,c.b)) >= 225 && Math.Max(c.r,Math.Max(c.g,c.b))-Math.Min(c.r,Math.Min(c.g,c.b)) <=24) queue.Enqueue(index); };
            for(int x=0;x<w;x++){add(x);add((h-1)*w+x);} for(int y=1;y<h-1;y++){add(y*w);add(y*w+w-1);}
            while(queue.Count>0){int i=queue.Dequeue();pixels[i].a=0;if(i%w>0)add(i-1);if(i%w<w-1)add(i+1);if(i>=w)add(i-w);if(i<pixels.Length-w)add(i+w);}
            for(int y=Mathf.FloorToInt(h*.96f);y<h;y++)for(int x=0;x<w*.19f;x++)pixels[y*w+x].a=0;
            // Drop tiny disconnected compression marks without cutting out enclosed white fur.
            Array.Clear(seen,0,seen.Length);
            var component=new List<int>();
            Action<int> visit=i=>{if(!seen[i] && pixels[i].a>0){seen[i]=true;queue.Enqueue(i);}};
            for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start] || pixels[start].a==0)continue;
                component.Clear();visit(start);
                while(queue.Count>0){int i=queue.Dequeue();component.Add(i);if(i%w>0)visit(i-1);if(i%w<w-1)visit(i+1);if(i>=w)visit(i-w);if(i<pixels.Length-w)visit(i+w);}
                if(component.Count<180)foreach(int i in component)pixels[i].a=0;
            }
            keyedFrame.SetPixels32(pixels);keyedFrame.Apply();videoImage.texture=keyedFrame;
        }
        void OnDestroy()
        {
            if(video){video.Stop();video.targetTexture=null;video.errorReceived-=VideoError;video.prepareCompleted-=VideoReady;}
            if(videoTexture){videoTexture.Release();Destroy(videoTexture);} if(keyedFrame)Destroy(keyedFrame);
            if(photoSprite)Destroy(photoSprite);if(photoTexture)Destroy(photoTexture);
        }
    }
}
