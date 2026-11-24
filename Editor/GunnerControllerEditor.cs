using UnityEditor;
using UnityEngine;

namespace Mecaota.Gunner.EditorTools
{
    /// <summary>GunnerController のインスペクタ(射出口の一覧と共通の設定)。</summary>
    [CustomEditor(typeof(GunnerController)), CanEditMultipleObjects]
    public class GunnerControllerEditor : GunnerInspectorBase
    {
        protected override void DrawBody()
        {
            ListField("muzzles", (e, i) => RowLabel(e.objectReferenceValue as GunnerEmitter, i), GunnerController.MaxEmitters);
            EditorGUILayout.LabelField(T("GunnerController.emitters.numberNote"), EditorStyles.wordWrappedMiniLabel);

            if (Foldout("GunnerController.section.shot"))
            {
                Field("fireAnimator");
                if (GUILayout.Button(T("GunnerController.syncAnimator")))
                    foreach (GunnerController r in targets) GunnerAnimatorSync.Sync(r);
            }

            if (Foldout("GunnerController.section.hit", false))
            {
                ListField("receivers");
                ListField("movers");
                Field("playerRadius");
                Field("maxHits");
            }

            if (Foldout("GunnerController.section.bone", false))
                Field("boneFollow");
        }

        static GUIContent RowLabel(GunnerEmitter muzzle, int i)
        {
            foreach (var c in GunnerChecks.Run(muzzle))
            {
                string icon = c.level == GunnerCheckLevel.Error ? "console.erroricon.sml" : "console.warnicon.sml";
                return new GUIContent(i.ToString(), EditorGUIUtility.IconContent(icon).image, c.Text);
            }
            return new GUIContent(i.ToString());
        }
    }
}
