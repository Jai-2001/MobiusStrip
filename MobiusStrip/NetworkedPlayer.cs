using UnityEngine;
using UnityEngine.AI;

namespace MobiusStrip;

public class NetworkedPlayer : ControlledCharacter
{
    private NetworkNode? _connection;
    private bool _started;
    private string? _label;
    private bool _requestSwap;
    private Player? _remote;
    private Player? _local;
    private Vector3 _lastPos;
    

    public NetworkedPlayer(Player player, PlayerAI golem, bool controllingGolem) : base(player, golem, controllingGolem)
    {
        _lastPos = Vector3.zero;
    }

    public void Start(bool asGolem)
    {

        base.Swap(asGolem);
        if (asGolem)
        {
            _label = "CLIENT";
            _remote = Player;   
            _local = Ghost;
            _connection = new Client("127.0.0.1", 55556);
        } else
        {
            _label = "SERVER";
            _remote = Ghost;
            _local = Player;
            _connection = new Server("127.0.0.1", 55556);
        }
        _connection.AwaitConnection();
        _started = true;
        ModMain.Log($"[{_label}]: Awaiting Connection!");
    }

    public bool Started()
    {
        return _started;
    }

    public override void Update(bool force = false)
    {
        Status = "";
        if (_connection != null && _connection.Paired())
        {
            if (_local != null)
            {
                var moveDestination = _local.myAgent.destination;
                if (moveDestination != _lastPos || _local.boxTouched)
                {
                    _lastPos = moveDestination;
                    string boxName = "";
                    Vector3 boxPosition = Vector3.zero;
                    MovingBox? box = _local.movingBox;
                    if (box != null && !_local.CantMoveBox())
                    {
                        Transform? toLabel = box.transform;
                        boxPosition = toLabel.position;
                        while (toLabel != null)
                        {
                            boxName = toLabel.name + "/" + boxName;
                            toLabel = toLabel.parent;
                        }
                        Status += "\nsending box " + boxName + $"({boxPosition})\n";
                    }
                    _connection.SendMessage(moveDestination, _requestSwap, boxName, boxPosition);
                }
            }
            if (_remote != null && _connection.MessageReceived())
            {
                _connection.ReceiveMessage();
                Vector3 pos = _connection.GetReceivedPosition();
                MovingBox? box = _connection.GetMovingBox();
                var transform = _remote.touchObj.transform;
                transform.position = pos;
                _remote.MoveTo(transform);
                if (box != null)
                {
                    if ( _remote.movingBox!= box) _remote.movingBox = box;
                    _remote.MovingBoxAndPushing();
                    PushBoxGhost(_remote, _connection.GetBoxPosition());
                }
                else if(_remote.movingBox!= null)
                {
                    PushBoxGhost(_remote, _connection.GetBoxPosition());
                    _remote.ResetPushing();
                    _remote.movingBox = null;
                }
                Status += ($"[{_label}] Warping {_remote.name} to {pos.ToString()}");
            }
        }
        //Ghost.eState = EntityState.Pushing;
        base.Update(_connection != null && _connection.Paired());
    }

    public override void Swap(bool toGolem)
    {
        if (_connection != null && _connection.CheckSwapGranted())
        {
            ModMain.Log($"[{_label}]: Swap Request Accepted.");
            base.Swap();
        }
        if (ControllingGolem != toGolem)
        {
            ModMain.Log($"[{_label}]: Requesting Swap.");
            _requestSwap = true;
        }
    }

    private void PushBoxGhost(Player remote, Vector3 destination)
    {
        remote.UpdatePushingPosition();
        if(destination!= Vector3.zero) remote.movingBox.transform.parent.position = destination;
    }
    protected virtual bool QuerySwapAccepted()
    {
        return GolemIsBusy();
    } 
}