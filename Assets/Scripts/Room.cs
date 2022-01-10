using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public abstract class Room {
    public float minSize;
    public float sectionWeight;
    public int priority, index, weight, id;
    public List<Block> surface;
    public List<List<Block>> blockSegments;
    public static Queue<Room> parents = new Queue<Room>();
    public static Room curRoom = null;
    public static int roomCounter = 1;
    public List<Room> left, right, top, bot;
    public int lastAdded;
    public PublicRoom parent;

    protected Room(PublicRoom parent) {
        weight = (int) minSize;
        this.parent = parent;
        blockSegments = new List<List<Block>>();
        left = new List<Room>();
        right = new List<Room>();
        top = new List<Room>();
        bot = new List<Room>();

}

public override string ToString() {
        return index.ToString();
    }

    public abstract int size();
    public abstract float grow(float g, float leftovers);
    public abstract void getNext();
    public void confirmRoom() {
        id = Room.roomCounter;
        Room.roomCounter++;
    }
    public abstract void swap(Room r);


    public float addBlockSegment(List<Block> blocks) {
        weight -= blocks.Count;
        blockSegments.Add(blocks);
        if (weight <= 0) {
            unwrap();
            this.getNext();
            return weight;
        }
        return 0;
    }

    public void unwrap() {
        int blockCount = 0;
        foreach (List<Block> i in blockSegments) blockCount += i.Count;
        surface = new List<Block>(blockCount);
        foreach (List<Block> i in blockSegments) surface.AddRange(i);
    }
}

public abstract class PublicRoom : Room {
    public List<PublicRoom> publicRooms;
    public List<PrivateRoom> privateRooms;
    private int searchIndex = 0;
    bool completedPublic = false;

    protected PublicRoom(PublicRoom parent) : base(parent) {
        this.publicRooms = new List<PublicRoom>();
        this.privateRooms = new List<PrivateRoom>();
    }

    public bool injectHallway(Hallway hallway) {
        if (canHaveHallway()) {
            addHallway(hallway);
            return true;
        }
        foreach (PublicRoom i in publicRooms) if (i.injectHallway(hallway)) return true;

        // This might be a problem if all Foyer rooms are public rooms but very unlikely
        if (this is Foyer) addHallway(hallway);

        return false;
    }

    private bool canHaveHallway() {
        if (this is Foyer || this is Hallway || publicRooms.Count == 3) return false;
        foreach (PublicRoom i in publicRooms) if (i is Hallway) return false;
        return false;
    }

    private void addHallway(Hallway hallway) {
        PrivateRoom tmpRoom = privateRooms.Last();
        privateRooms.RemoveAt(privateRooms.Count - 1);
        publicRooms.Add(hallway);
        hallway.privateRooms.Add(tmpRoom);
    }

    public void add(Room room) {
        if (room is PublicRoom) this.publicRooms.Add((PublicRoom)room);
        else if (room is PrivateRoom) this.privateRooms.Add((PrivateRoom)room);
    }

    public override int size() {
        int s = weight;
        foreach (PublicRoom i in publicRooms) s += i.size();
        foreach (PrivateRoom i in privateRooms) s += i.size();

        return s;
    }

    public override float grow(float g, float leftovers) {
        float scaledWeight = weight * g + leftovers;
        weight = (int)scaledWeight;
        leftovers = scaledWeight - weight;
        foreach (PublicRoom i in publicRooms) leftovers = i.grow(g, leftovers);
        foreach (PrivateRoom i in privateRooms) leftovers = i.grow(g, leftovers);
        return leftovers;
    }
    public override void getNext() {
        if (!completedPublic && searchIndex == publicRooms.Count) {
            searchIndex = 0;
            completedPublic = true;
        }
        if (completedPublic && searchIndex == privateRooms.Count) {
            // Final room has been visited
            //if (Room.parents.Count == 0) {
            //    curRoom = null;
            //    return;
            //} 
            //curRoom = Room.parents.Dequeue();
            //curRoom.getNext();
            curRoom = null;
            completedPublic = false;
            searchIndex = 0;
            return;
        }

        if (!completedPublic) curRoom = publicRooms[searchIndex];
        else if (completedPublic) curRoom = privateRooms[searchIndex];

        searchIndex++;
        //Room.parents.Enqueue(this);
        //if (Room.parents.Count > 0) 
        //else curRoom = null;
    }

