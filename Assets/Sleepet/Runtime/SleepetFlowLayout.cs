using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // Same width/height and safe-area policy as the existing main scenes.
    [DefaultExecutionOrder(-50)]
    public sealed class SleepetFlowLayout : MonoBehaviour
    {
        [Serializable] public struct Footer { public RectTransform rect; public float bottom; }
        public RectTransform page;
        public RectTransform[] backgrounds;
        public Footer[] footers;
        Canvas canvas;
        CanvasScaler scaler;
        void Awake() { canvas=GetComponent<Canvas>(); scaler=GetComponent<CanvasScaler>(); }
        void LateUpdate()
        {
            if (!page) return;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Short/tablet windows fit the complete artboard; tall phones extend it.
            float match=(float)Screen.width/Mathf.Max(1,Screen.height)>402f/874f ? 1 : 0;
            if(scaler.matchWidthOrHeight!=match){scaler.matchWidthOrHeight=match;Canvas.ForceUpdateCanvases();}
            var bounds=((RectTransform)transform).rect;
            float inset=Mathf.Max(0,Screen.safeArea.yMin/Mathf.Max(.01f,canvas.scaleFactor)-34);
            ApplyLayout(bounds.width,bounds.height,inset);
        }
        public void ApplyLayout(float width,float height,float inset)
        {
            page.anchorMin=page.anchorMax=page.pivot=new Vector2(0,1);
            page.anchoredPosition=new Vector2((width-402)/2,0);page.sizeDelta=new Vector2(402,height);
            foreach(var rect in backgrounds) if(rect) {
                rect.sizeDelta=new Vector2(402,height);
                var visual=rect.Find("Motion visual") as RectTransform;if(visual)visual.sizeDelta=rect.sizeDelta;
            }
            foreach(var item in footers)
            {
                if(!item.rect)continue;
                var rect=item.rect;float x=rect.anchoredPosition.x;
                rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                rect.anchoredPosition=new Vector2(x,item.bottom+inset);
            }
        }
    }
}
