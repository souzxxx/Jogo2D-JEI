namespace AFirma.Core
{
    /// <summary>Os 4 tipos de ladrão. Nome no jogo fica no ThiefTypeDef.displayName.</summary>
    public enum ThiefType
    {
        Driver,     // Pé-na-Tábua
        Locksmith,  // Gazua
        Hacker,     // Disquete
        ConArtist   // 171
    }

    /// <summary>Tipo de obstáculo de uma sala. Cada um tem um especialista.</summary>
    public enum ObstacleType
    {
        Lock,   // Fechadura -> Gazua
        System, // Sistema   -> Disquete
        Person  // Pessoa    -> 171
    }

    /// <summary>Tipo visual da sala. A Cena do Golpe usa isso pra escolher o prefab.</summary>
    public enum RoomKind
    {
        Entrance,
        Corridor,
        Counter,
        CameraRoom,
        Office,
        Vault,
        Tunnel,
        Garage
    }

    public enum ThiefStatus
    {
        Available,
        OnHeist,
        OnSideJob,
        Jailed
    }

    public enum HeistPhase
    {
        Idle,          // nenhum golpe rodando
        Working,       // crew trabalhando numa sala
        AwaitingDoor,  // pausado esperando "próxima sala" ou "meter o pé"
        AwaitingEvent, // pausado esperando escolha do evento
        Finished
    }

    public enum HeistOutcome
    {
        CleanEscape,   // meteu o pé antes da sirene (ou terminou o cofre sem sirene)
        HastyEscape,   // meteu o pé durante a contagem da polícia
        ForcedEscape,  // contagem zerou
        PerfectEscape, // Cavalo de pau
        Interrupted    // jogo fechou no meio do golpe
    }

    public enum CheckStat
    {
        None,
        Skill,
        Stealth,
        Nerves
    }

    public enum UpgradeEffect
    {
        HeistSlots,         // Van
        CrewSlots,          // Quartos
        SkillPercent,       // Ferramentas
        StealthPercent,     // Tênis de molinha
        LawyerPercent,      // Advogado de porta de cadeia
        ContactsLevel,      // Contatos
        LaundryPercent,     // Lavanderia
        PoliceRadioSeconds, // Rádio da polícia
        SideJobPercent      // Ponto de bico
    }

    public enum FamePerkEffect
    {
        RecruitDiscountPercent, // Reputação
        StartingMoney,          // Herança
        RecruitBonusLevels,     // Lenda
        LootPercent,            // Esquema
        SideJobPercent,         // Rede
        AlertReductionPercent   // Sangue-frio
    }

    public enum MusicTrack
    {
        None,
        Menu,
        Hideout,
        Heist,
        Siren
    }
}
