using System.Collections.Generic;
using AFirma.Core;
using UnityEngine;

namespace AFirma.HeistScene
{
    /// <summary>
    /// Cena do Golpe: monta o prédio do golpe e conta visualmente tudo o que o motor manda pelos GameEvents.
    /// Só escuta, nunca chama o serviço do golpe (quem decide porta, evento e fuga é a HUD).
    ///
    /// HeistStarted → prédio, crew na van, câmera · RoomEntered → porta abre, crew anda, sala acende ·
    /// RoomProgress → anel · RoomCleared → adereço abre, notas voam pra sacola, "+Cr$" · DoorDecisionRequested →
    /// crew na porta, prévia da próxima sala · evento → "!?" e resultado num balão · AbilityUsed → VFX da habilidade ·
    /// SirenStarted → viatura, tremida · HeistFinished → crew corre pra van (preso fica algemado) e a van arranca.
    /// </summary>
    public class HeistView : MonoBehaviour
    {
        public HeistSceneArt art;
        public HeistCameraRig cameraRig;
        public CityBackdrop backdrop;
        public AlertAtmosphere atmosphere;
        public VehicleView vanPrefab;
        public VehicleView policePrefab;
        [Tooltip("Distância (unidades) em que as luzes de uma sala ficam ligadas. Longe da câmera elas apagam (WebGL).")]
        public float lightRange = 14f;

        static readonly string[] SirenLines = { "Sujou!", "Olha a sirene!", "Bora, bora, bora!", "Mete o pé, chefia!" };

        BuildingView building;
        HeistDef heist;
        readonly List<ThiefActor> actors = new List<ThiefActor>();
        readonly HashSet<ThiefActor> arrested = new HashSet<ThiefActor>();
        VehicleView van, police;
        Transform world;
        ThiefActor worker, bagCarrier;
        int roomIndex = -1;
        bool atDoor, inEvent, leaving, sirenOn, perfectEscape;
        float leaveTimer;
        bool vanGone;
        readonly HashSet<string> sfxPlayedRooms = new HashSet<string>();

        public BuildingView Building => building;
        public IReadOnlyList<ThiefActor> Actors => actors;

        void OnEnable()
        {
            GameEvents.HeistStarted += OnHeistStarted;
            GameEvents.RoomEntered += OnRoomEntered;
            GameEvents.RoomProgress += OnRoomProgress;
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.BagChanged += OnBagChanged;
            GameEvents.DoorDecisionRequested += OnDoorRequested;
            GameEvents.DoorDecisionMade += OnDoorMade;
            GameEvents.HeistEventTriggered += OnEventTriggered;
            GameEvents.HeistEventResolved += OnEventResolved;
            GameEvents.AbilityUsed += OnAbilityUsed;
            GameEvents.SirenStarted += OnSiren;
            GameEvents.ThiefBark += OnBark;
            GameEvents.HeistFinished += OnFinished;
            GameEvents.PauseChanged += OnPause;
        }

        void OnDisable()
        {
            GameEvents.HeistStarted -= OnHeistStarted;
            GameEvents.RoomEntered -= OnRoomEntered;
            GameEvents.RoomProgress -= OnRoomProgress;
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.BagChanged -= OnBagChanged;
            GameEvents.DoorDecisionRequested -= OnDoorRequested;
            GameEvents.DoorDecisionMade -= OnDoorMade;
            GameEvents.HeistEventTriggered -= OnEventTriggered;
            GameEvents.HeistEventResolved -= OnEventResolved;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            GameEvents.SirenStarted -= OnSiren;
            GameEvents.ThiefBark -= OnBark;
            GameEvents.HeistFinished -= OnFinished;
            GameEvents.PauseChanged -= OnPause;
        }

        void Start()
        {
            // Cena aberta com o golpe já rodando (ex.: alguém recarregou a cena): monta do estado atual.
            if (building != null || !Services.IsInitialized || !Services.Heist.IsRunning) return;
            var h = Services.Heist;
            OnHeistStarted(h.CurrentHeist);
            OnRoomEntered(h.RoomIndex, h.CurrentRoom);
            for (int i = 0; i < h.RoomIndex; i++) building.rooms[i].MarkCleared();
            OnBagChanged(h.Bag);
            if (h.SirenActive) OnSiren(h.PoliceSecondsLeft);
            if (h.PendingDoor != null) OnDoorRequested(h.PendingDoor);
        }

