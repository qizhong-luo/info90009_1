#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Sleepet.Tests
{
    public sealed class WebParityTests
    {
        [UnitySetUp] public IEnumerator Setup(){SleepetSceneSession.TestDataDirectoryOverride=Path.GetFullPath("Validation/Flow/ParityData/"+Guid.NewGuid().ToString("N"));SceneManager.LoadScene("Sleepet_Home");yield return null;yield return null;yield return null;}
        [UnityTearDown] public IEnumerator Cleanup(){var s=SleepetSceneSession.Instance;if(s)Object.Destroy(s.gameObject);yield return null;SleepetSceneSession.TestDataDirectoryOverride=null;}
        static Bounds LocalBounds(RectTransform child,RectTransform root){var points=new Vector3[4];child.GetWorldCorners(points);var b=new Bounds(root.InverseTransformPoint(points[0]),Vector3.zero);foreach(var p in points)b.Encapsulate(root.InverseTransformPoint(p));return b;}
        [UnityTest] public IEnumerator AllScenesUseBundledFrontendFont()
        {
            for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++){
                SceneManager.LoadScene(i);yield return null;yield return null;
                var roots=SceneManager.GetActiveScene().GetRootGameObjects();
                foreach(var t in roots.SelectMany(r=>r.GetComponentsInChildren<Text>(true)))Assert.IsTrue(t.font&&t.font.name.StartsWith("Arial"),SceneManager.GetActiveScene().name+" / "+t.name);
            }
        }
        [UnityTest] public IEnumerator FooterPositionsStayStableAtPhoneTabletAndSafeAreaSizes()
        {
            foreach(var page in new[]{"ChooseCompanion","PersonalisePet","MeetPet","MorningRating","MorningDrink","MorningStretch","MorningTodos","MorningEnd"}){
                SleepetSceneSession.Instance.OpenFlow(page);yield return null;yield return null;yield return null;
                var flow=Object.FindFirstObjectByType<SleepetFlow>();var layout=flow.GetComponentInParent<SleepetFlowLayout>();layout.enabled=false;
                foreach(var size in new[]{new Vector2(402,874),new Vector2(402,980),new Vector2(655,874)})foreach(float inset in new[]{0f,20f}){
                    layout.ApplyLayout(size.x,size.y,inset);Canvas.ForceUpdateCanvases();
                    foreach(var footer in layout.footers){
                        var bounds=LocalBounds(footer.rect,layout.page);
                        Assert.GreaterOrEqual(bounds.min.x,-.1f,page+" left overflow");Assert.LessOrEqual(bounds.max.x,402.1f,page+" right overflow");
                        Assert.GreaterOrEqual(bounds.min.y-layout.page.rect.yMin,inset-.1f,page+" unsafe footer");
                        Assert.That(bounds.min.y-layout.page.rect.yMin,Is.EqualTo(footer.bottom+inset).Within(.15f),page+" layout drift");
                    }
                    string primary=page=="ChooseCompanion"?"Continue":page=="PersonalisePet"?"Create my Sleepet":page=="MeetPet"?"Confirm companion":page=="MorningTodos"?"Thanks":page=="MorningEnd"?"Swipe or continue":"Done";
                    float expected=page=="MorningTodos"?81:page=="MorningEnd"?44:page.StartsWith("Morning")?99:97;
                    var rect=(RectTransform)flow.transform.Find(primary);var b=LocalBounds(rect,layout.page);
                    Assert.That(b.min.y-layout.page.rect.yMin,Is.EqualTo(expected+inset).Within(.15f),page+" CSS bottom offset");
                }
            }
        }
        [UnityTest] public IEnumerator BottomNavigationHasIdenticalHitAreasAndCentersAcrossScenes()
        {
            foreach(var page in new[]{HighFiPage.Home,HighFiPage.Me,HighFiPage.Weekly,HighFiPage.Daily,HighFiPage.DayDetail}){
                SleepetSceneSession.Instance.Navigate(page);yield return null;yield return null;yield return null;
                var ui=Object.FindFirstObjectByType<SleepetHighFi>();var layout=ui.GetComponent<HighFiAdaptiveLayout>();layout.enabled=false;
                foreach(float inset in new[]{0f,20f}){
                    layout.ApplyLayout(980,inset);Canvas.ForceUpdateCanvases();var nav=(RectTransform)ui.bottomNavigation.transform;
                    Assert.That(nav.anchoredPosition.y,Is.EqualTo(0).Within(.01f),"Safe area was added twice");
                    for(int i=0;i<3;i++){
                        var icon=LocalBounds(ui.navIcons[i].rectTransform,nav);Assert.That(icon.center.x,Is.EqualTo(79+120*i).Within(.2f),page+" icon "+i);
                        var button=ui.navHighlights[i].GetComponentInChildren<Button>(true);Assert.NotNull(button);
                        var hit=LocalBounds((RectTransform)button.transform,nav);Assert.That(hit.center.x,Is.EqualTo(icon.center.x).Within(.2f));Assert.GreaterOrEqual(hit.size.y,44);
                    }
                }
            }
        }
        [UnityTest] public IEnumerator MorningMotionUsesStaggerAndNonDriftingGlassLoop()
        {
            SleepetSceneSession.Instance.OpenFlow("MorningDrink");yield return null;yield return null;yield return null;
            var flow=Object.FindFirstObjectByType<SleepetFlow>();
            var done=flow.primary.GetComponent<SleepetUIMotion>();Assert.NotNull(done);done.Evaluate(.5f);Assert.That(done.GetComponent<CanvasGroup>().alpha,Is.LessThan(.01f));done.Evaluate(1.5f);Assert.That(done.GetComponent<CanvasGroup>().alpha,Is.GreaterThan(.99f));
            var glass=flow.GetComponentsInChildren<SleepetUIMotion>().Single(m=>m.loop==SleepetUIMotion.Loop.Glass);glass.Evaluate(0);var first=glass.transform.localRotation;glass.Evaluate(5);Assert.Greater(Quaternion.Angle(first,glass.transform.localRotation),3.9f);glass.Evaluate(10);Assert.Less(Quaternion.Angle(first,glass.transform.localRotation),.1f);
        }
        [UnityTest] public IEnumerator ResponsiveScreensRenderWithoutFooterClipping()
        {
            foreach(string page in new[]{"Home","Me","ChooseCompanion","MorningDrink","MorningTodos","MorningEnd"}){
                SceneManager.LoadScene("Sleepet_"+page);yield return null;yield return null;yield return new WaitForSecondsRealtime(1.6f);
                var flow=Object.FindFirstObjectByType<SleepetFlow>();var ui=Object.FindFirstObjectByType<SleepetHighFi>();
                var canvas=flow?flow.GetComponentInParent<Canvas>():ui.GetComponent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();scaler.enabled=false;
                var adaptive=canvas.GetComponent<HighFiAdaptiveLayout>();if(adaptive)adaptive.enabled=false;
                var flowLayout=canvas.GetComponent<SleepetFlowLayout>();if(flowLayout)flowLayout.enabled=false;
                foreach(var size in new[]{new Vector2Int(402,980),new Vector2Int(768,1024)}){
                    var cameraObject=new GameObject("Responsive evidence camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;
                    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
                    canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                    canvas.scaleFactor=Mathf.Min(size.x/402f,size.y/874f);Canvas.ForceUpdateCanvases();
                    float width=size.x/canvas.scaleFactor,height=size.y/canvas.scaleFactor;
                    if(flowLayout)flowLayout.ApplyLayout(width,height,20);if(adaptive)adaptive.ApplyLayout(height,20);
                    foreach(var text in canvas.GetComponentsInChildren<Text>(true)){text.cachedTextGenerator.Invalidate();text.SetAllDirty();}
                    yield return null;Canvas.ForceUpdateCanvases();camera.Render();
                    var old=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0);texture.Apply();
                    Directory.CreateDirectory("Validation/Flow/Responsive");File.WriteAllBytes("Validation/Flow/Responsive/"+page+"-"+size.x+"x"+size.y+".png",texture.EncodeToPNG());RenderTexture.active=old;
                    canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;camera.targetTexture=null;target.Release();Object.Destroy(texture);Object.Destroy(target);Object.Destroy(cameraObject);
                }
            }
        }
    }
}
#endif
