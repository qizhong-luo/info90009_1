using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Sleepet
{
    // Animation only: deliberately has no AI provider, thought timer or bubble.
    public sealed class HomePetReaction : BaseMeshEffect
    {
        Image image;
        Sprite original;
        Coroutine playing;
        string previous;
        bool normalizeIdle;
        public bool IsPlaying => playing != null;

        public void Configure(UserPreferences preferences)
        {
            normalizeIdle = PetThoughtRules.AnimatedAppearance(preferences);
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || !normalizeIdle || IsPlaying || mesh.currentVertCount == 0) return;
            // Match the animation's 256px canvas: 228px content, feet at y=244.
            // Modify only rendered vertices, retaining the existing layout and hit target.
            var rect = graphic.rectTransform.rect;
            float side = Mathf.Min(rect.width, rect.height);
            var vertex = new UIVertex();
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                min = Vector2.Min(min, vertex.position); max = Vector2.Max(max, vertex.position);
            }
            float factor = Mathf.Min(1, side * (228f / 256f) / Mathf.Max(1, max.y - min.y));
            float centerX = (min.x + max.x) * .5f;
            float floor = rect.center.y - side * (116f / 256f);
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.position = new Vector3(centerX + (vertex.position.x - centerX) * factor,
                    floor + (vertex.position.y - min.y) * factor, vertex.position.z);
                mesh.SetUIVertex(vertex, i);
            }
        }

        public void Tap(UserPreferences preferences)
        {
            Stop();
            Configure(preferences);
            if (!PetThoughtRules.AnimatedAppearance(preferences)) return;
            image = GetComponent<Image>(); original = image.sprite;
            previous = PetThoughtRules.Pick(false, previous);
            var frames = Resources.LoadAll<Sprite>("BorderCollieThoughts/" + previous).OrderBy(s => s.name).ToArray();
            if (frames.Length > 0) playing = StartCoroutine(Play(frames));
        }
        IEnumerator Play(Sprite[] frames)
        {
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < frames.Length / 5f)
            {
                image.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt((Time.unscaledTime - start) * 5))];
                yield return null;
            }
            image.sprite = original; playing = null; graphic.SetVerticesDirty();
        }
        public void Stop()
        {
            if (playing == null) return;
            StopCoroutine(playing); playing = null;
            if (image) image.sprite = original;
            graphic.SetVerticesDirty();
        }
        protected override void OnDisable() { Stop(); base.OnDisable(); }
    }
}
