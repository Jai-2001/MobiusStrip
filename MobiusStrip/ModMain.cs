using System;
using MelonLoader;
using MobiusStrip;
using UnityEngine;

[assembly: MelonInfo(typeof(ModMain), "Mobius Strip", "0.1.0", "Jai-2001")]

namespace MobiusStrip
{
    public class ModMain : MelonMod
    {
        private String _playerLabel = "";
        private String _ghostLabel = "";
        private String _golemLabel = "";
        private String _statusLabel = "";
        private ControlledCharacter? _controls;
        private static MelonLogger.Instance? _logger;
        private bool _player2 = false;
        
        public override void OnInitializeMelon()
        {
            _logger = LoggerInstance;
            Log("Started!");
        }
        
        public static void Log(string msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int line = -1)
        {
            string message = $"[{caller}@{line}]: {msg}";
            if(_logger==null) Debug.Log(message);
            else _logger.Msg(message);
        }
        
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Log($"Scene {sceneName} with build index {buildIndex} has been loaded, HELLO!");
            if (buildIndex==2)
            {
                _controls = new NetworkedPlayer(Player.Instance, PlayerAI.Instance, _player2);
                MelonEvents.OnGUI.Subscribe(PrintPositions, 1);
                MelonEvents.OnUpdate.Subscribe(TrackGolem, 1);
            }
            else
            {
                MelonEvents.OnGUI.Unsubscribe(PrintPositions);
                MelonEvents.OnGUI.Unsubscribe(TrackGolem);
            }
        }
        
        private void PrintPositions()
        {
            GUI.Label(new Rect(500, 500, 300, 500), _playerLabel);
            GUI.Label(new Rect(500, 550, 300, 500), _ghostLabel);
            GUI.Label(new Rect(500, 600, 300, 500), _golemLabel);
            GUI.Label(new Rect(500, 650, 300, 500), _statusLabel);
        }

        private void TrackGolem()
        {
            if (_controls != null)
            {
                if (Input.GetKey("[7]"))
                {
                    _controls.Swap(false);
                }
                else if(Input.GetKey("[9]"))
                {
                    _controls.Swap(true);
                }
                if (Input.GetKey("[1]"))
                {
                    StartNetworkSession(false);
                } else if (Input.GetKey("[3]"))
                {
                    StartNetworkSession(true);
                }
                _controls.Update();
                _playerLabel = GetLabelText("Player", _controls.Player.transform, !_controls.ControllingGolem);
                _ghostLabel = GetLabelText("Ghost", _controls.Ghost.transform, _controls.ControllingGolem);
                _golemLabel = GetLabelText("Golem", _controls.Golem.transform, _controls.ControllingGolem);
                _statusLabel = _controls.Status;
                
            }
            
        }

        private void StartNetworkSession(bool asGolem)
        {
            if (_controls is NetworkedPlayer player)
            {
                if (!player.Started())
                {
                    player.Start(asGolem);
                }
            }
        }

        private String GetLabelText(string name, Transform transform, bool controlling)
        {
            return $"{name} position (Controlling: {controlling}) = {transform.position.ToString()}";
        }
        


        
    }
}