        // ---------------- Montagem ----------------

        void OnHeistStarted(HeistDef def)
        {
            Clear();
            heist = def;
            world = new GameObject("Golpe: " + def.displayName).transform;
            world.SetParent(transform, false);

            if (def.buildingPrefab != null)
            {
                var go = Instantiate(def.buildingPrefab, world);
                building = go.GetComponent<BuildingView>();
                if (building == null || !building.Matches(def))
                {
                    Debug.LogWarning($"[Cena do Golpe] O prédio de {def.displayName} não bate com as salas do golpe. Montando com as salas modulares.");
                    Destroy(go);
                    building = null;
                }
            }
            if (building == null) building = BuildingAssembler.Assemble(def, art, world);

            for (int i = 0; i < building.rooms.Count; i++)
                building.rooms[i].Setup(i, def.rooms[i], art);

            if (atmosphere != null)
            {
                atmosphere.SetAmbient(building.ambient, building.ambientIntensity);
            }
            if (backdrop != null) backdrop.SetStreet(building.transform.position.y + building.streetY);

            // van estacionada e crew descendo dela
            van = Instantiate(vanPrefab, world);
            van.Park(building.vanPoint.position);
            var crew = Services.Heist.CurrentCrew;
            for (int i = 0; i < crew.Count; i++)
            {
                var actor = Instantiate(art.thiefPrefab, world);
                actor.Setup(crew[i], Services.Db.GetThiefType(crew[i].type), art);
                actor.Teleport(building.vanPoint.position + new Vector3(0.6f - i * 0.35f, RoomView.FeetY, 0f));
                actors.Add(actor);
            }
            bagCarrier = actors.Count > 0 ? actors[actors.Count - 1] : null;

            if (cameraRig != null)
            {
                cameraRig.SetBounds(building.MinX, building.MaxX);
                cameraRig.Follow(CameraPoint(building.vanPoint.position.x + 3f), true);
            }
        }

        void Clear()
        {
            if (world != null) Destroy(world.gameObject);
            actors.Clear();
            arrested.Clear();
            sfxPlayedRooms.Clear();
            building = null;
            worker = null;
            van = police = null;
            roomIndex = -1;
            atDoor = inEvent = leaving = sirenOn = perfectEscape = vanGone = false;
        }

        Vector2 CameraPoint(float x) => new Vector2(x, (building != null ? building.transform.position.y + building.cameraY : 3.5f));

        // ---------------- Salas ----------------

        void OnRoomEntered(int index, RoomData room)
        {
            if (building == null || index < 0 || index >= building.rooms.Count) return;
            if (index > 0)
            {
                var previousDoor = building.rooms[index - 1].exitDoor;
                if (previousDoor != null && !previousDoor.IsOpen)
                {
                    previousDoor.SetOpen(true);
                    PlaySfx(art.doorSfx);
                }
            }
            roomIndex = index;
            atDoor = false;
            var view = building.rooms[index];
            view.SetState(RoomView.State.Active);
            if (index + 1 < building.rooms.Count) building.rooms[index + 1].SetState(RoomView.State.Hidden);

            worker = PickWorker(room.obstacle, out bool specialist);
            if (view.badge != null) view.badge.SetWarning(!specialist);

            // posição de cada um: quem trabalha na frente do obstáculo, o resto em fila atrás
            int slot = 1;
            foreach (var actor in actors)
            {
                var spot = actor == worker ? view.CrewSlot(0) : view.CrewSlot(slot++);
                if (index == 0)
                {
                    var path = new List<Vector3>(building.EntryPoints()) { spot };
                    actor.WalkPath(path, run: true);
                }
                else actor.WalkTo(spot, run: true);
                actor.ShowMarker(null);
            }
            if (worker != null && !specialist) worker.ShowMarker(art.noSpecialistIcon);
            cameraRig?.Follow(CameraPoint(view.Center.x));
        }

