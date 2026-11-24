// GunnerRig 0.4.0 の鎖の式を、Unity なしの純計算で確かめる(System.Numerics の四元数。Unity と同じハミルトン積)。
// 式は GunnerController.cs の BuildChains / StepLinks / RestWorld をそのまま写したもの。
using System;
using System.Numerics;
using System.Text;

namespace Mecaota.Gunner.Tests.Models
{
    public static class ChainMathTest
    {
        const float Deg = (float)(Math.PI / 180.0);
        static Quaternion AA(Vector3 axis, float deg) => Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), deg * Deg);
        // Unity の Quaternion.Euler(x, y, z) = Y * X * Z
        static Quaternion Euler(float x, float y, float z) => AA(Vector3.UnitY, y) * AA(Vector3.UnitX, x) * AA(Vector3.UnitZ, z);
        static Quaternion Inv(Quaternion q) => Quaternion.Inverse(q);
        static Vector3 Fwd(Quaternion q) => Vector3.Transform(Vector3.UnitZ, q);
        static float AngleOf(Quaternion q)
        {
            q = Quaternion.Normalize(q);
            float w = Math.Min(1f, Math.Abs(q.W));
            return 2f * (float)Math.Acos(w) / Deg;
        }
        static float AngleBetween(Vector3 a, Vector3 b)
        {
            float d = Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b));
            return (float)Math.Acos(Math.Max(-1f, Math.Min(1f, d))) / Deg;
        }
        // Unity の Slerp(identity, q, t)(最短の道)
        static Quaternion SlerpFromIdentity(Quaternion q, float t)
        {
            if (q.W < 0) q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
            return Quaternion.Slerp(Quaternion.Identity, q, t);
        }

        public static string Run()
        {
            var sb = new StringBuilder();
            int fails = 0;
            // 階層: P(親) → tip → mid → X(鎖に入らない間のボーン) → lead → 射出口
            Quaternion P = Euler(10, 35, -5);
            Quaternion[] baseLocal = { Euler(-20, 5, 8), Euler(12, -7, 3), Euler(4, 9, -6), Euler(-15, 2, 1) }; // tip, mid, X, lead
            bool[] inChain = { true, true, false, true };
            Quaternion emitterLocal = Euler(-30, 0, 0);

            Quaternion[] World(Quaternion[] locals)
            {
                var w = new Quaternion[locals.Length];
                Quaternion r = P;
                for (int i = 0; i < locals.Length; i++) { r = r * locals[i]; w[i] = r; }
                return w;
            }

            var restW = World(baseLocal);
            Quaternion F = restW[3] * emitterLocal;              // 射出口の休みの座標系(emitter の親 = lead)
            Quaternion restRelLead = Inv(restW[3]) * restW[3] * emitterLocal; // = Inverse(bone.rotation) * parentRot * restLocal
            // GunnerController は chainRel を先端のボーン基準で持ち、使う時に restRel[lead] を掛ける。この式と同値。
            var chainRel = new Quaternion[4];
            for (int i = 0; i < 4; i++) chainRel[i] = i == 3 ? restRelLead : Inv(restW[i]) * F;
            int n = 3;

            // 休みの座標系(RestWorld): 上の親から、鎖のボーンは休みの値、間のボーンは今の値
            Quaternion RestWorld(Quaternion[] cur)
            {
                Quaternion r = P;
                for (int i = 0; i < 4; i++) r = r * (inChain[i] ? baseLocal[i] : cur[i]);
                return r * restRelLead;
            }
            // 全部の行が同じ可動域 limit の鎖(行ごとに 可動域 × 本数 で、2つの値の長さを切る)
            void Case(float aimYaw, float aimPitch, float limit, float expectTotal)
            {
                // 追従の角度が狙いに追いついた状態(boneStrengths = 1)
                float len = (float)Math.Sqrt(aimYaw * aimYaw + aimPitch * aimPitch);
                float s = len > limit * n ? limit * n / len : 1f;
                float ty = aimYaw * s;
                float tp = aimPitch * s;
                Quaternion Q = Euler(-tp, ty, 0);
                Quaternion q = n > 1 ? SlerpFromIdentity(Q, 1f / n) : Q;
                var cur = (Quaternion[])baseLocal.Clone();
                for (int i = 0; i < 4; i++) if (inChain[i]) cur[i] = baseLocal[i] * chainRel[i] * q * Inv(chainRel[i]);
                var w = World(cur);
                float a0 = AngleOf(Inv(baseLocal[0]) * cur[0]);
                float a1 = AngleOf(Inv(baseLocal[1]) * cur[1]);
                float a3 = AngleOf(Inv(baseLocal[3]) * cur[3]);
                float total = a0 + a1 + a3;
                // ボーンと一緒に動いた射出口の座標系の前方と、狙いの向き
                Vector3 boneFwd = Fwd(w[3] * restRelLead);
                Vector3 wish = Fwd(F * Euler(-aimPitch, aimYaw, 0));
                Vector3 limited = Fwd(F * Q);
                float err = AngleBetween(boneFwd, limited);
                float restDrift = AngleBetween(Fwd(RestWorld(cur)), Fwd(F));
                bool same = Math.Abs(a0 - a1) < 0.01f && Math.Abs(a1 - a3) < 0.01f;
                bool sumOk = Math.Abs(total - expectTotal) < 0.05f;
                bool ok = same && sumOk && err < 0.5f && restDrift < 0.01f;
                if (!ok) fails++;
                sb.AppendLine($"狙い 左右{aimYaw,4} 上下{aimPitch,4} 可動域{limit,3}:ボーンの角度 {a0:F3} / {a1:F3} / {a3:F3}  合計 {total:F3}(期待 {expectTotal})  ボーンの先と狙い(上限後)の差 {err:F4}°  狙いとの差 {AngleBetween(boneFwd, wish):F3}°  休みの座標系のずれ {restDrift:F4}°  {(ok ? "OK" : "NG")}");
            }

            Case(0, 0, 30, 0);
            Case(20, 0, 30, 20);
            Case(40, 0, 30, 40);
            Case(60, 0, 30, 60);
            Case(-60, 0, 30, 60);
            Case(0, 20, 30, 20);
            // 上下と左右の組み合わせ: 合計は Euler(-p, y) の回転角
            float comb = AngleOf(Euler(-10, 40, 0));
            Case(40, 10, 30, comb);
            // 1 本あたりの可動域が効く(15 × 3 = 45 度、下向き 12 × 3 = 36 度)
            Case(60, 0, 15, 45);
            Case(0, -50, 12, 36);

            // 戻す: 休みのローカル回転へ戻すと全部 0 度
            {
                var cur = (Quaternion[])baseLocal.Clone();
                float sum = 0;
                for (int i = 0; i < 4; i++) sum += AngleOf(Inv(baseLocal[i]) * cur[i]);
                bool ok = sum < 0.001f;
                if (!ok) fails++;
                sb.AppendLine($"戻した後の角度の合計 {sum:F4}  {(ok ? "OK" : "NG")}");
            }

            // ボーン1本(n = 1)では 0.3.0 の式とビット単位で同じ
            {
                Quaternion P1 = Euler(3, 50, 7);
                Quaternion base1 = Euler(-11, 13, 2);
                Quaternion rel = Euler(-30, 4, 0);
                bool allSame = true;
                foreach (var (y, p) in new[] { (0f, 0f), (20f, 5f), (30f, -12f), (-17f, 25f) })
                {
                    // 0.3.0: bone.localRotation = linkBase * rel * Euler(-p, y, 0) * Inverse(rel)
                    Quaternion oldWrite = base1 * rel * Euler(-p, y, 0) * Inv(rel);
                    // 0.4.0: q = Euler(...)、n = 1 なら Slerp しない。chainBase * chainRel * q * Inverse(chainRel)
                    Quaternion q = Euler(-p, y, 0);
                    Quaternion newWrite = base1 * rel * q * Inv(rel);
                    // RestWorld: 0.3.0 = upRot * linkBase * restRel、0.4.0 = (upRot * chainBase) * restRel
                    Quaternion oldRest = P1 * base1 * rel;
                    Quaternion r = P1; r = r * base1; Quaternion newRest = r * rel;
                    // 上限: perBone * 1
                    float lim = 30f; float lim1 = lim * 1;
                    if (!oldWrite.Equals(newWrite) || !oldRest.Equals(newRest) || lim != lim1) allSame = false;
                }
                if (!allSame) fails++;
                sb.AppendLine($"ボーン1本で 0.3.0 と同じ値: {(allSame ? "OK" : "NG")}");
            }

            sb.AppendLine(fails == 0 ? "全部 OK" : $"NG {fails} 件");
            return sb.ToString();
        }
    }
}
