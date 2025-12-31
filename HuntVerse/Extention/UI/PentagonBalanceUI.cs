using Google.Protobuf.WellKnownTypes;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{

    [RequireComponent(typeof(RectTransform))]
    public class PentagonBalanceUI : Graphic
    {
        [Range(0f, 1f)] public float stat1 = 1f;
        [Range(0f, 1f)] public float stat2 = 1f;
        [Range(0f, 1f)] public float stat3 = 1f;
        [Range(0f, 1f)] public float stat4 = 1f;
        [Range(0f, 1f)] public float stat5 = 1f;

        [Header("SETTING")]
        public float padding = 10f;
        public bool drawOutline = true;
        public float outlineThickness = 2f;
        public Color outlineColor = Color.black;

        [Header("STATLABEL")]
        public TextMeshProUGUI[] labels;   
        public float labelOffset = 15f;    

        public void SetStats(float s1, float s2, float s3, float s4, float s5)
        {
            stat1 = Mathf.Clamp01(s1);
            stat2 = Mathf.Clamp01(s2);
            stat3 = Mathf.Clamp01(s3);
            stat4 = Mathf.Clamp01(s4);
            stat5 = Mathf.Clamp01(s5);
            SetVerticesDirty();
        }
        private Coroutine animateRoutine;
        public void AnimateStatsFromZero(float s1, float s2, float s3, float s4, float s5, float duration)
        {
            
            if (animateRoutine != null)
            {
                StopCoroutine(animateRoutine);
                animateRoutine = null;
            }

            animateRoutine = StartCoroutine(AnimateStatsRoutine(
          Mathf.Clamp01(s1),
          Mathf.Clamp01(s2),
          Mathf.Clamp01(s3),
          Mathf.Clamp01(s4),
          Mathf.Clamp01(s5),
          duration));
        }
        private IEnumerator AnimateStatsRoutine(float t1, float t2, float t3, float t4, float t5, float duration)
        {
            float start1 = stat1;
            float start2 = stat2;
            float start3 = stat3;
            float start4 = stat4;
            float start5 = stat5;

            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);

                // 멤버 변수를 직접 업데이트해야 OnPopulateMesh에서 반영됨
                stat1 = Mathf.Lerp(start1, t1, t);
                stat2 = Mathf.Lerp(start2, t2, t);
                stat3 = Mathf.Lerp(start3, t3, t);
                stat4 = Mathf.Lerp(start4, t4, t);
                stat5 = Mathf.Lerp(start5, t5, t);

                SetVerticesDirty();
                yield return null;
            }

            // 최종 값 설정
            stat1 = t1;
            stat2 = t2;
            stat3 = t3;
            stat4 = t4;
            stat5 = t5;

            SetVerticesDirty();
            animateRoutine = null;
        }


        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center; // ���� (0,0)
            float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f - padding;

            float[] stats = { stat1, stat2, stat3, stat4, stat5 };
            int count = 5;
            Vector2[] points = new Vector2[count];

            float angleStep = 360f / count;

            // ������ ��ǥ ���
            for (int i = 0; i < count; i++)
            {
                float angle = (90f - angleStep * i) * Mathf.Deg2Rad;
                float radius = maxRadius * Mathf.Clamp01(stats[i]);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                points[i] = center + dir * radius;
            }

            // ==== ���� ������ �׸��� (���� ����, �״��) ====
            int centerIndex = 0;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = center;
            vh.AddVert(vert);

            for (int i = 0; i < count; i++)
            {
                vert.position = points[i];
                vh.AddVert(vert);
            }

            for (int i = 0; i < count; i++)
            {
                int i0 = centerIndex;
                int i1 = i + 1;
                int i2 = (i + 1) % count + 1;
                vh.AddTriangle(i0, i1, i2);
            }

            // ==== �� ��ġ ������Ʈ ====
            UpdateLabels(center, maxRadius, angleStep);

            // ==== �ܰ��� �׸��� (���� �ڵ�) ====
            if (!drawOutline || outlineThickness <= 0f) return;

            int outlineStartIndex = vh.currentVertCount;
            float half = outlineThickness * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % count];

                Vector2 dir = (p2 - p1).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x);

                Vector2 v1 = p1 + normal * half;
                Vector2 v2 = p1 - normal * half;
                Vector2 v3 = p2 - normal * half;
                Vector2 v4 = p2 + normal * half;

                UIVertex v = UIVertex.simpleVert;
                v.color = outlineColor;

                v.position = v1; vh.AddVert(v);
                v.position = v2; vh.AddVert(v);
                v.position = v3; vh.AddVert(v);
                v.position = v4; vh.AddVert(v);

                int baseIndex = outlineStartIndex + i * 4;
                vh.AddTriangle(baseIndex + 0, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex + 0);
            }
        }

        private void UpdateLabels(Vector2 center, float maxRadius, float angleStep)
        {
            if (labels == null || labels.Length < 5) return;

            for (int i = 0; i < 5; i++)
            {
                if (labels[i] == null) continue;

                // ���� �׻� ���� �ٱ��� ���� + offset
                float angle = (90f - angleStep * i) * Mathf.Deg2Rad;
                float radius = maxRadius + labelOffset;

                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 pos = center + dir * radius;

                labels[i].rectTransform.anchoredPosition = pos;
            }
        }
    }

}
