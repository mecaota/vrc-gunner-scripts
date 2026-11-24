using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Mecaota.Gunner.EditorTools
{
    /// <summary>GunnerEmitter のインスペクタと、その射出口のシーンのハンドル: 姿勢、射程、狙いの扇。</summary>
    [CustomEditor(typeof(GunnerEmitter)), CanEditMultipleObjects]
    public class GunnerEmitterEditor : GunnerInspectorBase
    {
        static readonly Color FanFill = new Color(1f, 0.6f, 0.1f, 0.12f);
        static readonly Color FanLine = new Color(1f, 0.6f, 0.1f, 0.8f);
        static readonly Color RangeLine = new Color(0.3f, 0.8f, 1f, 0.9f);

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            // rig は起動時に値を写すので、Play 中の変更は写し直させる。
            if (EditorGUI.EndChangeCheck() && Application.isPlaying) Reload();
        }

        protected override void DrawBody()
        {
            var muzzle = (GunnerEmitter)target;
            var rig = RigOf(muzzle, out int index);
            if (targets.Length == 1 && rig != null)
                EditorGUILayout.LabelField(string.Format(T("GunnerEmitter.owner"), index, rig.name), EditorStyles.wordWrappedMiniLabel);

            ListField("turns", (e, i) => new GUIContent(i == 0 ? T("GunnerEmitter.turns.aim") : (i + 1).ToString()), int.MaxValue, "turnLimits", 60f);
            Field("shotParticle");
            Field("fireMode");

            Heading("GunnerEmitter.card.fire");
            Fields("fireSource", "fireClip", "fireMinInterval", "shotSpeed", "maxRange", "castRadius", "preFireClip", "postFireClip");

            Heading("GunnerEmitter.card.impact");
            Fields("impactSource", "impactClip", "impactMinInterval", "impactParticle", "impactBurst");

            Heading("GunnerEmitter.card.stream");
            Fields("streamRate", "streamHitMode", "streamHitInterval", "streamCollide");

            DrawFollow(muzzle);
        }

        void Fields(params string[] names)
        {
            foreach (var name in names) Field(name);
        }

        void Heading(string key)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(T(key), EditorStyles.boldLabel);
        }

        void DrawFollow(GunnerEmitter muzzle)
        {
            if (Tip(muzzle) == null) return;
            var fold = serializedObject.FindProperty("boneStrength");
            fold.isExpanded = EditorGUILayout.Foldout(fold.isExpanded, T("GunnerEmitter.card.follow"), true);
            if (!fold.isExpanded) return;
            Fields("boneStrength", "boneSmooth", "boneMinSpeed", "boneMaxSpeed");
        }

        void Reload()
        {
            foreach (var rig in GunnerEditorUtil.FindInScenes<GunnerController>())
            {
                if (rig.muzzles == null || !targets.Any(t => System.Array.IndexOf(rig.muzzles, t) >= 0)) continue;
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(rig);
                if (backing != null) backing.SendCustomEvent(nameof(GunnerController._LoadMuzzles));
            }
        }

        /// <summary>muzzle を射出口に持つ rig と、その番号。無ければ null。</summary>
        internal static GunnerController RigOf(GunnerEmitter muzzle, out int index)
        {
            foreach (var rig in GunnerEditorUtil.FindInScenes<GunnerController>())
            {
                index = rig.muzzles != null ? System.Array.IndexOf(rig.muzzles, muzzle) : -1;
                if (index >= 0) return rig;
            }
            index = -1;
            return null;
        }

        /// <summary>一覧の 1 行目(射出の向き)。無ければ null。</summary>
        internal static Transform Aim(GunnerEmitter muzzle) => muzzle != null && muzzle.turns != null && muzzle.turns.Length > 0 ? muzzle.turns[0] : null;

        /// <summary>連動の先端のボーン(一覧の 2 行目)。連動しない射出口は null。</summary>
        internal static Transform Tip(GunnerEmitter muzzle) => muzzle != null && muzzle.turns != null && muzzle.turns.Length > 1 ? muzzle.turns[1] : null;

        internal static float AimLimit(GunnerEmitter muzzle) => muzzle.turnLimits != null && muzzle.turnLimits.Length > 0 ? muzzle.turnLimits[0] : 0f;

        void OnSceneGUI()
        {
            var muzzle = (GunnerEmitter)target;
            RigOf(muzzle, out int index);
            DrawHandles(muzzle, index);
            var em = Aim(muzzle);
            if (em == null || Event.current.type != EventType.Repaint) return;
            Handles.color = RangeLine;
            // 移動ハンドルの Z 軸と重ならないよう、それより長くする。
            Handles.ArrowHandleCap(0, em.position, em.rotation, HandleUtility.GetHandleSize(em.position) * 2f, EventType.Repaint);
        }

        /// <summary>射出口自身が Target Rotation Transform なら、Unity のハンドルとぶつかるので姿勢のハンドルを出さない。</summary>
        private static void DrawHandles(GunnerEmitter muzzle, int index)
        {
            var em = Aim(muzzle);
            if (em == null) return;
            Vector3 pos = em.position;
            Vector3 fwd = em.forward;
            Vector3 up = em.up;
            Vector3 right = em.right;
            float size = HandleUtility.GetHandleSize(pos);
            float range = Mathf.Max(0f, muzzle.maxRange);
            float angle = Mathf.Clamp(AimLimit(muzzle), 0f, 180f);
            float fan = Mathf.Min(range, size * 1.5f);

            // 狙いは休み方向からここまで届く: 左右に扇を1つ、上下に1つ。
            Handles.color = FanFill;
            Handles.DrawSolidArc(pos, up, Quaternion.AngleAxis(-angle, up) * fwd, angle * 2f, fan);
            Handles.DrawSolidArc(pos, right, Quaternion.AngleAxis(-angle, right) * fwd, angle * 2f, fan);
            Handles.color = FanLine;
            Handles.DrawWireArc(pos, up, Quaternion.AngleAxis(-angle, up) * fwd, angle * 2f, fan);
            Handles.DrawWireArc(pos, right, Quaternion.AngleAxis(-angle, right) * fwd, angle * 2f, fan);

            Handles.color = RangeLine;
            Handles.DrawDottedLine(pos, pos + fwd * range, 4f);
            Handles.Label(pos + up * size * 0.3f, (index >= 0 ? index + " " : "") + muzzle.name);

            if (Application.isPlaying) return;

            bool own = em == muzzle.transform;
            if (!own && Tools.current == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 p = Handles.PositionHandle(pos, Tools.pivotRotation == PivotRotation.Local ? em.rotation : Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(em, "Gunner: move emitter");
                    em.position = p;
                }
            }
            else if (!own && Tools.current == Tool.Rotate)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion r = Handles.RotationHandle(em.rotation, pos);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(em, "Gunner: turn emitter");
                    em.rotation = r;
                }
            }

            Handles.color = RangeLine;
            EditorGUI.BeginChangeCheck();
            Vector3 tip = Handles.Slider(pos + fwd * range, fwd, size * 0.12f, Handles.SphereHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                float nextRange = Mathf.Max(0.1f, Vector3.Dot(tip - pos, fwd));
                GunnerEditorUtil.Edit(muzzle, so => so.FindProperty("maxRange").floatValue = nextRange);
            }

            Handles.color = FanLine;
            Vector3 edge = pos + Quaternion.AngleAxis(angle, up) * fwd * fan;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.FreeMoveHandle(edge, size * 0.08f, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 flat = Vector3.ProjectOnPlane(moved - pos, up);
                if (flat.sqrMagnitude > 1e-8f)
                {
                    float nextAngle = Mathf.Round(Mathf.Clamp(Vector3.Angle(fwd, flat), 1f, 180f));
                    GunnerEditorUtil.Edit(muzzle, so => so.FindProperty("turnLimits").GetArrayElementAtIndex(0).floatValue = nextAngle);
                }
            }
        }
    }
}
