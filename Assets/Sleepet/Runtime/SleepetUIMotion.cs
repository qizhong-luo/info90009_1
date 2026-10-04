using UnityEngine;

namespace Sleepet
{
    // CSS timing in reference pixels, with motion isolated from layout anchors.
    public sealed class SleepetUIMotion : MonoBehaviour
    {
        public float delay, duration=.48f, enterOffset=12, initialAlpha;
        public bool enter=true;
        public bool revealScaleY;
        public enum Loop { None, Bubble, Glass, Arrow, Puppy }
        public Loop loop;
        CanvasGroup group;
        RectTransform rect;
        Vector2 origin;
        float started;
        void OnEnable()
        {
            rect=(RectTransform)transform;origin=rect.anchoredPosition;
            group=GetComponent<CanvasGroup>();started=Time.unscaledTime;
            Evaluate(0);
        }
        void Update(){Evaluate(Time.unscaledTime-started);}
        public static float Bezier(float t,float x1,float y1,float x2,float y2)
        {
            t=Mathf.Clamp01(t);float lo=0,hi=1,u=t;
            for(int i=0;i<16;i++){u=(lo+hi)*.5f;float x=3*(1-u)*(1-u)*u*x1+3*(1-u)*u*u*x2+u*u*u;if(x<t)lo=u;else hi=u;}
            return 3*(1-u)*(1-u)*u*y1+3*(1-u)*u*u*y2+u*u*u;
        }
        public void Evaluate(float seconds)
        {
            if(!rect)return;
            float t=enter?Bezier((seconds-delay)/Mathf.Max(.001f,duration),.22f,.7f,.22f,1):1;
            float alpha=Mathf.Lerp(initialAlpha,1,t),offset=enter?-enterOffset*(1-t):0;
            if(loop==Loop.Bubble)offset+=Mathf.Lerp(3,-4,Bezier(Mathf.PingPong(seconds/7,1),.42f,0,.58f,1));
            if(loop==Loop.Glass)rect.localRotation=Quaternion.Euler(0,0,-Mathf.Lerp(-2,2,Bezier(Mathf.PingPong(seconds/5,1),.42f,0,.58f,1)));
            if(loop==Loop.Arrow){float wave=Bezier(Mathf.PingPong(seconds,1),.42f,0,.58f,1);offset+=Mathf.Lerp(-2,5,wave);alpha*=Mathf.Lerp(.7f,1,wave);}
            if(loop==Loop.Puppy)alpha*=Mathf.Lerp(1,.5f,Bezier(Mathf.PingPong(seconds/.4f,1),.42f,0,.58f,1));
            rect.anchoredPosition=origin+Vector2.up*offset;
            if(revealScaleY)rect.localScale=new Vector3(1,Mathf.Max(.001f,t),1);
            if(group)group.alpha=alpha;
        }
    }
}
