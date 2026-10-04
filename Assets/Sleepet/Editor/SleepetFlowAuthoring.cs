#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Sleepet.Editor
{
    public static class SleepetFlowAuthoring
    {
        const string Art="Assets/Sleepet/Art/Flow/", Scenes="Assets/Sleepet/Scenes/", Home=Scenes+"Sleepet_Home.unity";
        static readonly Color Ink=C("#082B5B"), Light=C("#D9EEF7");
        static Font font;
        static Sprite rounded,circle;
        static Transform root;
        static SleepetFlow flow;
        static bool rebuild;
        public static void RebuildNewScenes() { rebuild=true; try { Build(); } finally { rebuild=false; } }
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex,out var c);return c; }
        [MenuItem("Sleepet/Create Missing Onboarding And Morning Scenes")]
        public static void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateCircle();
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories).Concat(Directory.GetFiles(Art,"*.jpg")))
            {
                var imp=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
                imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.maxTextureSize=2048;
                imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            }
            rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/rounded-panel-22.png");
            circle=S("ui-circle.png");
            foreach(string page in SleepetFlow.PageNames)
            {
                // Existing flow scenes are hand-editable and must not be overwritten by rerunning this command.
                string target=Scenes+"Sleepet_"+page+".unity";
                if(File.Exists(target) && !rebuild) continue;
                var scene=EditorSceneManager.OpenScene(Home);
                var session=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SleepetSceneSession>(true)).Single();
                var existingUI=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SleepetHighFi>(true)).Single();
                var existingPets=existingUI.petOptions;var existingGolden=existingUI.goldenPet;
                foreach(var go in scene.GetRootGameObjects()) if(go!=session.transform.root.gameObject) UnityEngine.Object.DestroyImmediate(go);
                var camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
                camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=Ink;
                new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                var canvas=new GameObject("Editable "+page+" Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(402,874);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                var body=new GameObject("Page · 402 x 874",typeof(RectTransform),typeof(Image),typeof(SleepetFlow));
                body.transform.SetParent(canvas.transform,false);root=body.transform;
                var rect=(RectTransform)root;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(402,874);
                body.GetComponent<Image>().color=Ink;
                flow=body.GetComponent<SleepetFlow>();flow.page=page;
                flow.pets=new[]{AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/home-mocha-transparent.png"),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/pet-cat-option.png"),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/pet-other-option.png")};
                flow.pets=existingPets;flow.goldenPet=existingGolden;
                Image("Blue gradient",0,0,402,874,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sleepet/Art/Figma/setting-gradient-figma.png"));
                flow.content=body.AddComponent<CanvasGroup>();
                switch(page)
                {
                    case "OnboardingSplash":Splash();break;
                    case "Account":Account();break;
                    case "ChooseCompanion":Choose();break;
                    case "PersonalisePet":Personalise();break;
                    case "CreatingPet":Creating();break;
                    case "MeetPet":Meet();break;
                    case "WakeFeedback":Wake();break;
                    case "MorningRating":Rating();break;
                    case "MorningDrink":Drink();break;
                    case "MorningStretch":Stretch();break;
                    case "MorningTodos":Todos();break;
                    case "MorningEnd":End();break;
                }
                flow.error=Text("Feedback",28,page=="Account"?460:818,346,32,"",12,C("#FFF1CE"));
                flow.error.supportRichText=false;
                if(page!="OnboardingSplash") Button("Return Home",316,15,72,28,"Home",flow.Home,12,new Color(0,0,0,.15f),Color.white);
                EditorSceneManager.SaveScene(scene,target);
            }
            var home=EditorSceneManager.OpenScene(Home);
            var ui=home.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SleepetHighFi>(true)).Single();
            root=ui.pages[0].transform;
            if(!root.Find("Registration entry")) Button("Registration entry",307,48,76,27,"Register",ui.OpenRegistration,12,new Color(.03f,.16f,.32f,.75f),Color.white);
            EditorSceneManager.SaveScene(home);
            var existing=EditorBuildSettings.scenes.ToList();
            foreach(var page in SleepetFlow.PageNames)
            { string path=Scenes+"Sleepet_"+page+".unity";if(!existing.Any(s=>s.path==path))existing.Add(new EditorBuildSettingsScene(path,true)); }
            EditorBuildSettings.scenes=existing.ToArray();AssetDatabase.SaveAssets();
            Debug.Log("SLEEPET_FLOW_AUTHORING_SUCCESS: 12 independent editable flow scenes. Existing page layouts retained.");
        }
        static void CreateCircle()
        {
            if(File.Exists(Art+"ui-circle.png"))return;
            var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(63.5f-Vector2.Distance(new Vector2(x,y),new Vector2(63.5f,63.5f)))));
            tex.Apply();File.WriteAllBytes(Art+"ui-circle.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name);
        static RectTransform Rect(Transform t,float x,float y,float w,float h)
        {
            var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static GameObject Node(string name,float x,float y,float w,float h,Transform parent=null)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent?parent:root,false);Rect(go.transform,x,y,w,h);return go;}
        static Image Image(string name,float x,float y,float w,float h,Sprite sprite=null,Color? color=null,Transform parent=null)
        {
            var im=Node(name,x,y,w,h,parent).AddComponent<Image>();im.sprite=sprite;im.color=color??Color.white;im.raycastTarget=false;return im;
        }
        static Image Panel(string name,float x,float y,float w,float h,Color color,Transform parent=null)
        {var im=Image(name,x,y,w,h,rounded,color,parent);im.type=UnityEngine.UI.Image.Type.Sliced;return im;}
        static Text Text(string name,float x,float y,float w,float h,string value,int size=20,Color? color=null,Transform parent=null)
        {
            var text=Node(name,x,y,w,h,parent).AddComponent<Text>();text.font=font;text.fontSize=size;text.color=color??Color.white;
            text.alignment=TextAnchor.MiddleCenter;text.text=value;text.raycastTarget=false;text.supportRichText=false;return text;
        }
        static Button Button(string name,float x,float y,float w,float h,string label,UnityAction action,int size=22,Color? bg=null,Color? fg=null,Transform parent=null)
        {
            var im=Panel(name,x,y,w,h,bg??Light,parent);im.raycastTarget=true;
            var button=im.gameObject.AddComponent<Button>();button.targetGraphic=im;
            Text("Label",4,0,w-8,h,label,size,fg??Ink,im.transform);
            if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);return button;
        }
        static Button Link(string name,float y,string label,UnityAction action)=>Button(name,81,y,240,34,label,action,16,Color.clear,Color.white);
        static Button GoButton(string name,float x,float y,float w,float h,string label,string destination)
        {var b=Button(name,x,y,w,h,label,null);UnityEventTools.AddStringPersistentListener(b.onClick,flow.Go,destination);return b;}
        static InputField Input(string name,float y,string label,bool secret=false)
        {
            Text(name+" caption",28,y-33,346,28,label,21,Color.white).alignment=TextAnchor.MiddleLeft;
            var im=Panel(name,28,y,346,56,Color.white);im.raycastTarget=true;
            var field=im.gameObject.AddComponent<InputField>();field.targetGraphic=im;
            field.textComponent=Text("Value",16,0,314,56,"",20,Ink,im.transform);field.textComponent.alignment=TextAnchor.MiddleLeft;
            field.placeholder=Text("Placeholder",16,0,314,56,secret?"Password":name=="Email"?"you@example.com":"Mocha",20,Color.gray,im.transform);
            ((Text)field.placeholder).alignment=TextAnchor.MiddleLeft;
            field.contentType=secret?InputField.ContentType.Password:name=="Email"?InputField.ContentType.EmailAddress:InputField.ContentType.Standard;
            field.characterLimit=secret?128:name=="Email"?254:20;return field;
        }
        static Dropdown Dropdown(string name,float y,string[] options)
        {
            Text(name+" caption",28,y-33,346,28,name,21,Ink).alignment=TextAnchor.MiddleLeft;
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources { standard=rounded,background=rounded,checkmark=circle });
            go.name=name;go.transform.SetParent(root,false);Rect(go.transform,28,y,346,56);
            var d=go.GetComponent<Dropdown>();d.ClearOptions();d.AddOptions(options.ToList());
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=font;text.fontSize=20;text.color=Ink;}
            d.GetComponent<Image>().color=Color.white;Image("Chevron",308,22,18,12,S("select-chevron.png"),null,go.transform);return d;
        }
        static void Step(string count,string heading,string previous)
        {var back=GoButton("Back",18,51,40,40,"",previous);back.GetComponent<Image>().color=Color.clear;Image("Back icon",6,6,28,28,S("back-step.png"),null,back.transform);Text("Step",142,58,118,30,count,22,Light);flow.title=Text("Title",20,126,362,70,heading,29,Ink);flow.title.fontStyle=FontStyle.Bold;}
        static void Splash()
        {
            Image("Symbol",153,218,96,136,S("symbol.png"));
            string[] letters={"s","l","e-1","e-2","p","e-3","t"};flow.splashLetters=new RectTransform[7];
            float[] widths={45,14,45,45,45,45,30};float left=47;
            for(int i=0;i<7;i++){var letter=Image("Letter "+letters[i],left,370,widths[i],62,S("letter-"+letters[i]+".png"));letter.preserveAspect=true;flow.splashLetters[i]=letter.rectTransform;left+=widths[i]+2;}
            Text("Welcome",28,500,346,60,"A quiet companion is on the way.",21);
        }
        static void Account()
        {
            Text("Brand",28,86,346,85,"Sleepet",58);flow.title=Text("Welcome",28,192,346,60,"Welcome",34,Ink);
            flow.email=Input("Email",294,"Email");flow.password=Input("Password",403,"Password",true);
            flow.primary=Button("Create account",28,486,346,64,"Create account",flow.SubmitAccount,25,C("#1D4975"),Color.white);
            Image("Divider left",28,586,147,1,null,Light);Text("Or",175,568,52,34,"or",22,Ink);Image("Divider right",227,586,147,1,null,Light);
            Button("Log in",28,625,346,64,"Log in",flow.LoginDemo,25,Color.white,Ink);
            Text("Terms",28,725,346,50,"By continuing, you agree to our\nTerms and Privacy Policy.",15,C("#B8CADD"));
            Text("Local demo",28,790,346,24,"Local prototype · no online account is created",11,Light);
        }
        static void Choose()
        {
            Step("1 of 3","Choose your companion","Account");Text("Subtitle",28,197,346,34,"You can change this later.",22,Ink);
            flow.selections=new Image[3];flow.choiceIndicators=new Image[3];string[] labels={"Dog","Cat","Other"};
            for(int i=0;i<3;i++)
            {
                var b=Button("Choose "+labels[i],28,267+i*134,346,110,"",null);
                flow.selections[i]=b.GetComponent<Image>();UnityEventTools.AddIntPersistentListener(b.onClick,flow.SelectPet,i);
                if(i<2)Image("Icon",23,26,64,60,S(i==0?"dog-icon.png":"cat-icon.png"),null,b.transform);
                else Text("Other icon",23,15,64,75,"?",48,Ink,b.transform);
                Text("Pet type",110,24,142,62,labels[i],28,Ink,b.transform);
                Image("Choice ring",285,38,35,35,S("radio-unselected.png"),null,b.transform);
                flow.choiceIndicators[i]=Image("Selected",285,38,35,35,S("radio-selected.png"),null,b.transform);
            }
            GoButton("Continue",28,713,346,64,"Continue","PersonalisePet");
        }
        static void Personalise()
        {
            Step("2 of 3","Personalise your pet","ChooseCompanion");
            flow.petName=Input("Pet name",231,"Name");
            flow.breed=Dropdown("Breed",343,new[]{"Border Collie","Labrador","Golden Retriever","Mixed breed","Domestic Shorthair","Other"});
            flow.colour=Dropdown("Colour",455,new[]{"Black & white","Brown","White","Grey"});
            Button("Add photo",28,546,346,95,"Add a photo",flow.PickPhoto,23,Color.white,Ink);
            flow.photo=Image("Photo preview",44,559,62,68,S("camera.png"));flow.photo.preserveAspect=true;
            Text("Photo note",20,650,362,40,"Optional – create a pet inspired by your own.",13,Light);
            flow.primary=Button("Create my Sleepet",28,713,346,64,"Create my Sleepet",flow.CreatePet,24);
        }
        static void Creating()
        {
            flow.title=Text("Creating title",18,130,366,70,"Creating your Sleepet...",28,Ink);
            flow.dots=new Image[4];for(int i=0;i<4;i++)flow.dots[i]=Image("Loading dot "+i,106+i*52,235,22,22,circle);
            flow.pet=Image("Creating pet",91,326,220,244,S("creating-dog.png"));flow.pet.preserveAspect=true;
            Text("Reassurance",28,637,346,85,"A quiet companion\nis on the way.",26,Ink);
        }
        static void Meet()
        {
            Text("Step",142,58,118,30,"3 of 3",22,Light);flow.title=Text("Pet name heading",28,124,346,68,"Meet Mocha",34,Ink);
            flow.pet=Image("Companion",67,278,252,315,S("meet-pet.png"));flow.pet.preserveAspect=true;
            Panel("Speech bubble",237,220,136,120,Color.white);flow.speech=Text("Pet introduction",246,227,118,102,"Hi, I’m\nMocha.",24,Ink);
            Text("Question",20,567,362,50,"Keep this companion?",25,Ink);
            flow.primary=Button("Confirm companion",28,713,346,64,"Yes, continue",flow.ConfirmPet,24);
            var change=GoButton("Change companion",81,780,240,34,"Change","PersonalisePet");change.GetComponent<Image>().color=Color.clear;change.GetComponentInChildren<Text>().color=Light;
        }
        static void Background(string file)=>Image("Morning landscape",0,0,402,874,S(file));
        static void Video(string file,float x,float y,float w,float h,bool loop=true,bool key=false)
        {
            var go=Node("Animated landscape or companion",x,y,w,h);flow.videoImage=go.AddComponent<RawImage>();flow.videoImage.raycastTarget=false;
            flow.video=go.AddComponent<VideoPlayer>();flow.video.playOnAwake=false;flow.video.isLooping=loop;flow.video.audioOutputMode=VideoAudioOutputMode.None;
            flow.video.renderMode=VideoRenderMode.RenderTexture;flow.video.clip=AssetDatabase.LoadAssetAtPath<VideoClip>(Art+file);flow.video.playbackSpeed=.75f;
            flow.video.aspectRatio=w==402?VideoAspectRatio.FitOutside:VideoAspectRatio.FitInside;
            flow.removeWhiteBackground=key;go.SetActive(false);
            // Keep the player active to prepare while hiding the raw image until its first frame.
            go.SetActive(true);flow.videoImage.color=Color.white;
        }
        static void Wake()
        {
            flow.wakeBackground=Image("Morning landscape",0,0,402,874,S("wake-medium-poster.jpg"));flow.wakeStillScene=S("wake-landscape.png");Video("wake-medium-video.mp4",0,0,402,874);
            flow.pet=Image("Saved companion",58,466,275,266,flow.pets[0]);flow.pet.preserveAspect=true;flow.pet.gameObject.SetActive(false);
            Text("Eyebrow",28,54,346,24,"MORNING FEEDBACK",14);
            flow.title=Text("Night feedback",39,91,324,78,"Last night had a few\nrestless moments.",28,Ink);
            Text("Reassurance",28,178,346,40,"Let’s find our rhythm together.",18,C("#404040"));
            flow.primary=GoButton("Wake up",75,770,262,43,"Wake up","MorningRating");
            Button("Sleep data",36,820,330,32,"Check last night’s sleep data",flow.ShowData,15,Color.clear,Color.white);
            var panel=Panel("Sleep data panel",28,238,346,432,C("#E8F4FD"));flow.dataPanel=panel.gameObject;
            flow.dataText=Text("Saved session summary",20,25,306,300,"",20,Ink,panel.transform);
            Button("Close data",50,344,246,52,"Back to morning",flow.CloseData,20,Ink,Color.white,panel.transform);panel.gameObject.SetActive(false);
        }
        static void Rating()
        {
            Background("rating-background.png");flow.title=Text("Question",20,112,362,40,"How do you feel this morning?",22);
            Text("Subtitle",20,152,362,24,"Take a moment to record how rested you feel.",13);
            flow.bubble=Image("Glass bubble",71,235,261,261,S("rating-glass-bubble.png"));
            flow.shade=Image("Rating shade",80,244,243,243,circle,new Color(0,0,0,.2f));
            flow.ratingLights=new Image[4];
            for(int i=0;i<4;i++)flow.ratingLights[i]=Image("Light effects "+(i+1),81,245,241,241,S("rating-effects-"+(i+1)+".png"));
            flow.value=Text("Rating label",35,516,332,40,"Refreshed",22);
            var go=DefaultControls.CreateSlider(new DefaultControls.Resources{background=rounded,standard=rounded,knob=circle});
            go.name="Rested rating";go.transform.SetParent(root,false);Rect(go.transform,35,575,334,43);
            flow.rating=go.GetComponent<Slider>();flow.rating.minValue=0;flow.rating.maxValue=4;flow.rating.wholeNumbers=true;flow.rating.value=3;
            var track=go.transform.Find("Background").GetComponent<Image>();track.color=new Color(1,1,1,.27f);track.rectTransform.anchorMin=Vector2.zero;track.rectTransform.anchorMax=Vector2.one;track.rectTransform.offsetMin=track.rectTransform.offsetMax=Vector2.zero;
            flow.rating.fillRect.parent.gameObject.SetActive(false);
            var area=(RectTransform)flow.rating.handleRect.parent;area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;area.offsetMin=new Vector2(21,0);area.offsetMax=new Vector2(-21,0);
            flow.rating.handleRect.sizeDelta=new Vector2(42,0);flow.rating.handleRect.GetComponent<Image>().color=new Color(1,1,1,.75f);
            Image("Five rating stops",18,19,298,6,S("rating-slider-dots.png"),null,go.transform).transform.SetSiblingIndex(1);
            Text("Low",35,632,130,30,"Exhausted",14);Text("High",237,632,130,30,"Energetic",14);
            flow.primary=Button("Done",81,732,240,43,"Done",flow.SubmitRating,22,C("#2F4263"),Color.white);
        }
        static void Progress(int step)
        {
            Text("Morning routine",80,42,242,32,"Morning routine.",18);
            for(int i=0;i<3;i++)Panel("Progress "+i,142+i*38,78,32,4,i==step?C("#FAC79B"):C("#7086B4"));
        }
        static void Drink()
        {
            Background("drink-blurred.png");Progress(0);
            flow.bubble=Image("Drink glass bubble",65,137,261,261,S("drink-bubble.png"));Image("Water glass",116,173,170,189,S("drink-glass.png")).preserveAspect=true;
            flow.title=Text("Instruction",20,437,362,45,"Drink a glass of water",24);
            flow.animatedPet=Image("Animated companion",46,512,310,178,S("drink-mocha-poster.png"));flow.animatedPet.preserveAspect=true;
            flow.animationFrames=Directory.GetFiles(Art+"DrinkFrames","*.png").OrderBy(p=>p).Select(p=>AssetDatabase.LoadAssetAtPath<Sprite>(p.Replace('\\','/'))).ToArray();
            flow.primary=Button("Done",81,732,240,43,"Done",flow.DrinkDone,22,C("#2F4263"),Color.white);
            Link("Skip",785,"Skip for now",flow.DrinkSkip);
        }
        static void Stretch()
        {
            Background("stretch-blurred.png");Progress(1);
            flow.bubble=Image("Stretch bubble",79,144,249,249,S("stretch-bubble.png"));
            Image("Ring track",97,163,214,214,S("stretch-ring-base.png"));
            flow.ring=Image("Ring progress",97,163,214,214,S("stretch-ring-progress.png"));flow.ring.type=UnityEngine.UI.Image.Type.Filled;flow.ring.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;flow.ring.fillOrigin=2;
            flow.value=Text("Remaining time",125,221,156,67,"05:00",48);flow.timerLabel=Text("Timer caption",115,288,180,28,"STRETCH TIME",16);
            flow.title=Text("Instruction",20,437,362,45,"Stretch for 5 minutes",24);
            flow.pet=Image("Pet fallback",72,505,260,190,flow.pets[0]);flow.pet.preserveAspect=true;flow.videoFallback=flow.pet.gameObject;
            Video("stretch-mocha-video.mp4",49,486,304,226,true,true);
            flow.primary=Button("Done",81,732,240,43,"Done",flow.StretchDone,22,C("#2F4263"),Color.white);
            flow.primary.interactable=false;Link("Skip",785,"Skip for now",flow.StretchSkip);
        }
        static void Todos()
        {
            Background("todos-blurred.png");Progress(2);flow.title=Text("Title",38,153,326,52,"Don’t forget to...",24);
            Image("Timeline",38,215,5,385,null,C("#9CB8F1"));
            var viewport=Node("Todo scroll viewport",27,212,348,288);viewport.AddComponent<RectMask2D>();var scroll=viewport.AddComponent<ScrollRect>();
            var content=Node("Editable task cards",0,0,348,440,viewport.transform);scroll.viewport=(RectTransform)viewport.transform;scroll.content=(RectTransform)content.transform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            flow.todoLabels=new Text[6];
            for(int i=0;i<6;i++)
            {
                var card=Panel("Todo "+(i+1),31,i*73,304,64,new Color(.04f,.17f,.35f,.88f),content.transform);
                flow.todoLabels[i]=Text("Task",15,3,274,58,i==0?"Go grocery shopping":i==1?"Group meeting":"",18,Color.white,card.transform);
                flow.todoLabels[i].alignment=TextAnchor.MiddleLeft;
            }
            flow.pet=Image("Companion",60,520,133,175,S("todos-pet.png"));flow.pet.preserveAspect=true;
            var t=DefaultControls.CreateToggle(new DefaultControls.Resources{standard=rounded,checkmark=circle});t.name="Notify later preference";t.transform.SetParent(root,false);Rect(t.transform,72,710,290,30);
            flow.notify=t.GetComponent<Toggle>();var label=t.GetComponentInChildren<Text>();label.font=font;label.text="Send me notifications later";label.fontSize=16;label.color=Color.white;
            flow.primary=Button("Thanks",60,756,282,43,"Thanks for reminding",flow.FinishTodos,21,C("#2F4263"),Color.white);
        }
        static void End()
        {
            Background("end-background.png");Video("end-landscape.mp4",0,0,402,874,false);
            flow.bubble=Image("End bubble",80,236,261,261,S("end-bubble.png"));
            flow.title=Text("Title",94,340,234,40,"Let’s ease into the day.",18);
            flow.subtitle=Text("Reassurance",100,387,225,58,"Mocha will be here whenever\nyou feel like talking.",13);
            flow.primary=Button("Swipe or continue",58,748,286,82,"⌃\nSwipe up to continue",flow.FinishMorning,20,Color.clear,Color.white);
            flow.transition=Image("Circle transition",101,670,200,200,circle,new Color(.8f,.9f,1,.94f));
            flow.transition.rectTransform.pivot=Vector2.one*.5f;flow.transition.gameObject.SetActive(false);
        }
    }
}
#endif