        ThiefActor PickWorker(ObstacleType obstacle, out bool specialist)
        {
            ThiefActor best = null, bestSpecialist = null;
            float bestSkill = -1f, bestSpecSkill = -1f;
            foreach (var actor in actors)
            {
                float skill = Services.Crew.GetSkill(actor.Thief);
                var def = actor.Type;
                if (def != null && def.hasSpecialty && def.specialty == obstacle && skill > bestSpecSkill)
                {
                    bestSpecSkill = skill;
                    bestSpecialist = actor;
                }
                if (skill > bestSkill)
                {
                    bestSkill = skill;
                    best = actor;
                }
            }
            specialist = bestSpecialist != null;
            return specialist ? bestSpecialist : best;
        }

        void OnRoomProgress(int index, float progress)
        {
            if (building == null || index < 0 || index >= building.rooms.Count) return;
            building.rooms[index].SetProgress(progress);
        }

        void OnRoomCleared(int index, RoomData room, double loot)
        {
            if (building == null || index < 0 || index >= building.rooms.Count) return;
            var view = building.rooms[index];
            view.MarkCleared();
            foreach (var actor in actors)
            {
                actor.ShowMarker(null);
                if (!actor.Moving) actor.SetPose(ThiefActor.Celebrate);
            }
            SpawnLoot(view.LootPosition, loot);
        }

        void SpawnLoot(Vector3 from, double loot)
        {
            if (loot <= 0) return;
            int notes = Mathf.Clamp(Mathf.RoundToInt((float)System.Math.Log10(loot + 1) * 2f), 3, 10);
            var target = bagCarrier != null ? bagCarrier.BagAnchor : null;
            for (int i = 0; i < notes; i++)
            {
                var sprite = i % 3 == 2 && art.coin != null ? art.coin : art.note;
                bool last = i == notes - 1;
                FlyingLoot.Spawn(sprite, from, target, Vector3.zero, i * 0.07f, last ? (System.Action)(() => PlaySfx(art.lootSfx)) : null);
            }
            FloatingText.Spawn(art.font, "+" + Formulas.Money(loot), new Color(0.71f, 0.86f, 0.55f), from + new Vector3(0f, 0.4f, 0f));
        }

        void OnBagChanged(double bag)
        {
            if (bagCarrier != null) bagCarrier.SetBag(bag > 0 ? art.BagSprite(bag) : null);
        }

        // ---------------- Porta ----------------

        void OnDoorRequested(DoorDecision decision)
        {
            if (building == null) return;
            atDoor = true;
            var current = building.rooms[Mathf.Clamp(roomIndex, 0, building.rooms.Count - 1)];
            int slot = 0;
            foreach (var actor in actors)
            {
                actor.WalkTo(current.ExitPosition + new Vector3(-0.38f * slot++, 0f, 0f));
                actor.ShowMarker(null);
            }
            if (decision.nextRoomIndex < building.rooms.Count)
            {
                var next = building.rooms[decision.nextRoomIndex];
                next.SetState(RoomView.State.Preview);
                if (next.badge != null) next.badge.SetWarning(!decision.hasSpecialist);
                cameraRig?.Follow(CameraPoint((current.Center.x + next.Center.x) / 2f));
            }
            if (current.exitDoor != null) current.exitDoor.SetHighlight(true);
        }

        void OnDoorMade(bool goToNext)
        {
            if (building == null || roomIndex < 0) return;
            var door = building.rooms[roomIndex].exitDoor;
            if (door != null) door.SetHighlight(false);
            if (goToNext && door != null)
            {
                door.SetOpen(true);
                PlaySfx(art.doorSfx);
            }
        }

        // ---------------- Eventos e falas ----------------

        void OnEventTriggered(HeistEventInstance instance)
        {
            inEvent = true;
            var spot = CrewTop();
            PopIcon.Spawn(art.eventIcon, spot + new Vector3(0f, 0.35f, 0f), 2.2f, 0.2f);
        }

