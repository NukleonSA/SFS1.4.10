using UnityEngine;

namespace PartEditor14
{
    public static class InputBlocker
    {
        public static bool IsOverUI(Vector2 posPixel)
        {
            if (global::Ref.currentScene != global::Ref.SceneType.Build)
                return false;
            var ui = PartEditorUI.Current;
            return ui != null && ui.IsOverUI(posPixel);
        }
    }
}
