namespace Aetherlands.Fishing
{
    public enum FishingState : byte
    {
        Idle = 0,
        ChargingCast = 1,
        Casting = 2,
        WaitingForBite = 3,
        HookedMinigame = 4,
        Caught = 5,
        Escaped = 6
    }
}
