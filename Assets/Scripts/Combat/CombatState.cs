namespace DungeonCrawler.Combat
{
    public enum CombatState
    {
        None,
        PlayerTurn_SelectAction,
        PlayerTurn_ExecutingQTE,
        EnemyTurn_Telegraph,
        EnemyTurn_DefendQTE,
        TurnResolution
    }
}
