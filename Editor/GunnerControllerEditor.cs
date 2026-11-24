using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mecaota.Gunner.EditorTools
{
    /// <summary>GunnerController のインスペクタ(射出口の一覧と共通の設定)。</summary>
    [CustomEditor(typeof(GunnerController)), CanEditMultipleObjects]
    public class GunnerControllerEditor : GunnerInspectorBase
    {
        ReorderableList list;

        void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("muzzles"), true, false, true, true)
            {
                drawElementCallback = (rect, i, active, focused) =>
                {
                    var element = list.serializedProperty.GetArrayElementAtIndex(i);
                    rect.y += 2f;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, element, RowLabel(element.objectReferenceValue as GunnerEmitter, i));
                },
                onCanAddCallback = l => l.count < GunnerController.MaxEmitters,
                onAddCallback = l => GunnerEditorUtil.Append(l.serializedProperty, null),
                // 「外す」は一覧から外すだけで、GameObject は消さない。
                onRemoveCallback = l =>
                {
                    GunnerEditorUtil.RemoveAt(l.serializedProperty, l.index);
                    l.index = Mathf.Min(l.index, l.count - 1);
                },
            };
        }

        protected override void DrawBody()
        {
            bool open = Foldout("GunnerController.section.emitters");
            var muzzles = serializedObject.FindProperty("muzzles");
            if (targets.Length == 1) CountAt(GUILayoutUtility.GetLastRect(), muzzles.arraySize);
            if (open)
            {
                if (targets.Length == 1)
                {
                    list.DoLayoutList();
                    EditorGUILayout.LabelField(T("GunnerController.emitters.numberNote"), EditorStyles.wordWrappedMiniLabel);
                }
                else
                    EditorGUILayout.HelpBox(T("GunnerController.emitters.multi"), MessageType.Info);
            }

            if (Foldout("GunnerController.section.shot"))
            {
                Fields("stream", "fireAnimator");
                if (GUILayout.Button(T("GunnerController.syncAnimator")))
                    foreach (GunnerController r in targets) GunnerAnimatorSync.Sync(r);
            }

            if (Foldout("GunnerController.section.hit", false))
                Fields("receivers", "movers", "playerRadius", "maxHits");

            if (Foldout("GunnerController.section.bone", false))
                Field("boneFollow");
        }

        void Fields(params string[] names)
        {
            foreach (var name in names) Field(name);
        }

        static void CountAt(Rect row, int n)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.IntField(new Rect(row.xMax - 40f, row.y, 40f, EditorGUIUtility.singleLineHeight), n);
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
