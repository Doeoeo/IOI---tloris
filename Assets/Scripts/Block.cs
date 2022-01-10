using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block {
    public float x { get; set; }
    public float y { get; set; }
    public int roomIndex { get; set; }

    public static List<Wall> activeWalls = new List<Wall>();

    public Room room;

    private Wall[] walls = new Wall[4]; 
    public bool isUsed { get; set; }
    public Block(float x, float y) {
        this.x = x;
        this.y = y;
        roomIndex = -1;
        walls[0] = new Wall(x, y + FloorPlacer.blockSize / 2);              // Top
        walls[1] = new Wall(x + FloorPlacer.blockSize / 2, y);              // Right
        walls[2] = new Wall(x, y - FloorPlacer.blockSize / 2);              // Bot
        walls[3] = new Wall(x - FloorPlacer.blockSize / 2, y);              // Left
        this.isUsed = false;
    }

    public bool isContained(float x1, float x2, float y1, float y2) {
        if (x1 < x && x2 > x && y1 < y && y2 > y) return true;
        return false;
    }

    public void topWall(int i = 0) { walls[i].isUsed = true; activeWalls.Add(walls[i]); }
    public void rightWall(int i = 1) { walls[i].isUsed = true; activeWalls.Add(walls[i]); }
    public void botWall(int i = 2) { walls[i].isUsed = true; activeWalls.Add(walls[i]); }
    public void leftWall(int i = 3) { walls[i].isUsed = true; activeWalls.Add(walls[i]); }


    public bool isContainedTriangle(float x1, float x2) {
        if (x1 < x2 && x1 < x && x2 > x) return true;
        if (x1 > x2 && x2 < x && x1 > x) return true;
        else return false;
    }
}
