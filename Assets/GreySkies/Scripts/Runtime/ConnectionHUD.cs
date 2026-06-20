using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text.RegularExpressions;

namespace GreySkies
{
    public class ConnectionHUD : MonoBehaviour
    {
        private string _ipAddress = "127.0.0.1";
        private string _portString = "7777";
        private string _statusMessage = "";
        private bool _showUI = true;

        private void Start()
        {
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient))
            {
                _showUI = false;
            }
        }

        private void Update()
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
                {
                    _showUI = false;
                }
                else
                {
                    _showUI = true;
                }
            }
        }

        private void OnGUI()
        {
            if (!_showUI) return;

            // Colors based on DESIGN.md Green countryside survival palette
            Color deepCharcoal = new Color(30f/255f, 34f/255f, 31f/255f, 0.95f);
            Color lighterCharcoal = new Color(56f/255f, 64f/255f, 59f/255f);
            Color activeMoss = new Color(74f/255f, 109f/255f, 85f/255f);
            Color offWhite = new Color(227f/255f, 232f/255f, 229f/255f);
            Color desaturatedSage = new Color(160f/255f, 170f/255f, 164f/255f);

            Texture2D bgTex = CreateColorTexture(deepCharcoal);
            Texture2D btnTex = CreateColorTexture(lighterCharcoal);
            Texture2D hoverTex = CreateColorTexture(activeMoss);

            GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = bgTex;
            windowStyle.border = new RectOffset(4, 4, 4, 4);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = offWhite;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontSize = 14;

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.normal.textColor = activeMoss;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = btnTex;
            buttonStyle.normal.textColor = offWhite;
            buttonStyle.hover.background = hoverTex;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.background = CreateColorTexture(deepCharcoal);
            buttonStyle.fontSize = 13;
            buttonStyle.fontStyle = FontStyle.Bold;

            GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.normal.background = btnTex;
            textFieldStyle.normal.textColor = offWhite;
            textFieldStyle.fontSize = 13;
            textFieldStyle.alignment = TextAnchor.MiddleCenter;

            float width = 340f;
            float height = 280f;
            float x = (Screen.width - width) * 0.5f;
            float y = 50f;

            GUILayout.BeginArea(new Rect(x, y, width, height), windowStyle);
            GUILayout.Space(15);
            GUILayout.Label("GREY SKIES ONLINE", headerStyle);
            GUILayout.Space(10);

            if (GUILayout.Button("HOST SERVER & GAME", buttonStyle, GUILayout.Height(35)))
            {
                ConfigureTransport();
                if (NetworkManager.Singleton.StartHost())
                {
                    _statusMessage = "Hosting started successfully.";
                }
                else
                {
                    _statusMessage = "Failed to start host.";
                }
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("IP ADDRESS:", labelStyle, GUILayout.Width(100));
            _ipAddress = GUILayout.TextField(_ipAddress, textFieldStyle, GUILayout.Height(24));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("PORT:", labelStyle, GUILayout.Width(100));
            _portString = GUILayout.TextField(_portString, textFieldStyle, GUILayout.Height(24));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("JOIN MULTIPLAYER GAME", buttonStyle, GUILayout.Height(35)))
            {
                ConfigureTransport();
                if (NetworkManager.Singleton.StartClient())
                {
                    _statusMessage = "Attempting to connect...";
                }
                else
                {
                    _statusMessage = "Failed to initiate connection.";
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("START DEDICATED SERVER", buttonStyle, GUILayout.Height(25)))
            {
                ConfigureTransport();
                if (NetworkManager.Singleton.StartServer())
                {
                    _statusMessage = "Server started successfully.";
                }
                else
                {
                    _statusMessage = "Failed to start server.";
                }
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(10);
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.normal.textColor = desaturatedSage;
                statusStyle.alignment = TextAnchor.MiddleCenter;
                statusStyle.fontSize = 12;
                GUILayout.Label(_statusMessage, statusStyle);
            }

            GUILayout.EndArea();
        }

        private void ConfigureTransport()
        {
            if (NetworkManager.Singleton == null) return;
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                string cleanIP = Regex.Replace(_ipAddress, "[^0-9.]", "").Trim();
                ushort port = 7777;
                ushort.TryParse(Regex.Replace(_portString, "[^0-9]", ""), out port);

                transport.SetConnectionData(cleanIP, port);
            }
        }

        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}