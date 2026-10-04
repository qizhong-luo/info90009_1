#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet.Editor
{
    // Patches existing serialized controls; never rebuilds a scene or changes backend bindings.
    public static class SleepetWebParityAuthoring
    {
        const string Art="Assets/Sleepet/Art/Flow/";
        static Font regular,bold,italic;
        static Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
        static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        [MenuItem("Sleepet/Match Frontend Typography Motion And Alignment")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Art,"web-*.png")){
                var imp=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
                imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            }
            regular=AssetDatabase.LoadAssetAtPath<Font>("Assets/Sleepet/Art/Fonts/Arial.ttf");
            bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/Sleepet/Art/Fonts/Arial-Bold.ttf");
            italic=AssetDatabase.LoadAssetAtPath<Font>("Assets/Sleepet/Art/Fonts/Arial-Italic.ttf");
            if(!regular||!bold||!italic)throw new InvalidOperationException("Frontend fallback font files missing");
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
                var scene=EditorSceneManager.OpenScene(entry.path);
                var roots=scene.GetRootGameObjects();
                var flow=roots.SelectMany(r=>r.GetComponentsInChildren<SleepetFlow>(true)).FirstOrDefault();
                if(flow){
                    PolishFlow(flow);
                    var splash=flow.GetComponent<SleepetSplashMotion>();
                    if(splash)foreach(var binding in splash.bindings.Where(b=>b.target.name!="Account preview")){
                        var r=binding.target;if(r.pivot==new Vector2(0,1)){r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(r.sizeDelta.x/2,-r.sizeDelta.y/2);}
                    }
                    if(splash&&!flow.transform.Find("Splash background fade")){
                        var blackout=AddImage(flow.transform,"Splash background fade",0,0,402,874,null);blackout.color=C("#001B4A");blackout.transform.SetSiblingIndex(1);blackout.gameObject.AddComponent<CanvasGroup>();
                        splash.bindings=splash.bindings.Concat(new[]{new SleepetSplashMotion.Binding{target=blackout.rectTransform,tracks=new[]{"splash-background"}}}).ToArray();
                    }
                    if(flow.page=="MorningRating"&&!flow.bubble.GetComponent<Mask>())flow.bubble.gameObject.AddComponent<Mask>().showMaskGraphic=true;
                    if(flow.page=="MorningTodos")flow.transform.Find("Timeline/Motion visual").GetComponent<SleepetUIMotion>().revealScaleY=true;
                }
                var ui=roots.SelectMany(r=>r.GetComponentsInChildren<SleepetHighFi>(true)).FirstOrDefault();
                if(ui)AlignNavigation(ui);
                foreach(var text in roots.SelectMany(r=>r.GetComponentsInChildren<Text>(true))){
                    text.font=text.fontStyle==FontStyle.Bold||text.fontStyle==FontStyle.BoldAndItalic||(text.font&&text.font.name.Contains("Bold"))?bold:text.fontStyle==FontStyle.Italic||(text.font&&text.font.name.Contains("Italic"))?italic:regular;
                    text.fontStyle=FontStyle.Normal;
                }
                foreach(var canvas in roots.SelectMany(r=>r.GetComponentsInChildren<CanvasScaler>(true))){canvas.referenceResolution=new Vector2(402,874);canvas.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;canvas.matchWidthOrHeight=0;}
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_Home.unity");
            AssetDatabase.SaveAssets();Debug.Log("WEB_PARITY_AUTHORING_SUCCESS");
        }
        static void R(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Transform Find(Transform root,string name){var direct=root.Find(name);return direct?direct:root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);}
        static Image AddImage(Transform parent,string name,float x,float y,float w,float h,Sprite sprite){
            var found=parent.Find(name);var go=found?found.gameObject:new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var im=go.GetComponent<Image>();R(im.rectTransform,x,y,w,h);im.sprite=sprite;im.raycastTarget=false;return im;
        }
        static void Move(Transform root,string name,float x,float y,float w,float h){var t=Find(root,name);if(t)R((RectTransform)t,x,y,w,h);}
        static void Label(Transform root,string name,int size,bool heavy=false,Color? color=null){var t=Find(root,name)?.GetComponent<Text>();if(!t)return;t.fontSize=size;t.fontStyle=heavy?FontStyle.Bold:FontStyle.Normal;if(color.HasValue)t.color=color.Value;}
        static Transform Motion(Transform root,string name,float delay,float duration=.48f,float offset=12,SleepetUIMotion.Loop loop=SleepetUIMotion.Loop.None,float initial=0,bool enter=true){
            var target=root.Find(name);if(!target)return null;
            if(target.Find("Motion visual"))return target;
            var rect=(RectTransform)target;var wrapper=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            wrapper.SetParent(root,false);wrapper.SetSiblingIndex(target.GetSiblingIndex());
            wrapper.anchorMin=rect.anchorMin;wrapper.anchorMax=rect.anchorMax;wrapper.pivot=rect.pivot;wrapper.anchoredPosition=rect.anchoredPosition;wrapper.sizeDelta=rect.sizeDelta;
            target.SetParent(wrapper,false);target.name="Motion visual";R(rect,0,0,wrapper.sizeDelta.x,wrapper.sizeDelta.y);
            if(loop==SleepetUIMotion.Loop.Glass){rect.pivot=new Vector2(.5f,.15f);rect.anchoredPosition=new Vector2(rect.sizeDelta.x*.5f,-rect.sizeDelta.y*.85f);}
            var group=target.GetComponent<CanvasGroup>();if(!group)group=target.gameObject.AddComponent<CanvasGroup>();group.alpha=1;
            var motion=target.gameObject.AddComponent<SleepetUIMotion>();motion.delay=delay;motion.duration=duration;motion.enterOffset=offset;motion.loop=loop;motion.initialAlpha=initial;motion.enter=enter;
            return wrapper;
        }
        static void AlignNavigation(SleepetHighFi ui){
            if(!ui.bottomNavigation)return;
            // Centers are shared across Home, Me and every report scene, independent of icon bounds.
            for(int i=0;i<3;i++){
                var highlight=ui.navHighlights[i];R(highlight.rectTransform,23+i*120,5,112,56);
                var hit=highlight.GetComponentInChildren<Button>(true);
                if(hit&&hit.transform!=highlight.transform)R((RectTransform)hit.transform,0,0,112,56);
                var icon=ui.navIcons[i];if(icon){var r=icon.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(30,30);}
            }
        }
        static void PolishFlow(SleepetFlow f){
            var root=f.transform;
            if(root.Find("Web parity applied"))return;
            if(f.page=="OnboardingSplash")Splash(f);
            if(f.page=="Account"){
                Label(root,"Brand",58,true);Label(root,"Welcome",34,true,C("#292E36"));Label(root,"Terms",17,false,C("#747D88"));
                foreach(var field in new[]{f.email,f.password}){field.textComponent.fontSize=22;((Text)field.placeholder).fontSize=22;}
            }
            if(f.page=="ChooseCompanion"||f.page=="PersonalisePet"){
                Move(root,"Step",130,62,142,28);Move(root,"Title",24,130,354,38);Label(root,"Title",30,true,C("#292E36"));
                if(f.page=="ChooseCompanion")Move(root,"Subtitle",24,183,354,28);
                if(f.page=="PersonalisePet"){Label(root,"Pet name caption",21,false,C("#292E36"));Label(root,"Photo note",16,false,C("#747D88"));}
            }
            if(f.page=="CreatingPet"){
                Move(root,"Creating title",0,138,402,38);Label(root,"Creating title",29,true,C("#292E36"));
                Motion(root,"Creating pet",0,.48f,0,SleepetUIMotion.Loop.Puppy,1,false);
            }
            if(f.page=="MeetPet"){
                Move(root,"Pet name heading",28,130,346,43);Label(root,"Pet name heading",34,true,C("#292E36"));
                Move(root,"Companion",75,269,209,258);
                var bubble=Find(root,"Speech bubble").GetComponent<Image>();bubble.sprite=S("meet-speech");bubble.type=Image.Type.Simple;R(bubble.rectTransform,242,211,130,116);
                Move(root,"Pet introduction",263,234,100,65);Label(root,"Pet introduction",24,true,C("#292E36"));
            }
            bool morning=f.page.StartsWith("Morning")||f.page=="WakeFeedback";
            if(morning){
                foreach(var b in root.GetComponentsInChildren<Button>(true).Where(b=>b.name=="Done"||b.name=="Thanks"||b.name=="Wake up")){
                    var outline=b.GetComponent<Outline>();if(!outline)outline=b.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,1,1,.85f);outline.effectDistance=new Vector2(.7f,-.7f);
                    var shadow=b.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,.45f,1,.45f);shadow.effectDistance=new Vector2(0,-2);
                }
                foreach(string bg in new[]{"Morning landscape","Animated landscape or companion"}){
                    if(f.page=="MorningStretch"&&bg.StartsWith("Animated"))continue;
                    Motion(root,bg,0,.5f,0,SleepetUIMotion.Loop.None,.68f);
                }
            }
            if(f.page=="WakeFeedback"){
                f.primary.GetComponent<Image>().color=new Color(.012f,.27f,.47f,.2f);f.primary.GetComponentInChildren<Text>().color=Color.white;
                Motion(root,"Eyebrow",.03f);Motion(root,"Night feedback",.1f);Motion(root,"Reassurance",.17f);Motion(root,"Wake up",.7f);Motion(root,"Sleep data",.82f);
            }
            if(f.page=="MorningRating"){
                f.shade.sprite=S("web-rating-shade");f.shade.color=new Color(1,1,1,.18f);
                f.ratingLight=AddImage(root,"Rating light",80,244,243,243,S("web-rating-light"));f.ratingLight.transform.SetSiblingIndex(f.shade.transform.GetSiblingIndex()+1);
                // Put all effects inside the moving bubble, so their contours never separate.
                foreach(var im in new[]{f.shade,f.ratingLight}.Concat(f.ratingLights)){
                    var r=im.rectTransform;var p=r.anchoredPosition-f.bubble.rectTransform.anchoredPosition;r.SetParent(f.bubble.transform,false);r.anchoredPosition=p;
                }
                Motion(root,"Glass bubble",.26f,.52f,0,SleepetUIMotion.Loop.Bubble);
                Motion(root,"Question",.03f);Motion(root,"Subtitle",.1f);Motion(root,"Rating label",.48f);Motion(root,"Rested rating",.58f);Motion(root,"Low",.66f);Motion(root,"High",.66f);Motion(root,"Done",.81f);
            }
            if(f.page=="MorningDrink"||f.page=="MorningStretch"){
                bool drink=f.page=="MorningDrink";
                Motion(root,"Morning routine",.03f);for(int i=0;i<3;i++)Motion(root,"Progress "+i,.1f);
                Motion(root,"Instruction",drink?.5f:.52f);Motion(root,"Done",drink?.78f:.79f);Motion(root,"Skip",.88f);
                if(drink){
                    var glow=AddImage(root,"Glass warm light",65,137,261,261,S("web-drink-glow"));glow.transform.SetSiblingIndex(f.bubble.transform.GetSiblingIndex()+1);
                    var glass=Find(root,"Water glass").GetComponent<Image>();glass.sprite=S("web-glass-crop");R(glass.rectTransform,131,173,111,189);
                    Motion(root,"Drink glass bubble",.25f,.52f,0);Motion(root,"Glass warm light",.25f,.52f,0);Motion(root,"Water glass",.31f,.52f,0,SleepetUIMotion.Loop.Glass);
                    Motion(root,"Animated companion",.62f,.52f,0);
                }else{
                    AddImage(root,"Timer end point",189,154,25,24,S("stretch-ring-dot"));
                    Motion(root,"Stretch bubble",.23f,.52f,0);foreach(string n in new[]{"Ring track","Ring progress","Timer end point"})Motion(root,n,.27f,.52f,0);
                    Motion(root,"Remaining time",.4f);Motion(root,"Timer caption",.44f);Motion(root,"Animated landscape or companion",.63f,.52f,0);Motion(root,"Pet fallback",.63f,.52f,0);
                }
            }
            if(f.page=="MorningTodos"){
                Move(root,"Title",58,166,326,34);f.title.alignment=TextAnchor.MiddleLeft;
                Move(root,"Notify later preference",91,705,270,28);Move(root,"Thanks",66,750,288,43);
                Motion(root,"Morning routine",.03f,.42f);for(int i=0;i<3;i++)Motion(root,"Progress "+i,.1f,.42f);
                Motion(root,"Title",.17f,.46f);Motion(root,"Timeline",.22f,.8f,0);
                var list=Find(root,"Editable task cards");for(int i=0;i<6;i++)Motion(list,"Todo "+(i+1),.28f+i*.11f);
                Motion(root,"Companion",.74f,.52f);Motion(root,"Notify later preference",.87f);Motion(root,"Thanks",.97f,.5f);
            }
            if(f.page=="MorningEnd"){
                var glow=AddImage(root,"End warm light",80,236,261,261,S("web-end-glow"));glow.transform.SetSiblingIndex(f.bubble.transform.GetSiblingIndex()+1);
                f.primary.GetComponentInChildren<Text>().text="Swipe up to continue";
                var label=f.primary.GetComponentInChildren<Text>();R(label.rectTransform,0,35,286,28);
                var arrow=AddImage(f.primary.transform,"Up arrows",131,5,24,23,S("web-up-arrows"));Motion(f.primary.transform,"Up arrows",0,.48f,0,SleepetUIMotion.Loop.Arrow,1,false);
                Motion(root,"End bubble",.26f,.52f,0);Motion(root,"End warm light",.26f,.52f,0);Motion(root,"Title",.41f);Motion(root,"Reassurance",.52f);Motion(root,"Swipe or continue",.78f);
                f.transition.sprite=S("web-reveal");f.transition.color=Color.white;
            }
            var canvas=f.GetComponentInParent<Canvas>();var layout=canvas.GetComponent<SleepetFlowLayout>();if(!layout)layout=canvas.gameObject.AddComponent<SleepetFlowLayout>();layout.page=(RectTransform)root;
            var backgroundNames=new List<string>{"Blue gradient","Morning landscape"};if(f.page=="WakeFeedback"||f.page=="MorningEnd")backgroundNames.Add("Animated landscape or companion");
            layout.backgrounds=backgroundNames.Select(n=>root.Find(n) as RectTransform).Where(r=>r).ToArray();
            var footerNames=new[]{"Continue","Create my Sleepet","Confirm companion","Change companion","Wake up","Sleep data","Done","Skip","Thanks","Notify later preference","Swipe or continue","Feedback","Local demo"};
            layout.footers=footerNames.Select(n=>root.Find(n) as RectTransform).Where(r=>r).Select(r=>new SleepetFlowLayout.Footer{rect=r,bottom=874+r.anchoredPosition.y-r.sizeDelta.y}).ToArray();
            new GameObject("Web parity applied").transform.SetParent(root,false);
        }
        static void Splash(SleepetFlow f){
            var root=f.transform;var welcome=root.Find("Welcome");if(welcome)UnityEngine.Object.DestroyImmediate(welcome.gameObject);
            var symbol=(RectTransform)root.Find("Symbol");R(symbol,100.58f,264.82f,177.68f,269.28f);symbol.gameObject.AddComponent<CanvasGroup>();
            var bindings=new List<SleepetSplashMotion.Binding>{new SleepetSplashMotion.Binding{target=symbol,tracks=new[]{"splash-symbol-opacity","splash-symbol-translate","splash-symbol-scale"}}};
            float[] xs={44.98f,102.83f,124.14f,174.55f,226.81f,277.98f,325.9f},ys={547.04f,547.04f,560.76f,560.76f,560.85f,560.76f,548.17f},ws={47.6f,11.14f,42.37f,42.37f,43.58f,42.37f,30.11f},hs={51.65f,51.65f,37.93f,37.93f,51.65f,37.93f,50.52f},exits={39.244f,26.073f,13.633f,0,-15.175f,-28.215f,-37.922f};
            string[] names={"standard","l","e1","e2","standard-heightless","e3","standard"};
            for(int i=0;i<7;i++){
                var r=f.splashLetters[i];R(r,xs[i],ys[i],ws[i],hs[i]);var tracks=new List<string>{"splash-letter-"+names[i]+"-translate","splash-letter-scale"};
                if(i==1||i==2||i==3||i==5)tracks.Add("splash-letter-"+names[i]+"-height");r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(ws[i]/2,-hs[i]/2);bindings.Add(new SleepetSplashMotion.Binding{target=r,tracks=tracks.ToArray(),exitX=exits[i]});
            }
            var original=root.gameObject.scene;
            var account=EditorSceneManager.OpenScene("Assets/Sleepet/Scenes/Sleepet_Account.unity",OpenSceneMode.Additive);
            var source=account.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SleepetFlow>(true)).Single();
            var preview=UnityEngine.Object.Instantiate(source.gameObject,root).GetComponent<RectTransform>();preview.name="Account preview";R(preview,0,0,402,874);
            UnityEngine.Object.DestroyImmediate(preview.GetComponent<SleepetFlow>());UnityEngine.Object.DestroyImmediate(preview.GetComponent<Image>());
            foreach(string name in new[]{"Blue gradient","Brand","Return Home","Local demo","Feedback","Web parity applied"}){var t=preview.Find(name);if(t)UnityEngine.Object.DestroyImmediate(t.gameObject);}
            var group=preview.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            bindings.Add(new SleepetSplashMotion.Binding{target=preview,tracks=new[]{"splash-account-group"}});
            EditorSceneManager.CloseScene(account,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
            var motion=root.gameObject.AddComponent<SleepetSplashMotion>();motion.source=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"web-motion.json");motion.bindings=bindings.ToArray();
        }
    }
}
#endif