    public void adjustRooms(PublicRoom r1, PublicRoom r2) {
        for (int i = 0; i < publicRooms.Count; i++)
            if (publicRooms[i] == r1) publicRooms[i] = r2;
    }
    public void adjustRooms(PrivateRoom r1, PrivateRoom r2) {
        for (int i = 0; i < privateRooms.Count; i++)
            if (privateRooms[i] == r1) privateRooms[i] = r2;
    }

    public override void swap(Room r) {
        if(r is PublicRoom) {
            PublicRoom tmpr = (PublicRoom)r;
            List<PublicRoom> tmpPub = publicRooms;
            List<PrivateRoom> tmpPriv = privateRooms;
            PublicRoom tmpPar = parent;

            parent.adjustRooms(this, tmpr);
            tmpr.parent.adjustRooms(tmpr, this);

            this.publicRooms = tmpr.publicRooms;
            privateRooms = tmpr.privateRooms;
            parent = tmpr.parent;

            tmpr.publicRooms = tmpPub;
            tmpr.privateRooms = tmpPriv;
            tmpr.parent = tmpPar;
        }
    }


}

public abstract class PrivateRoom : Room {
    public PrivateRoom(PublicRoom parent) : base(parent) { }

    public override int size() {return weight;}
    public override float grow(float g, float leftovers) {
        float scaledWeight = weight * g + leftovers;
        weight = (int)scaledWeight;
        leftovers = scaledWeight - weight;

        return leftovers;
    }
    public override void getNext() { curRoom = Room.parents.Dequeue(); curRoom.getNext(); }

    public override void swap(Room r) {
        if (r is PrivateRoom) {
            PrivateRoom tmpr = (PrivateRoom)r;

            parent.adjustRooms(this, tmpr);
            tmpr.parent.adjustRooms(tmpr, this);
        }
    }
}


public class Foyer : PublicRoom {
    public Foyer(PublicRoom parent) : base(parent) {
        minSize = 4f;
        weight = (int) (minSize / 0.01f);
        priority = 2;
        index = 0;
    }

    public override string ToString() { return "Foyer"; }
}

public class Hallway : PublicRoom {
    public Hallway(PublicRoom parent) : base(parent) {
        minSize = 1;
        weight = (int)(minSize / 0.01f);
        priority = 2;
        index = 1;
    }

    public override string ToString() { return "Hallway"; }

}

public class LivingRoom : PublicRoom {
    public LivingRoom(PublicRoom parent) : base(parent) {
        minSize = 6.5f * FloorPlacer.residents;
        weight = (int)(minSize / 0.01f);
        priority = 0;
        index = 2;
    }

    public override string ToString() { return "Living Room"; }

}

public class DiningRoom : PublicRoom {
    public DiningRoom(PublicRoom parent) : base(parent) {
        minSize = 10f * FloorPlacer.residents;
        weight = (int)(minSize / 0.01f);
        priority = 2;
        index = 3;
    }

    public override string ToString() { return "Dining Room"; }

}

public class Kitchen : PrivateRoom {
    public Kitchen(PublicRoom parent) : base(parent) {
        minSize = 5f;
        weight = (int)(minSize / 0.01f);
        priority = 0;
        index = 4;
    }

    public override string ToString() { return "Kitchen"; }

}

public class Bathroom : PrivateRoom {
    public Bathroom(PublicRoom parent) : base(parent) {
        minSize = 2f;
        weight = (int)(minSize / 0.01f);
        priority = 1;
        index = 5;
    }

    public override string ToString() { return "Bathroom"; }

}


public class Bedroom : PrivateRoom {
    public Bedroom(PublicRoom parent) : base(parent) {
        minSize = 4.5f * FloorPlacer.residents;
        weight = (int)(minSize / 0.01f);
        priority = 0;
        index = 6;
    }

    public override string ToString() { return "Bedroom"; }

}

public class Office : PrivateRoom {
    public Office(PublicRoom parent) : base(parent) {
        minSize = 2.5f;
        weight = (int)(minSize / 0.01f);
        priority = 3;
        index = 7;
    }

    public override string ToString() { return "Office"; }

}
