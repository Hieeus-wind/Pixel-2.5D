using Fusion;
using UnityEngine;

namespace Aetherlands.Networking
{
    /// <summary>
    /// Code-generated, unobtrusive top-left multiplayer status and room controls panel.
    /// Avoids per-frame allocations by caching GUI styles and textures.
    /// </summary>
    public sealed class MultiplayerHud : MonoBehaviour
    {
        [SerializeField] private CoastalSessionRunner sessionRunner;
        [SerializeField] private bool startCollapsed;

        private string _roomNameInput = CoastalSessionConfig.DefaultRoom;
        private bool _collapsed;
        private bool _stylesInitialized;

        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _textFieldStyle;
        private Texture2D _panelBgTexture;
        private Texture2D _buttonBgTexture;

        public void SetSessionRunner(CoastalSessionRunner runner)
        {
            sessionRunner = runner;
            SyncRoomInputFromRunner();
        }

        private void Awake()
        {
            _collapsed = startCollapsed;
            if (sessionRunner == null)
            {
                sessionRunner = GetComponent<CoastalSessionRunner>();
            }

            if (sessionRunner == null)
            {
                sessionRunner = FindFirstObjectByType<CoastalSessionRunner>();
            }

            SyncRoomInputFromRunner();
        }

        private void OnDestroy()
        {
            if (_panelBgTexture != null)
            {
                Destroy(_panelBgTexture);
                _panelBgTexture = null;
            }

            if (_buttonBgTexture != null)
            {
                Destroy(_buttonBgTexture);
                _buttonBgTexture = null;
            }
        }

        private void OnGUI()
        {
            if (sessionRunner == null)
            {
                return;
            }

            EnsureStyles();

            const float margin = 12f;
            float width = _collapsed ? 210f : 292f;
            float height = _collapsed ? 34f : ComputePanelHeight();

            Rect panelRect = new Rect(margin, margin, width, height);
            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 6f, panelRect.width - 20f, panelRect.height - 12f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Aetherlands Coast · MP", _headerStyle);
            if (GUILayout.Button(_collapsed ? "+" : "−", _buttonStyle, GUILayout.Width(24f), GUILayout.Height(20f)))
            {
                _collapsed = !_collapsed;
            }
            GUILayout.EndHorizontal();

            if (!_collapsed)
            {
                DrawBody();
            }

            GUILayout.EndArea();
        }

        private void DrawBody()
        {
            GUILayout.Space(4f);

            CoastalSessionState state = sessionRunner.State;
            GUILayout.Label($"Mode: {FormatState(state)}", _labelStyle);

            if (state == CoastalSessionState.Connected)
            {
                PlayerRef localPlayer = sessionRunner.LocalPlayer;
                string roleLabel = sessionRunner.IsMasterClient ? "Master Client" : "Peer";
                GUILayout.Label($"Peers: {sessionRunner.PeerCount}/{sessionRunner.MaxPlayers}   Role: {roleLabel}", _labelStyle);
                GUILayout.Label($"Local Player: {(localPlayer.IsNone ? "None" : localPlayer.ToString())}", _labelStyle);

                CoastalVersionValidationResult gate = sessionRunner.VersionGateStatus;
                GUIStyle gateStyle = gate.IsCompatible ? _labelStyle : _warningStyle;
                GUILayout.Label($"Version Gate: {(gate.IsCompatible ? "OK" : "Blocked")} — {gate.Summary}", gateStyle);
            }
            else
            {
                GUILayout.Label($"Peers: 1/{sessionRunner.MaxPlayers} (Local)", _labelStyle);
            }

            if (!sessionRunner.IsPhotonAppIdConfigured)
            {
                GUILayout.Space(2f);
                GUILayout.Label(
                    "Setup needed: Set AppIdFusion in PhotonAppSettings.asset to connect.",
                    _warningStyle);
            }

            if (!string.IsNullOrEmpty(sessionRunner.LastStatusMessage) && state == CoastalSessionState.Error)
            {
                GUILayout.Label(sessionRunner.LastStatusMessage, _warningStyle);
            }

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Room:", _labelStyle, GUILayout.Width(44f));
            bool canEditRoom = state == CoastalSessionState.LocalSolo || state == CoastalSessionState.Error;
            GUI.enabled = canEditRoom;
            _roomNameInput = GUILayout.TextField(_roomNameInput ?? string.Empty, 32, _textFieldStyle);
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            if (state == CoastalSessionState.LocalSolo || state == CoastalSessionState.Error)
            {
                GUI.enabled = sessionRunner.IsPhotonAppIdConfigured;
                if (GUILayout.Button("Join Shared Room", _buttonStyle, GUILayout.Height(24f)))
                {
                    sessionRunner.JoinSharedRoom(_roomNameInput);
                }
                GUI.enabled = true;
            }
            else if (state == CoastalSessionState.Connecting)
            {
                if (GUILayout.Button("Cancel Connection", _buttonStyle, GUILayout.Height(24f)))
                {
                    sessionRunner.LeaveSharedRoom();
                }
            }
            else if (state == CoastalSessionState.Connected)
            {
                if (GUILayout.Button("Leave Room / Local Play", _buttonStyle, GUILayout.Height(24f)))
                {
                    sessionRunner.LeaveSharedRoom();
                }
            }
        }

        private float ComputePanelHeight()
        {
            float height = 148f;
            if (sessionRunner.State == CoastalSessionState.Connected)
            {
                height += 42f;
            }

            if (!sessionRunner.IsPhotonAppIdConfigured)
            {
                height += 36f;
            }

            if (sessionRunner.State == CoastalSessionState.Error && !string.IsNullOrEmpty(sessionRunner.LastStatusMessage))
            {
                height += 34f;
            }

            return height;
        }

        private void SyncRoomInputFromRunner()
        {
            if (sessionRunner != null && !string.IsNullOrWhiteSpace(sessionRunner.ActiveRoomName))
            {
                _roomNameInput = sessionRunner.ActiveRoomName;
            }
        }

        private static string FormatState(CoastalSessionState state)
        {
            switch (state)
            {
                case CoastalSessionState.LocalSolo: return "Local Solo";
                case CoastalSessionState.Connecting: return "Connecting...";
                case CoastalSessionState.Connected: return "Shared Mode";
                case CoastalSessionState.Disconnecting: return "Disconnecting...";
                case CoastalSessionState.Error: return "Error (Local Solo)";
                default: return state.ToString();
            }
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }

            _panelBgTexture = MakeSolidTexture(new Color(0.08f, 0.11f, 0.15f, 0.84f));
            _buttonBgTexture = MakeSolidTexture(new Color(0.18f, 0.27f, 0.36f, 0.95f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelBgTexture }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.92f, 0.84f, 1f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.90f, 0.94f, 1f) }
            };

            _warningStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(1f, 0.78f, 0.42f, 1f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { background = _buttonBgTexture, textColor = Color.white }
            };

            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 11
            };

            _stylesInitialized = true;
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
