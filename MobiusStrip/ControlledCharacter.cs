using System;
using UnityEngine;
using MelonLoader;
using Object = UnityEngine.Object;

namespace MobiusStrip;

public class ControlledCharacter
{
    public readonly Player Player;
    public readonly Player Ghost;
    public readonly PlayerAI Golem;
    public bool ControllingGolem;
    public String Status = "";

    public ControlledCharacter(Player player, PlayerAI golem, bool controllingGolem)
    {
        Player = player;
        Golem = golem;
        ControllingGolem = controllingGolem;
        Ghost = Object.Instantiate(player, player.transform.parent.parent , true);
        Ghost.name = "Ghost";
        Ghost.tag = "Ghost";
        Ghost.childObject.active = false;
        Ghost.adultObject.active = false;
        Ghost.hasControl = false;
        Golem.childObject.GetComponent<AnimPlayer>().entity = Ghost;
        Swap(controllingGolem);
    }
    
    public virtual void Update(bool force = false)
    {
        
        bool notMoving = GolemIsBusy();
        if (ControllingGolem || force)
        {
            if(notMoving)
            {
                Ghost.myAgent.Warp(Golem.transform.position);
                Ghost.LockElevator();
            }
            else
            {
                Golem.state = AIStates.MovingTo;
                var target = Ghost.transform.position;
                Golem.MoveTo(target);
                Golem.LookAt(target);
                Ghost.UnlockElevator();
            }
        }
        Vector3 forcedMovement = GetNumpadVector3();
        if (forcedMovement.magnitude > 0)
        { 
            Player.myAgent.Warp(Player.transform.position + forcedMovement);
        }

        Status += $"{Golem.state.ToString()}";
        if (Golem.destination != null) Status += $"{Golem.destination.position.ToString()}";
    }

    public void Swap()
    {
        Swap(!ControllingGolem);
    }

    public virtual void Swap(bool toGolem)
    {
        if (!GolemIsBusy())
        {
            ControllingGolem = toGolem;
            Ghost.myAgent.Warp(Golem.transform.position);
            Ghost.hasControl = toGolem;
            Player.hasControl = !toGolem;
        }


    }

    protected bool GolemIsBusy()
    {
        return Golem.forceMeditate || Golem.isControlledByElevator;
    }

    private Vector3 GetNumpadVector3()
    {
        Vector3 position = Vector3.zero;
        position += VectorIfNumPressed(2, Vector3.back / 16);
        position += VectorIfNumPressed(4, Vector3.left / 16);
        position += VectorIfNumPressed(6, Vector3.right / 16);
        position += VectorIfNumPressed(8, Vector3.forward / 16);
        return position;
    }

    private Vector3 VectorIfNumPressed(int num, Vector3 direction)
    {
        return Input.GetKey($"[{num}]") ? direction : Vector3.zero;
    }
}