using UdonSharp;
using UnityEngine;

namespace Mecaota.Gunner
{
    // 射出口1つの設定。GunnerController が起動時に読む。
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GunnerEmitter : UdonSharpBehaviour
    {
        public Transform[] turns = new Transform[0];  // 1行目 = 射出の向き(+Z = 休み方向)、以降 = 先端 → 根元のボーン
        public float[] turnLimits = new float[0];    // 行ごとの可動域(度)
        public ParticleSystem shotParticle;       // World シミュレーション空間
        public int fireMode = GunnerController.FireSingle;

        public AudioSource fireSource;
        public AudioClip fireClip;
        public float fireMinInterval;
        public float shotSpeed = 25f;
        public float maxRange = 30f;
        public float castRadius = 0.05f;
        public AnimationClip preFireClip;
        public AnimationClip postFireClip;

        public AudioSource impactSource;
        public AudioClip impactClip;
        public float impactMinInterval;
        public ParticleSystem impactParticle;
        public int impactBurst = 1;

        public float streamRate = 12f;
        public int streamHitMode = GunnerController.HitOnce;
        public float streamHitInterval = 0.25f;
        public bool streamCollide = true;

        public float boneStrength = 1f;
        public float boneSmooth = 10f;
        public float boneMinSpeed = 30f;
        public float boneMaxSpeed;

        // 点検の「直す」が turns・turnLimits へ移す読み元。次に互換を切る版で消す。
        [HideInInspector] public Transform pivot;
        [HideInInspector] public Transform[] linkBones = new Transform[0];
        [HideInInspector] public float aimMaxAngle;
        [HideInInspector] public float boneYawMax = 30f;
    }
}
