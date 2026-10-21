using System;
using System.Collections.Generic;

namespace AFirma.Core
{
    /// <summary>Multiplicadores que valem para um golpe específico (modificador sorteado + Nível de Procurado + Fama).</summary>
    public class HeistContext
    {
        public HeistDef heist;
        public HeistModifierDef modifier; // pode ser null
        public int wantedLevel;
        public float lockDifficultyMult = 1f;
        public float systemDifficultyMult = 1f;
        public float personDifficultyMult = 1f;
        public float lootMult = 1f;
        public float alertMult = 1f;
        public float firstRoomsAlertMult = 1f;
        public int firstRoomsCount;
        public float policeCountdownDelta;

        public float DifficultyMult(ObstacleType obstacle)
        {
            switch (obstacle)
            {
                case ObstacleType.Lock: return lockDifficultyMult;
                case ObstacleType.System: return systemDifficultyMult;
                default: return personDifficultyMult;
            }
        }
    }

    /// <summary>Pedido de decisão entre salas: "próxima sala" ou "meter o pé".</summary>
    public class DoorDecision
    {
        public int nextRoomIndex;
        public RoomData nextRoom;
        public bool hasSpecialist;
        public double estimatedLoot;
        public double currentBag;
        public bool sirenActive;
    }

    public class EventChoiceView
    {
        public EventChoice choice;
        public bool available;
        public string unavailableReason;
        public float successChance;
    }

    public class HeistEventInstance
    {
        public HeistEventDef def;
        public List<EventChoiceView> choices = new List<EventChoiceView>();
    }

    public class EventResolution
    {
        public HeistEventDef def;
        public int choiceIndex;
        public bool success;
        public EventEffect effect;
    }

    [Serializable]
    public class HeistResult
    {
        public string heistId;
        public HeistOutcome outcome;
        public int roomsCleared;
        public int totalRooms;
        public bool completed;      // limpou o cofre principal
        public bool sirenHappened;
        public double bag;          // sacola no momento da saída
        public double moneyGained;  // o que entrou no caixa (depois de perdas e bônus)
        public List<string> arrestedThiefIds = new List<string>();
        public List<string> leveledUpThiefIds = new List<string>();
        public int xpPerThief;      // XP base (salas × xpPerRoom), sem manha. O de cada um está em xpGains.
        public List<ThiefXpGain> xpGains = new List<ThiefXpGain>();
    }

    /// <summary>XP que um ladrão ganhou no golpe, pro jornal mostrar.</summary>
    [Serializable]
    public class ThiefXpGain
    {
        public string thiefId;
        public int xp;
        public int levelAfter;
        public bool leveledUp;
    }
}
