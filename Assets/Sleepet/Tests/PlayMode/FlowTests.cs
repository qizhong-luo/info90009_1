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
using Object = UnityEngine.Object;

namespace Sleepet.Tests
{
    public sealed class FlowTests
    {
        SleepetSceneSession session;
        string path;
        SleepetFlow Flow => Object.FindFirstObjectByType<SleepetFlow>();
        [UnitySetUp] public IEnumerator Setup()
        {
            path=Path.GetFullPath("Validation/Flow/TestData/"+Guid.NewGuid().ToString("N"));
            SleepetSceneSession.TestDataDirectoryOverride=path;
            SceneManager.LoadScene("Sleepet_Home");yield return null;yield return null;yield return null;
            session=SleepetSceneSession.Instance;Assert.NotNull(session.Demo.Store);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(session)Object.Destroy(session.gameObject);yield return null;SleepetSceneSession.TestDataDirectoryOverride=null;}
        IEnumerator Open(string page)
        {session.OpenFlow(page);yield return null;yield return null;yield return null;Assert.IsTrue(Flow.Ready);}
        void StartNight()
        {session.Demo.StartSleep();session.Demo.EndSleep();session.BeginMorning();}

        [UnityTest] public IEnumerator OnboardingCommitsOnceAndSurvivesLegacySettingsSave()
        {
            var store=session.Demo.Store;var identity=session.RuntimeSessionId;
            session.BeginOnboarding();yield return Open("Account");
            Flow.email.text="invalid";Flow.password.text="local-demo-only";Flow.SubmitAccount();
            Assert.AreEqual("Account",Flow.page);Assert.IsFalse(store.Preferences.onboardingComplete);
            Flow.email.text="test@example.com";Flow.SubmitAccount();yield return null;yield return null;
            Flow.SelectPet(1);yield return Open("PersonalisePet");
            Flow.petName.text="Luna";Flow.CreatePet();yield return null;yield return null;
            Assert.AreEqual("Mocha",store.Preferences.petName,"Draft must not mutate saved preferences");
            yield return Open("MeetPet");Assert.AreEqual("Meet Luna",Flow.title.text);Flow.ConfirmPet();
            yield return null;yield return null;
            Assert.AreEqual("Sleepet_Home",SceneManager.GetActiveScene().name);Assert.AreSame(store,session.Demo.Store);
            Assert.AreEqual(identity,session.RuntimeSessionId);
            session.Demo.SaveSettings();var disk=new SleepetStore(path);
            Assert.AreEqual("Luna",disk.Preferences.petName);Assert.AreEqual(1,disk.Preferences.petOption);
            Assert.AreEqual("cat",disk.Preferences.petSpecies);Assert.AreEqual("test@example.com",disk.Preferences.accountEmail);
            Assert.IsTrue(disk.Preferences.onboardingComplete);StringAssert.DoesNotContain("local-demo-only",File.ReadAllText(Path.Combine(path,"settings.json")));
            var home=Object.FindFirstObjectByType<SleepetHighFi>();StringAssert.Contains("Luna",home.homeReadyLabel.text);
            session.Navigate(HighFiPage.Me);yield return null;yield return null;
            Assert.AreEqual("test@example.com",Object.FindFirstObjectByType<SleepetHighFi>().GetComponentsInChildren<Text>(true).Single(t=>t.name=="Email").text);
        }
        [UnityTest] public IEnumerator CancelledDraftLeavesSavedPetUnchanged()
        {
            session.BeginOnboarding();yield return Open("ChooseCompanion");Flow.SelectPet(2);Flow.Home();yield return null;yield return null;
            Assert.AreEqual(0,session.Demo.Store.Preferences.petOption);Assert.IsFalse(session.Demo.Store.Preferences.onboardingComplete);
        }
        [UnityTest] public IEnumerator MorningReadsPlanAndUpdatesOnlyItsSession()
        {
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path,"tomorrow-plan.json"),JsonUtility.ToJson(new TomorrowPlan{water=false,stretch=false,breakfast=true,groceries=false,meeting=false,shortEvent="Bring the notebook"}));
            var store=session.Demo.Store;StartNight();yield return null;yield return null;yield return null;
            string id=session.MorningSessionId;
            store.Add(new SleepSummary{sessionId="unrelated",startedAt=DateTime.UtcNow.AddDays(1).ToString("O"),result="Calm"});
            yield return Open("MorningRating");Flow.rating.value=4;Flow.SubmitRating();yield return null;yield return null;
            Assert.AreEqual("MorningTodos",Flow.page);Assert.AreEqual("Make breakfast",Flow.todoLabels[0].text);Assert.AreEqual("Bring the notebook",Flow.todoLabels[1].text);
            Flow.notify.isOn=true;Flow.FinishTodos();yield return null;yield return null;
            Assert.AreEqual("MorningEnd",Flow.page);Flow.FinishMorning();yield return new WaitForSecondsRealtime(.8f);yield return null;
            var disk=new SleepetStore(path);var record=disk.History.records.Single(r=>r.sessionId==id);
            Assert.AreEqual(2,disk.History.records.Count);Assert.IsTrue(record.morningRated);Assert.AreEqual(4,record.morningRating);
            Assert.IsTrue(record.morningCompleted);Assert.IsTrue(record.notifyLater);Assert.IsFalse(disk.History.records.Single(r=>r.sessionId=="unrelated").morningRated);
            Assert.AreSame(store,session.Demo.Store);Assert.AreEqual("Sleepet_Home",SceneManager.GetActiveScene().name);
            session.Demo.SetDifficultResult();
            var afterDebug=new SleepetStore(path).History.records.Single(r=>r.sessionId==id);
            Assert.IsTrue(afterDebug.morningRated);Assert.AreEqual(4,afterDebug.morningRating);Assert.IsTrue(afterDebug.morningCompleted);
        }
        [UnityTest] public IEnumerator StretchRequiresElapsedTimeAndSkipIsRecorded()
        {
            StartNight();yield return Open("MorningStretch");
            Assert.IsFalse(Flow.primary.interactable);Flow.StretchDone();Assert.AreEqual("MorningStretch",Flow.page);
            Flow.StretchSkip();yield return null;yield return null;Assert.AreEqual("MorningTodos",Flow.page);
            Assert.AreEqual("skipped",new SleepetStore(path).History.records.Single().stretchStatus);
        }
        [Test] public void MissingSessionAndFailedWritesDoNotClaimSuccess()
        {
            var store=session.Demo.Store;Assert.IsFalse(store.SaveMorning("missing",r=>r.morningRating=4));Assert.AreEqual(0,store.History.records.Count);
            store.Add(new SleepSummary{sessionId="fixture",startedAt=DateTime.UtcNow.ToString("O")});
            using(var locked=new FileStream(Path.Combine(path,"history.json"),FileMode.Open,FileAccess.Read,FileShare.None))
            {Assert.IsFalse(store.SaveMorning("fixture",r=>r.morningRated=true));Assert.IsFalse(store.History.records[0].morningRated);}
        }
        [UnityTest] public IEnumerator CompletedStretchAndSelectedPetStayConsistent()
        {
            var settings=session.Demo.Store.CopyPreferences();settings.petOption=1;settings.petName="Luna";session.Demo.Store.SavePreferences(settings);
            StartNight();yield return Open("WakeFeedback");Assert.IsNull(Flow.video.clip);Assert.IsTrue(Flow.pet.gameObject.activeSelf);
            Assert.AreSame(Flow.pets[1],Flow.pet.sprite);
            yield return Open("MorningDrink");Assert.AreSame(Flow.pets[1],Flow.animatedPet.sprite);Assert.IsEmpty(Flow.animationFrames);
            Flow.DrinkDone();yield return null;yield return null;yield return null;
            Assert.AreEqual("MorningStretch",Flow.page);Flow.stretchSeconds=.01f;yield return null;yield return null;
            Assert.IsTrue(Flow.primary.interactable);Flow.StretchDone();yield return null;yield return null;
            var record=new SleepetStore(path).History.records.Single();Assert.AreEqual("done",record.waterStatus);Assert.AreEqual("done",record.stretchStatus);
        }
        [UnityTest] public IEnumerator AllNewScenesHaveEditableUIAndSameBackend()
        {
            var store=session.Demo.Store;StartNight();yield return null;yield return null;
            foreach(string page in SleepetFlow.PageNames)
            {
                yield return Open(page);
                Assert.AreSame(store,session.Demo.Store);
                Assert.AreEqual(1,Object.FindObjectsByType<SleepetSceneSession>(FindObjectsSortMode.None).Length);
                Assert.NotNull(Flow.GetComponentInParent<Canvas>());
                Assert.Greater(Flow.GetComponentsInChildren<Text>(true).Length,1);
                yield return new WaitForSecondsRealtime(page=="OnboardingSplash"?.9f:page=="CreatingPet"?1.1f:1.5f);
                Capture(page);
            }
        }
        [UnityTest] public IEnumerator UpwardGestureCompletesMorningButShortDragDoesNot()
        {
            StartNight();yield return Open("MorningEnd");
            Assert.IsTrue(Flow is UnityEngine.EventSystems.IDragHandler);
            var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=Vector2.zero};
            Flow.OnBeginDrag(e);e.position=new Vector2(0,1);Flow.OnDrag(e);Flow.OnEndDrag(e);
            Assert.IsFalse(Flow.Record.morningCompleted);
            e.position=Vector2.zero;Flow.OnBeginDrag(e);e.position=new Vector2(0,100*Flow.GetComponentInParent<Canvas>().scaleFactor);Flow.OnDrag(e);Flow.OnEndDrag(e);
            yield return new WaitForSecondsRealtime(.8f);yield return null;
            Assert.IsTrue(new SleepetStore(path).History.records.Single().morningCompleted);
            Assert.AreEqual("Sleepet_Home",SceneManager.GetActiveScene().name);
        }
        void Capture(string name)
        {
            var canvas=Flow.GetComponentInParent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();
            var cameraObject=new GameObject("Evidence camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(402,874,24);camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            scaler.enabled=false;canvas.scaleFactor=1;
            var layout=canvas.GetComponent<SleepetFlowLayout>();layout.enabled=false;Canvas.ForceUpdateCanvases();layout.ApplyLayout(402,874,0);
            foreach(var text in canvas.GetComponentsInChildren<Text>(true)){text.cachedTextGenerator.Invalidate();text.SetAllDirty();}
            Canvas.ForceUpdateCanvases();camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(402,874,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,402,874),0,0);image.Apply();
            Directory.CreateDirectory("Validation/Flow/Screens");File.WriteAllBytes("Validation/Flow/Screens/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=prior;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            scaler.enabled=true;layout.enabled=true;
            camera.targetTexture=null;target.Release();Object.Destroy(image);Object.Destroy(target);Object.Destroy(cameraObject);
        }
    }
}
#endif