        void OnEventResolved(EventResolution resolution)
        {
            inEvent = false;
            var spot = CrewTop();
            PopIcon.Spawn(resolution.success ? art.checkIcon : art.failIcon, spot + new Vector3(0f, 0.35f, 0f), 1.4f);
            var effect = resolution.effect;
            if (effect == null) return;
            var talker = worker != null ? worker : (actors.Count > 0 ? actors[0] : null);
            if (talker != null && !string.IsNullOrEmpty(effect.resultText)) talker.Say(effect.resultText);
            if (effect.delaySeconds > 0f && talker != null) talker.ShowMarker(art.delayIcon, effect.delaySeconds / Mathf.Max(0.1f, SceneClock.Speed));
            if (effect.bagPercentDelta < 0f && bagCarrier != null) DropNotes(bagCarrier.BagAnchor.position, 4);
            if (effect.jailRandomThief) PopIcon.Spawn(art.handcuffsIcon, spot + new Vector3(0.4f, 0.2f, 0f), 1.6f);
        }

        void OnBark(ThiefState thief, string line)
        {
            var actor = Find(thief);
            if (actor != null) actor.Say(line);
        }

        Vector3 CrewTop()
        {
            if (actors.Count == 0) return Vector3.zero;
            var sum = Vector3.zero;
            foreach (var a in actors) sum += a.HeadPosition;
            return sum / actors.Count;
        }

        ThiefActor Find(ThiefState thief)
        {
            foreach (var a in actors)
                if (a.Thief == thief || (thief != null && a.Thief != null && a.Thief.id == thief.id)) return a;
            return null;
        }

        // ---------------- Habilidades ----------------

        void OnAbilityUsed(ThiefState thief, ThiefAbilityDef ability)
        {
            var actor = Find(thief);
            if (actor == null || ability == null) return;
            actor.SetPose(ThiefActor.Celebrate);
            actor.Say(ability.displayName + "!");
            var room = building != null && roomIndex >= 0 ? building.rooms[roomIndex] : null;
            var at = room != null && room.Prop != null ? room.Prop.transform.position + new Vector3(0f, 0.5f, 0f) : actor.HeadPosition;
            if (ability.vfxPrefab != null)
            {
                var fx = Instantiate(ability.vfxPrefab, at, Quaternion.identity, world);
                Destroy(fx, 4f);
            }
            else PopIcon.Spawn(ability.icon != null ? ability.icon : art.checkIcon, actor.HeadPosition + new Vector3(0f, 0.3f, 0f), 1.4f);
            if (thief.type == ThiefType.Locksmith) cameraRig?.Shake(0.09f, 0.35f);
            if (thief.type == ThiefType.Driver) perfectEscape = true;
        }

        // ---------------- Sirene e fuga ----------------

        void OnSiren(float seconds)
        {
            if (sirenOn || building == null) return;
            sirenOn = true;
            cameraRig?.Shake(0.12f, 0.6f);
            police = Instantiate(policePrefab, world);
            // a viatura chega pela rua e para atrás da van: aparece de vez quando a crew corre pra fuga
            var stop = building.policePoint.position;
            police.Park(stop + new Vector3(-12f, 0f, 0f));
            police.DriveTo(stop, 6f, 4f);
            police.SetFlashing(true);
            if (actors.Count > 0)
                actors[Random.Range(0, actors.Count)].Say(SirenLines[Random.Range(0, SirenLines.Length)]);
        }

        void OnFinished(HeistResult result)
        {
            if (building == null) return;
            leaving = true;
            leaveTimer = 0f;
            perfectEscape |= result.outcome == HeistOutcome.PerfectEscape;
            var path = ExitPath();
            foreach (var actor in actors)
            {
                actor.ShowMarker(null);
                bool caught = actor.Thief != null && result.arrestedThiefIds.Contains(actor.Thief.id);
                if (caught)
                {
                    actor.WalkTo(actor.transform.position);
                    actor.ShowMarker(art.handcuffsIcon);
                    actor.body.color = new Color(0.7f, 0.7f, 0.8f);
                    arrested.Add(actor);
                    continue;
                }
                actor.WalkPath(path, run: true);
            }
            if (result.outcome == HeistOutcome.ForcedEscape || result.outcome == HeistOutcome.HastyEscape)
                if (bagCarrier != null && result.bag > 0) DropNotes(bagCarrier.BagAnchor.position, result.outcome == HeistOutcome.ForcedEscape ? 6 : 3);
            foreach (var room in building.rooms)
                if (room.exitDoor != null) room.exitDoor.SetHighlight(false);
        }

