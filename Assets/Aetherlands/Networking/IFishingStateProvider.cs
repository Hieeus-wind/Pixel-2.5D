using UnityEngine;

namespace Aetherlands.Networking
{
    /// <summary>
    /// Adapter interface allowing a local fishing controller to expose its state to NetworkedTraveler
    /// without introducing circular assembly dependencies.
    /// </summary>
    public interface IFishingStateProvider
    {
        byte FishingState { get; }
        Vector3 BobberTarget { get; }
    }

    /// <summary>
    /// Optional receiver interface allowing remote proxy visuals (such as a bobber/line renderer)
    /// to reflect replicated fishing state from NetworkedTraveler.
    /// </summary>
    public interface IFishingStateReceiver
    {
        void ApplyRemoteFishingState(byte fishingState, Vector3 bobberTarget);
    }
}
