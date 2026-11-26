using Aetherlands.Coast;
using Fusion;
using UnityEngine;

namespace Aetherlands.Networking
{
    /// <summary>
    /// Shared Mode networked traveler avatar.
    /// Only State Authority writes Networked properties in FixedUpdateNetwork().
    /// Local player retains local TravelerMotor/TravelerKeyboard and CoastalCamera;
    /// remote proxies disable local input/camera and drive TravelerView from replicated state.
    /// </summary>
    public sealed class NetworkedTraveler : NetworkBehaviour
    {
        private const float TeleportSnapDistanceSqr = 16f;
        private const float MovementEpsilonSqr = 0.00001f;

        [SerializeField] private CoastalSessionConfig sessionConfig;
        [SerializeField] private TravelerMotor motor;
        [SerializeField] private TravelerView view;
        [SerializeField] private CharacterController characterController;

        [Networked] public Vector3 NetworkPosition { get; set; }
        [Networked] public byte NetworkFacing { get; set; }
        [Networked] public NetworkBool NetworkIsMoving { get; set; }
        [Networked] public byte NetworkFishingState { get; set; }
        [Networked] public Vector3 NetworkBobberTarget { get; set; }

        private IFishingStateProvider _fishingProvider;
        private IFishingStateReceiver _fishingReceiver;
        private CoastalCamera _boundCamera;
        private Transform _previousCameraTarget;

        private Vector3 _lastAuthorityPosition;
        private byte _lastAuthorityFacing;
        private byte _lastAppliedFishingState = byte.MaxValue;
        private Vector3 _lastAppliedBobberTarget;
        private bool _interactionEnabled = true;

        public bool IsLocalTraveler => Object != null && Object.IsValid && Object.HasStateAuthority;

        public void SetFishingBridge(IFishingStateProvider provider, IFishingStateReceiver receiver = null)
        {
            _fishingProvider = provider;
            _fishingReceiver = receiver;
        }

        public void BindCamera(CoastalCamera camera)
        {
            if (camera == null || !IsLocalTraveler)
            {
                return;
            }

            if (_boundCamera != camera)
            {
                _previousCameraTarget = camera.target;
            }

            _boundCamera = camera;
            _boundCamera.target = transform;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            if (motor != null && IsLocalTraveler)
            {
                motor.enabled = _interactionEnabled;
            }
        }

        public override void Spawned()
        {
            CacheComponents();

            if (Object.HasStateAuthority)
            {
                ConfigureAsLocalAuthority();
            }
            else
            {
                ConfigureAsRemoteProxy();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_boundCamera != null && _boundCamera.target == transform && _previousCameraTarget != null)
            {
                _boundCamera.target = _previousCameraTarget;
            }

            _boundCamera = null;
            _previousCameraTarget = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - _lastAuthorityPosition;
            Vector2 planarDelta = new Vector2(delta.x, delta.z);
            bool moving = planarDelta.sqrMagnitude > MovementEpsilonSqr;

            if (moving)
            {
                _lastAuthorityFacing = EncodeFacing(planarDelta);
            }

            NetworkPosition = currentPosition;
            NetworkFacing = _lastAuthorityFacing;
            NetworkIsMoving = moving;

            if (_fishingProvider != null)
            {
                NetworkFishingState = _fishingProvider.FishingState;
                NetworkBobberTarget = _fishingProvider.BobberTarget;
            }

            _lastAuthorityPosition = currentPosition;
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid || Object.HasStateAuthority)
            {
                return;
            }

            Vector3 targetPos = NetworkPosition;
            Vector3 currentPos = transform.position;
            Vector3 offset = targetPos - currentPos;

            Vector3 nextPos;
            if (offset.sqrMagnitude > TeleportSnapDistanceSqr)
            {
                nextPos = targetPos;
            }
            else
            {
                float rate = sessionConfig != null ? sessionConfig.remoteInterpolationRate : 18f;
                float t = 1f - Mathf.Exp(-rate * Time.deltaTime);
                nextPos = Vector3.Lerp(currentPos, targetPos, t);
            }

            Vector3 frameDelta = nextPos - currentPos;
            transform.position = nextPos;

            if (view != null)
            {
                Vector2 facingIntent = DecodeFacingIntent(NetworkFacing);
                float visualDistance = NetworkIsMoving
                    ? Mathf.Max(new Vector2(frameDelta.x, frameDelta.z).magnitude, Time.deltaTime * 2.5f)
                    : 0f;
                view.Animate(facingIntent, visualDistance);
            }

            if (_fishingReceiver != null)
            {
                byte state = NetworkFishingState;
                Vector3 target = NetworkBobberTarget;
                if (state != _lastAppliedFishingState || (target - _lastAppliedBobberTarget).sqrMagnitude > 0.0001f)
                {
                    _lastAppliedFishingState = state;
                    _lastAppliedBobberTarget = target;
                    _fishingReceiver.ApplyRemoteFishingState(state, target);
                }
            }
        }

        public static byte EncodeFacing(Vector2 planarIntent)
        {
            if (Mathf.Abs(planarIntent.x) > Mathf.Abs(planarIntent.y))
            {
                return planarIntent.x > 0f ? (byte)2 : (byte)3;
            }

            return planarIntent.y > 0f ? (byte)1 : (byte)0;
        }

        public static Vector2 DecodeFacingIntent(byte facing)
        {
            switch (facing)
            {
                case 1: return Vector2.up;
                case 2: return Vector2.right;
                case 3: return Vector2.left;
                default: return Vector2.down;
            }
        }

        private void CacheComponents()
        {
            if (motor == null) motor = GetComponent<TravelerMotor>();
            if (view == null) view = GetComponent<TravelerView>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (_fishingProvider == null) _fishingProvider = GetComponentInChildren<IFishingStateProvider>();
            if (_fishingReceiver == null) _fishingReceiver = GetComponentInChildren<IFishingStateReceiver>();
        }

        private void ConfigureAsLocalAuthority()
        {
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            if (motor != null)
            {
                motor.enabled = _interactionEnabled;
            }

            _lastAuthorityPosition = transform.position;
            _lastAuthorityFacing = 0;

            NetworkPosition = _lastAuthorityPosition;
            NetworkFacing = 0;
            NetworkIsMoving = false;

            if (_fishingProvider != null)
            {
                NetworkFishingState = _fishingProvider.FishingState;
                NetworkBobberTarget = _fishingProvider.BobberTarget;
            }
        }

        private void ConfigureAsRemoteProxy()
        {
            if (motor != null)
            {
                motor.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = NetworkPosition;
            if (view != null)
            {
                view.Animate(DecodeFacingIntent(NetworkFacing), 0f);
            }
        }
    }
}