        List<Vector3> ExitPath()
        {
            var path = new List<Vector3>();
            float y = building.transform.position.y + RoomView.FeetY;
            if (roomIndex >= 0) path.Add(new Vector3(building.rooms[0].EntryPosition.x, y, 0f));
            var entry = new List<Vector3>(building.EntryPoints());
            entry.Reverse();
            path.AddRange(entry);
            path.Add(building.vanPoint.position + new Vector3(0.5f, RoomView.FeetY, 0f));
            return path;
        }

        void DropNotes(Vector3 from, int count)
        {
            var floor = new GameObject("chão").transform;
            floor.SetParent(world, false);
            floor.position = new Vector3(from.x - 0.6f, building.transform.position.y + RoomView.FeetY, 0f);
            for (int i = 0; i < count; i++)
                FlyingLoot.Spawn(art.note, from, floor, new Vector3(Random.Range(-0.6f, 0.2f), 0f, 0f), i * 0.05f, null);
            Destroy(floor.gameObject, 3f);
        }

        // ---------------- Todo frame ----------------

        void Update()
        {
            if (building == null) return;

            if (leaving)
            {
                UpdateLeaving();
                CullLights();
                return;
            }

            bool working = Services.Heist.IsRunning && Services.Heist.Phase == HeistPhase.Working && !atDoor && !inEvent;
            bool workerInPlace = false;
            foreach (var actor in actors)
            {
                if (actor.Moving) continue;
                if (actor.transform.position.x > (building.rooms.Count > 0 ? building.rooms[0].transform.position.x : 0f)) actor.Face(false);
                if (actor == worker && working)
                {
                    actor.SetPose(ThiefActor.Work);
                    workerInPlace = true;
                }
                else if (!actor.IsCelebrating) actor.SetPose(ThiefActor.Idle);
            }
            if (roomIndex >= 0 && roomIndex < building.rooms.Count)
            {
                var room = building.rooms[roomIndex];
                room.SetWorking(workerInPlace);
                if (workerInPlace && worker != null && worker.Type != null && worker.Type.actionSfx != null)
                {
                    string key = roomIndex + ":" + worker.Thief?.id;
                    if (sfxPlayedRooms.Add(key)) PlaySfx(worker.Type.actionSfx);
                }
            }

            // a câmera acompanha a crew quando ela está andando
            if (actors.Count > 0 && cameraRig != null && AnyMoving())
                cameraRig.Follow(CameraPoint(CrewCenterX()));
            CullLights();
        }

        bool AnyMoving()
        {
            foreach (var a in actors) if (a.Moving) return true;
            return false;
        }

        float CrewCenterX()
        {
            float sum = 0f;
            foreach (var a in actors) sum += a.transform.position.x;
            return sum / Mathf.Max(1, actors.Count);
        }

        void UpdateLeaving()
        {
            leaveTimer += SceneClock.Delta;
            bool everyoneIn = true;
            foreach (var actor in actors)
            {
                if (arrested.Contains(actor)) continue; // preso fica pra trás, algemado
                if (actor.Moving) everyoneIn = false;
                else actor.SetVisible(false);
            }
            if (!vanGone && van != null && (everyoneIn || leaveTimer > 8f))
            {
                vanGone = true;
                if (perfectEscape) van.Spin(0.9f);
                van.DriveTo(van.transform.position + new Vector3(-30f, 0f, 0f), 2f, perfectEscape ? 14f : 8f);
            }
            if (cameraRig != null)
            {
                if (van != null && vanGone) cameraRig.Follow(CameraPoint(van.transform.position.x));
                else if (actors.Count > 0) cameraRig.Follow(CameraPoint(CrewCenterX()));
            }
        }

        void CullLights()
        {
            if (cameraRig == null) return;
            float camX = cameraRig.transform.position.x;
            foreach (var room in building.rooms)
            {
                bool near = Mathf.Abs(room.Center.x - camX) < lightRange;
                room.SetNear(near);
            }
        }

        void OnPause(bool paused)
        {
            if (world == null) return;
            foreach (var ps in world.GetComponentsInChildren<ParticleSystem>())
            {
                if (paused) ps.Pause();
                else if (ps.isPaused) ps.Play();
            }
        }

        void PlaySfx(AudioClip clip)
        {
            if (clip != null && Services.IsInitialized) Services.Audio.PlaySfx(clip, 0.8f);
        }
    }
}
