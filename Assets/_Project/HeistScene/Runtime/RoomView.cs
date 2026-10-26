using AFirma.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Uma sala em corte lateral. Há um prefab por RoomKind (Salas/Sala_Kind) e cada prédio usa instâncias deles
    /// com o fundo e os adereços do tema. Escura até a crew chegar; na decisão de porta mostra a prévia da próxima.
    /// </summary>
    public class RoomView : MonoBehaviour
    {
        public const float WidthUnits = 200f / 32f;   // 192 px de sala + 8 px de parede
        public const float HeightUnits = 96f / 32f;
        public const float FeetY = 3f / 32f;

        public RoomKind kind;
        public SpriteRenderer background;
        public SpriteRenderer fog;
        [Tooltip("Paredes, laje e fachada genéricas: só ligadas quando o prédio é montado sem prefab próprio.")]
        public GameObject structure;
        public Transform entryPoint;
        public Transform exitPoint;
        public ObstacleBadge badge;
        public ObstacleProp[] props = new ObstacleProp[0];
        public DoorView exitDoor;
        public GuardView[] guards = new GuardView[0];
        public SecurityCameraView[] cameras = new SecurityCameraView[0];
        public AlarmLight alarm;
        public Light2D[] lights = new Light2D[0];
        public ParticleSystem dust;

        public enum State { Hidden, Preview, Active, Cleared }

        public int Index { get; private set; }
        public RoomData Data { get; private set; }
        public State Current { get; private set; }
        public ObstacleProp Prop { get; private set; }

        const float HiddenFog = 0.82f, PreviewFog = 0.45f;
        float fogTarget, fogAlpha;

        public void Setup(int index, RoomData data, HeistSceneArt art)
        {
            Index = index;
            Data = data;
            Prop = null;
            foreach (var p in props)
            {
                if (p == null) continue;
                bool match = p.obstacle == data.obstacle && Prop == null;
                p.gameObject.SetActive(match);
                if (match) Prop = p;
            }
            if (Prop != null) Prop.SetOpen(false);
            if (badge != null)
            {
                badge.ringFrames = art.ringFrames;
                badge.checkIcon = art.checkIcon;
                badge.Setup(art.ObstacleIcon(data.obstacle));
            }
            if (exitDoor != null) exitDoor.SetOpen(false);
            SetState(State.Hidden, true);
        }

        /// <summary>Onde o especialista trabalha: na frente do adereço do obstáculo.</summary>
        public Vector3 WorkPosition
        {
            get
            {
                if (Prop != null && Prop.workPoint != null) return Ground(Prop.workPoint.position);
                return Ground(transform.position + new Vector3(WidthUnits * 0.62f, 0f, 0f));
            }
        }

        /// <summary>Lugar de cada ladrão na sala: o especialista na frente, os outros atrás, em fila.</summary>
        public Vector3 CrewSlot(int slot)
        {
            if (slot == 0) return WorkPosition;
            return WorkPosition + new Vector3(-0.42f * slot - 0.1f, 0f, 0f);
        }

        public Vector3 EntryPosition => Ground(entryPoint != null ? entryPoint.position : transform.position + new Vector3(0.5f, 0f, 0f));
        public Vector3 ExitPosition => Ground(exitPoint != null ? exitPoint.position : transform.position + new Vector3(WidthUnits - 0.4f, 0f, 0f));
        public Vector3 LootPosition => Prop != null ? Prop.transform.position + new Vector3(0f, 0.6f, 0f) : WorkPosition + new Vector3(0.5f, 0.6f, 0f);
        public Vector3 Center => transform.position + new Vector3(WidthUnits / 2f, HeightUnits / 2f, 0f);

        Vector3 Ground(Vector3 p) => new Vector3(p.x, transform.position.y + FeetY, 0f);

        public void SetState(State state, bool instant = false)
        {
            Current = state;
            fogTarget = state == State.Hidden ? HiddenFog : state == State.Preview ? PreviewFog : 0f;
            if (instant) fogAlpha = fogTarget;
            ApplyFog();
            if (badge != null)
            {
                if (state == State.Hidden) badge.ShowHidden();
                else if (state == State.Preview) badge.ShowPreview();
                else if (state == State.Active) badge.ShowActive();
            }
            foreach (var l in lights) if (l != null) l.enabled = state != State.Hidden;
            foreach (var g in guards) if (g != null) g.SetRoomActive(state == State.Active, false);
            if (Prop != null && Prop.person != null) Prop.person.SetRoomActive(state == State.Active, false);
            if (dust != null)
            {
                if (state == State.Active || state == State.Cleared) { if (!dust.isPlaying) dust.Play(); }
                else dust.Stop();
            }
        }

        public void SetWorking(bool working)
        {
            if (Prop != null && Prop.person != null) Prop.person.SetRoomActive(Current == State.Active, working);
        }

        public void SetProgress(float progress)
        {
            if (badge != null) badge.SetProgress(progress);
            if (Prop != null && Data != null && Data.obstacle != ObstacleType.Person && progress < 1f) Prop.Jiggle(0.4f);
        }

        public void MarkCleared()
        {
            Current = State.Cleared;
            if (badge != null) badge.ShowCleared();
            if (Prop != null) Prop.SetOpen(true);
            SetWorking(false);
        }

        Light2D[] allLights;
        bool near = true;

        /// <summary>Longe da câmera as luzes da sala (lâmpada, giroflex, lanterna, câmera) desligam: poupa o WebGL.</summary>
        public void SetNear(bool value)
        {
            if (value == near) return;
            near = value;
            if (allLights == null) allLights = GetComponentsInChildren<Light2D>(true);
            foreach (var l in allLights)
                if (l != null) l.gameObject.SetActive(near);
        }

        void Update()
        {
            if (Mathf.Approximately(fogAlpha, fogTarget)) return;
            fogAlpha = Mathf.MoveTowards(fogAlpha, fogTarget, SceneClock.UiDelta * 2.5f);
            ApplyFog();
        }

        void ApplyFog()
        {
            if (fog == null) return;
            fog.enabled = fogAlpha > 0.01f;
            fog.color = new Color(0.04f, 0.035f, 0.07f, fogAlpha);
        }
    }
}
