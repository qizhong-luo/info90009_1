using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // A transient clone keeps the CSS reveal fade alive over the scene handoff.
    public sealed class SleepetRevealOverlay : MonoBehaviour
    {
        public float duration=.56f;
        float started;
        CanvasGroup group;
        void Awake(){started=Time.unscaledTime;group=GetComponent<CanvasGroup>();}
        void Update(){float t=(Time.unscaledTime-started)/duration;group.alpha=1-SleepetUIMotion.Bezier(t,.22f,.7f,.22f,1);if(t>=1)Destroy(gameObject);}
        public static void ContinueAcrossScene(Image source)
        {
            var original=source.GetComponentInParent<Canvas>();
            var go=new GameObject("Morning circle reveal",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;canvas.scaleFactor=original.scaleFactor;
            var group=go.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            var clone=Instantiate(source,go.transform,false);clone.rectTransform.anchorMin=clone.rectTransform.anchorMax=clone.rectTransform.pivot=new Vector2(.5f,.5f);
            clone.rectTransform.anchoredPosition=Vector2.zero;
            clone.rectTransform.sizeDelta=new Vector2(Screen.width,Screen.height)/Mathf.Max(.01f,canvas.scaleFactor);clone.rectTransform.localScale=Vector3.one*3;
            DontDestroyOnLoad(go);go.AddComponent<SleepetRevealOverlay>();
        }
    }
}
