using NewBuildSystem;
using UnityEngine;

namespace PartEditor14
{
    public static class TransformEditor
    {
        public static void Move(PlacedPart part, float dx, float dy, Build build)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            part.position += new Vector2(dx, dy);
            ClampNoRound(part, grid);
            if (part.partIcon != null)
            {
                var p = part.partIcon.localPosition;
                part.partIcon.localPosition = new Vector3(part.position.x, part.position.y, p.z);
            }
            Enqueue(grid, part);
        }

        public static void Rotate(PlacedPart part, Build build, int delta)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            part.orientation.z = (part.orientation.z + delta + 360) % 360;
            RefreshIcon(part, build);
        }

        public static void FlipX(PlacedPart part, Build build)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            part.orientation.FlipX();
            RefreshIcon(part, build);
        }

        public static void FlipY(PlacedPart part, Build build)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            part.orientation.FlipY();
            RefreshIcon(part, build);
        }

        public const float MinSize = 0.05f;
        public const float MaxSize = 32f;

        /// <summary>有效宽度（格），含小数。</summary>
        public static float GetWidth(PlacedPart part)
        {
            if (part == null || part.orientation == null)
                return 0f;
            return Abs(part.orientation.x) * part.orientation.Mx;
        }

        /// <summary>有效高度（格），含小数。</summary>
        public static float GetHeight(PlacedPart part)
        {
            if (part == null || part.orientation == null)
                return 0f;
            return Abs(part.orientation.y) * part.orientation.My;
        }

        /// <summary>设置有效宽度（格，含小数）：x 保持整数格数+符号不变，小数进 sxf = 宽度/|x|，几何与物理随之缩放。</summary>
        public static void SetWidth(PlacedPart part, Build build, float width)
        {
            ApplySize(part, build, width, true);
        }

        /// <summary>设置有效高度（格，支持小数）。语义同 SetWidth。</summary>
        public static void SetHeight(PlacedPart part, Build build, float height)
        {
            ApplySize(part, build, height, false);
        }

        private static void ApplySize(PlacedPart part, Build build, float size, bool isX)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            Orientation orientation = part.orientation;
            if (orientation == null)
                return;

            int cells = isX ? orientation.x : orientation.y;
            if (cells < 0)
                cells = -cells;
            if (cells < 1)
                cells = 1;

            float clamped = Mathf.Clamp(size, MinSize, MaxSize);
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            if (isX)
                orientation.Mx = clamped / (float)cells;
            else
                orientation.My = clamped / (float)cells;
            RefreshIcon(part, build);
        }

        private static int Abs(int v)
        {
            return v < 0 ? -v : v;
        }

        public static void NextSkin(PlacedPart part, Build build)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            if (!CanChangeSkin(part))
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            part.NextSkin();
            Enqueue(grid, part);
        }

        public static void PrevSkin(PlacedPart part, Build build)
        {
            if (part == null || build == null || build.buildGrid == null)
                return;
            if (!CanChangeSkin(part))
                return;
            var grid = build.buildGrid;
            grid.CreateUndoStep();
            int count = part.partData.skins.skins.Length;
            part.skinId = (part.skinId - 1 + count) % count;
            Utility.ApplySkin(part.partIcon, part.partData, part.skinId, part.orientation);
            Enqueue(grid, part);
        }

        private static bool CanChangeSkin(PlacedPart part)
        {
            return part.partData != null &&
                   part.partData.skins != null &&
                   part.partData.skins.skins != null &&
                   part.partData.skins.skins.Length > 0 &&
                   part.partIcon != null;
        }

        private static void ClampNoRound(PlacedPart part, PartGrid grid)
        {
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;
            PlacedPart.AppliedPartData.HitboxMesh[] hitboxes = part.appliedData.hitboxes;
            foreach (PlacedPart.AppliedPartData.HitboxMesh hitbox in hitboxes)
            {
                Vector2[] vertices = hitbox.vertices;
                foreach (Vector2 vertex in vertices)
                {
                    Vector2 world = part.position + vertex * part.orientation;
                    max.x = Mathf.Max(max.x, world.x);
                    max.y = Mathf.Max(max.y, world.y);
                    min.x = Mathf.Min(min.x, world.x);
                    min.y = Mathf.Min(min.y, world.y);
                }
            }
            Vector2 outOfBounds = new Vector2(Mathf.Min(0f, min.x), Mathf.Min(0f, min.y)) +
                                  new Vector2(Mathf.Max(0f, max.x - grid.gridSize.Value.x), Mathf.Max(0f, max.y - grid.gridSize.Value.y));
            part.position = part.position - outOfBounds;
        }

        private static void RefreshIcon(PlacedPart part, Build build)
        {
            if (part == null || part.partData == null || build.buildGrid == null)
                return;
            var grid = build.buildGrid;
            var old = part.partIcon;
            Transform parent = old != null ? old.parent : grid.transform;
            if (old != null)
                Object.Destroy(old.gameObject);

            part.partIcon = PartGrid.LoadIcon(
                part.partData,
                part.orientation,
                part.skinId,
                new Vector3(part.position.x, part.position.y, 0f),
                part.partData.prefab.localScale,
                parent,
                0,
                Color.white,
                true);

            Enqueue(grid, part);
        }

        private static void Enqueue(PartGrid grid, PlacedPart part)
        {
            if (!grid.changed.Contains(part))
                grid.changed.Add(part);
        }
    }
}
