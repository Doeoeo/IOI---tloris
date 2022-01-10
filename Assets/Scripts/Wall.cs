using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall {
    public float x { get; set; }
    public float y { get; set; }

    public bool isUsed { get; set; }
    public Wall(float x, float y) {
        this.x = x;
        this.y = y;
        this.isUsed = false;
    }

}
