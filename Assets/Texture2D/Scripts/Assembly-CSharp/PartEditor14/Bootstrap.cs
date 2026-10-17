using NewBuildSystem;
using UnityEngine;

namespace PartEditor14
{
    public static class Bootstrap
    {
        public static void Attach(Build build)
        {
            if (build == null || build.GetComponent<PartEditorUI>() != null)
                return;
            var ui = build.gameObject.AddComponent<PartEditorUI>();
            ui.Init(build);
        }
    }
}
