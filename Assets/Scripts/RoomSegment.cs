using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RoomSegment {
    public static Queue<RoomSegment> roomOrder;

    public float x1, x2, y1, y2, w;
    public RoomSegment[] subRooms;
    public RoomSegment parent;


    public List<RoomSegment> rooms;
    public List<Block> blocks;
    public Room room;
    public float2 entrance;
    public Vector2Int bl, tr;

    public RoomSegment(Vector2Int bl, Vector2Int tr, RoomSegment parent) {
        this.bl = bl;
        this.tr = tr;
        this.parent = parent;
        rooms = new List<RoomSegment>();
        blocks = new List<Block>();
    }

    public int xLen() { return tr.x - bl.x; }
    public int yLen() { return tr.y - bl.y; }

    public float realSize() { return blocks.Count * math.pow(FloorPlacer.blockSize, 2); }
    public int size() { return blocks.Count; }

    public void roomVisitor() {
        if (Room.curRoom == null) return;
        if (this.size() <= Room.curRoom.weight) {
            Room.curRoom.addBlockSegment(blocks);
            return;
        }
        foreach (RoomSegment i in rooms) i.roomVisitor();
        if (this.parent == null && Room.curRoom != null) { 
            Room.curRoom.unwrap(); 
        }
    }


}
