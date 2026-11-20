using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>카드의 태생 등급을 세 자리 별로 그린다. 만점 카드는 세 별을 금색으로 표시한다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DefenseGradeStars : MaskableGraphic
    {
        [SerializeField] private Color _filledColor = new Color32(54, 112, 105, 255);
        [SerializeField] private Color _goldColor = new Color32(193, 126, 25, 255);
        [SerializeField] private Color _emptyColor = new Color32(216, 218, 203, 255);
        private int _grade;

        /// <summary>획득 등급 <paramref name="grade"/>가 바뀔 때만 별을 다시 그린다.</summary>
        public void SetGrade(int grade)
        {
            if (_grade != grade)
            {
                _grade = grade;
                SetVerticesDirty();
            }
        }

        /// <summary>영역 너비에 맞춰 <paramref name="mesh"/>에 다섯 꼭짓점의 별 세 개를 추가한다.</summary>
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float step = rect.width / 3f;
            float radius = Mathf.Min(step * 0.45f, rect.height * 0.48f);
            Color filled = _grade == 4 ? _goldColor : _filledColor;
            for (int i = 0; i < 3; i++)
            {
                Vector2 center = new Vector2(rect.xMin + (i + 0.5f) * step, rect.center.y);
                Color32 tint = i < _grade ? filled : _emptyColor;
                int start = mesh.currentVertCount;
                mesh.AddVert(center, tint, Vector2.zero);
                for (int tip = 0; tip < 10; tip++)
                {
                    float angle = tip * Mathf.PI / 5f;
                    float length = tip % 2 == 0 ? radius : radius * 0.45f;
                    Vector2 edge = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                    mesh.AddVert(center + length * edge, tint, Vector2.zero);
                }
                for (int tip = 0; tip < 10; tip++)
                {
                    mesh.AddTriangle(start, start + tip + 1, start + (tip + 1) % 10 + 1);
                }
            }
        }
    }
}
