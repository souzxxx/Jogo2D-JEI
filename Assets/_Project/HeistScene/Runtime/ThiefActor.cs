using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Um ladrão da crew na cena. Usa o Animator do ThiefTypeDef (estados idle, andar, trabalhar, comemorar)
    /// ou, se o dono ainda não fez a arte, o ladrão genérico tingido com a cor do tipo.
    /// Só a HeistView mexe nele; ele não fala com o serviço do golpe.
    /// </summary>
    public class ThiefActor : MonoBehaviour
    {
        public const string Idle = "idle", Walk = "andar", Work = "trabalhar", Celebrate = "comemorar";

        public SpriteRenderer body;
        public SpriteRenderer cloth;
        public Animator animator;
        public SpriteFlipbook flipbook;
        public SpriteRenderer bag;
        public SpriteRenderer marker;

        public ThiefState Thief { get; private set; }
        public ThiefTypeDef Type { get; private set; }
        public bool Moving => path.Count > 0;
        public bool IsCelebrating => pose == Celebrate;
        public float WalkSpeed = 2.4f;

        readonly Queue<Vector3> path = new Queue<Vector3>();
        string pose = Idle;
        string playing;
        float celebrateTimer;
        float speedMult = 1f;
        float markerTimer;
        bool facingLeft;
        SpeechBubble bubble;
        HeistSceneArt art;

        public Vector3 HeadPosition => transform.position + new Vector3(0f, 30f / 32f, 0f);
        public Transform BagAnchor => bag != null ? bag.transform : transform;

        public void Setup(ThiefState thief, ThiefTypeDef type, HeistSceneArt sceneArt)
        {
            Thief = thief;
            Type = type;
            art = sceneArt;
            name = "Ladrão " + (thief != null ? thief.DisplayName : "?");
            bool hasArt = type != null && type.animator != null;
            if (hasArt)
            {
                animator.runtimeAnimatorController = type.animator;
                animator.enabled = true;
                flipbook.enabled = false;
                cloth.enabled = false;
            }
            else
            {
                animator.enabled = false;
                flipbook.enabled = true;
                flipbook.anims = sceneArt.genericThief;
                cloth.enabled = true;
                cloth.color = type != null ? type.color : Color.white;
            }
            bubble = SpeechBubble.Create(sceneArt, transform.parent);
            if (bag != null) bag.enabled = false;
            if (marker != null) marker.enabled = false;
            Play(Idle, true);
        }

        // ---------------- Movimento ----------------

        public void WalkTo(Vector3 target, bool run = false)
        {
            path.Clear();
            path.Enqueue(target);
            speedMult = run ? 1.8f : 1f;
        }

        public void WalkPath(IEnumerable<Vector3> points, bool run = false)
        {
            path.Clear();
            foreach (var p in points) path.Enqueue(p);
            speedMult = run ? 1.8f : 1f;
        }

        public void Teleport(Vector3 p)
        {
            path.Clear();
            transform.position = p;
        }

        public void Face(bool left)
        {
            facingLeft = left;
            body.flipX = left;
            if (cloth != null) cloth.flipX = left;
        }

        public void SetPose(string newPose)
        {
            pose = newPose;
            if (newPose == Celebrate) celebrateTimer = 0.9f;
        }

        void Update()
        {
            float dt = SceneClock.Delta;
            if (animator != null && animator.enabled) animator.speed = SceneClock.Paused ? 0f : SceneClock.Speed * (Moving ? speedMult : 1f);
            if (flipbook != null) flipbook.speed = Moving ? speedMult : 1f;

            if (path.Count > 0)
            {
                var target = path.Peek();
                var pos = transform.position;
                var step = WalkSpeed * speedMult * dt;
                var delta = target - pos;
                if (Mathf.Abs(delta.x) > 0.01f) Face(delta.x < 0f);
                if (delta.magnitude <= step)
                {
                    transform.position = target;
                    path.Dequeue();
                }
                else transform.position = pos + delta.normalized * step;
                Play(Walk);
            }
            else
            {
                if (pose == Celebrate)
                {
                    celebrateTimer -= dt;
                    if (celebrateTimer <= 0f) pose = Idle;
                }
                Play(pose);
            }

            if (markerTimer > 0f)
            {
                markerTimer -= SceneClock.UiDelta;
                if (markerTimer <= 0f && marker != null) marker.enabled = false;
            }
        }

        void Play(string anim, bool force = false)
        {
            if (anim == playing && !force) return;
            playing = anim;
            if (animator != null && animator.enabled)
            {
                int hash = Animator.StringToHash(anim);
                if (animator.HasState(0, hash)) animator.Play(hash, 0, 0f);
            }
            else if (flipbook != null)
            {
                flipbook.Play(anim, true);
                flipbook.SetFlip(facingLeft);
            }
        }

        // ---------------- Feedback ----------------

        public void Say(string line) => bubble?.Show(line, transform, new Vector3(0f, 30f / 32f, 0f));

        /// <summary>Ícone pequeno fixo em cima da cabeça (ex.: "sem especialista" enquanto trabalha).</summary>
        public void ShowMarker(Sprite sprite, float seconds = -1f)
        {
            if (marker == null) return;
            marker.sprite = sprite;
            marker.enabled = sprite != null;
            markerTimer = seconds;
        }

        public void SetBag(Sprite sprite)
        {
            if (bag == null) return;
            bag.sprite = sprite;
            bag.enabled = sprite != null;
        }

        public void SetVisible(bool visible)
        {
            body.enabled = visible;
            if (cloth != null) cloth.enabled = visible && flipbook != null && flipbook.enabled;
            if (bag != null && !visible) bag.enabled = false;
            if (marker != null && !visible) marker.enabled = false;
        }

        void OnDestroy()
        {
            if (bubble != null) Destroy(bubble.gameObject);
        }
    }
}
