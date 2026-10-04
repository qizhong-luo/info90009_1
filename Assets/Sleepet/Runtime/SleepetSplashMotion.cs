using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    public sealed class SleepetSplashMotion : MonoBehaviour
    {
        [Serializable] public class Frame { public float at,x,y,x1,y1,x2,y2;public bool exit; }
        [Serializable] public class Track { public string name,kind;public Frame[] frames; }
        [Serializable] public class Timeline { public Track[] tracks; }
        [Serializable] public class Binding { public RectTransform target;public string[] tracks;public float exitX;[NonSerialized]public Vector2 origin,size; }
        public TextAsset source;
        public Binding[] bindings;
        Timeline timeline;float started;
        void Awake()
        {
            timeline=JsonUtility.FromJson<Timeline>(source.text);started=Time.unscaledTime;
            foreach(var b in bindings){b.origin=b.target.anchoredPosition;b.size=b.target.sizeDelta;}
            Evaluate(0);
        }
        void Update(){Evaluate((Time.unscaledTime-started)/4);}
        public void Evaluate(float progress)
        {
            foreach(var b in bindings)foreach(var name in b.tracks)
            {
                var track=Array.Find(timeline.tracks,t=>t.name==name);if(track==null)continue;
                var a=track.frames[0];var z=a;
                foreach(var frame in track.frames){z=frame;if(frame.at>=progress)break;a=frame;}
                float t=z.at<=a.at?1:SleepetUIMotion.Bezier((progress-a.at)/(z.at-a.at),a.x1,a.y1,a.x2,a.y2);
                float x=Mathf.LerpUnclamped(a.exit?b.exitX:a.x,z.exit?b.exitX:z.x,t),y=Mathf.LerpUnclamped(a.y,z.y,t);
                switch(track.kind){
                    case "opacity":b.target.GetComponent<CanvasGroup>().alpha=x;break;
                    case "translate":b.target.anchoredPosition=b.origin+new Vector2(x,-y);break;
                    case "scale":b.target.localScale=new Vector3(x,Mathf.Abs(y),1);break;
                    case "height":b.target.sizeDelta=new Vector2(b.size.x,x);break;
                }
            }
        }
    }
}
