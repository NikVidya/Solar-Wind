using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnManager : MonoBehaviour {
    public Transform checkPoint;
    public PlayerEntity player;
    public void PlayerDeath() {
        player.ResetMovement("right");
        player.Respawn(checkPoint);
    }
}
