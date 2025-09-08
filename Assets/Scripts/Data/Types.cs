// Types.cs  (deja aquí todos los enums)
public enum MoveKind
{
    Ground = 0,
    Knight = 1,
    Archer = 2,
    Flier = 3,
    Heavy = 4,
    Mage = 5,
    Monk = 6,
    Thief = 7,

    // Aliases para compatibilidad con scripts antiguos:
    Walker = Ground,
    Cavalry = Knight,
    Aquatic = Heavy
}

public enum PromoItem { None, PegasusWing, WarriorPride, VigorBall, SecretBook, SilverTank }
public enum WeaponGroup { Swords, Axes, SpearsLances, Bows, RodsStaves, Gloves, Knives }
