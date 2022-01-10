using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System;
using System.Linq;
using UnityEngine.UI;

public class FloorPlacer : MonoBehaviour {
    [SerializeField] private Camera camera;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    [SerializeField] private Material wallMaterial;

    [SerializeField] private Material hallway;
    [SerializeField] private Material livingRoom;
    [SerializeField] private Material diningRoom;
    [SerializeField] private Material kitchen;
    [SerializeField] private Material bathroom;
    [SerializeField] private Material bedroom;
    [SerializeField] private Material office;

    [SerializeField] private Canvas canvasUi;
    [SerializeField] private InputField residentsInput;
    [SerializeField] private Text uiSurface;

    [SerializeField] private Canvas optionalDisplay;
    [SerializeField] private Text optionalDisplayData;
    private static Material[] roomMaterials;


    public static float blockSize = 0.1f, wallSize = 0.7f;

    EntityArchetype blockArchetype, wallArchetype;
    private static EntityArchetype coloredBlockArchetype;
    EntityManager entityManager;
    Vector3 clickPressed, clickReleased;
    bool clicked = false, released = false, building = true;

    private LineRenderer lineRenderer;

    private float minX = 100, minY = 100, maxX = -100, maxY = -100;

    private float[] roomSizes = { 0.2f, 0.5f, 0.2f, 0.1f };
    int roomIndex = 0;
    float progress = 0;
    RoomSegment[] rooms;
    Foyer entrance;
    List<RoomSegment> roomList;
    public static float4[] roomArray;
    public static float2[] doors;
    public static int[] roomTypes;
    public static float4 wallBorder;
    public static bool completed = false;
    public static BlockComponent[] blockData;
    public static Translation[] blockPositions;

    private static float statBlockSize;
    private static Mesh statMesh;
    public static FloorPlacer instance;

    float curSum;

    // UI
    public int[] numberOfRooms = { 
        1, // Bathrooms
        1, // Bedrooms
        1, // Diningrooms
        0, // Hallway
        1, // Kitchen
        1, // Livingroom
        1  // Office
    };
    public static float residents;
    // Selection
    GameObject selection;
    bool placing = false, selected = false;
    float3 clickStart, clickEnd, selectionMove;

    Room shownRoom;
    bool swap = false;
    // Drawing
    private Block[,] floor;
    private List<Block> structure;
    private float startX = -30f, startY = -25f, surface = 0;
    int width = 60, height = 50;
    private Func<int>[] drawModes;
    private Func<int>[] spawnModes;
    private int drawModeIndex = 0;
    public int availableSurface = 0;
    int flagCounter = 0;
    Vector2Int bl, tr;
    void Start() {
        instance = this;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blockArchetype = entityManager.CreateArchetype(
            typeof(BlockComponent),
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld), 
            typeof(RenderBounds),
            typeof(NonUniformScale)
        );

        wallArchetype = entityManager.CreateArchetype(
            typeof(WallComponent),
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(NonUniformScale)
        );

        coloredBlockArchetype = entityManager.CreateArchetype(
            typeof(ColoredBlockComponent),
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(NonUniformScale)
        );

        numberOfRooms = new int[7] {
            1, // Bathrooms
            1, // Bedrooms
            1, // Diningrooms
            0, // Hallway
            1, // Kitchen
            1, // Livingroom
            1  // Office
        };
        
        drawModes = new Func<int>[] {
            drawRect,
            drawTriangle,
        };
        spawnModes = new Func<int>[] {
            spawnBlocks,
            spawnTriangles,
        };

        optionalDisplay.enabled = false;


        structure = new List<Block>();
        bl = new Vector2Int(1000, 1000);
        tr = new Vector2Int(-1000, -1000);

        roomMaterials = new Material[] {
            hallway,
            hallway,
            livingRoom,
            diningRoom,
            kitchen,
            bathroom,
            bedroom,
            office
        };

        lineRenderer = GetComponent<LineRenderer>();
        statBlockSize = blockSize;
        statMesh = mesh;


       
        floor = new Block[height * 10, width * 10];
        for (int i = 0; i < floor.GetLength(1); i++) 
            for (int j = 0; j < floor.GetLength(0); j++)
                floor[j, i] = new Block(startX + (float)(i * width) / floor.GetLength(1), startY + (float)(j * height) / floor.GetLength(0));
        
        Debug.Log("Plane split on: " + height * width * 100 + " segments");
        Debug.Log("Surface size: " + height * width * 100 * blockSize * blockSize + " m^2");

        
    }

    public void updateRoomNumbers(String tmp) {
        InputField[] fields = canvasUi.GetComponentsInChildren<InputField>();
        for (int i = 0; i < fields.Length; i++) {
            if(fields[i].text.Length !=0)
                numberOfRooms[i] = int.Parse(fields[i].text);
        }
        Debug.Log(string.Join(", ", numberOfRooms));
        int testInput = 1;
        try { testInput = int.Parse(residentsInput.text); } catch { }
        residents = math.max(1 + (testInput - 1) * 0.3f, 1);
    }

    // Update is called once per frame
    void Update() {
        //updateRoomNumbers("a");
        if (building && clicked) drawModes[drawModeIndex]();
        if (building && placing && Input.GetKeyDown(KeyCode.Return)) spawnModes[drawModeIndex]();
        if (building && Input.GetKeyDown("tab")) drawModeIndex = (drawModeIndex + 1) % 2;
        if (building && Input.GetKeyDown("space")) {
            building = false;
            updateRoomNumbers("a");
            Debug.Log("People " + FloorPlacer.residents);
            entrance = generateRooms(0);
            Debug.Log("Made " + Room.roomCounter + " Rooms ");
            //RoomSegment root = initialSplit(new RoomSegment(bl, tr, null));
            //filterSplit(root);
            int roomSurface = entrance.size();
            //int availableSurface = root.size();
            if(availableSurface < roomSurface) {
                Debug.Log("ROOM IS TOO SMALL TO FIT EVERYTHING");
                return;
            }
            float roomToGrow = (float)(availableSurface) / roomSurface;
            entrance.grow(roomToGrow, 0);
            Room.curRoom = entrance;
            RoomSegment root = mySplitX(new RoomSegment(bl, tr, null));

            //roomShifter(entrance);

            clearBlocks();
            //entityCreator(root.blocks);
            assignedRoomBuilder(entrance);
            findWalls();
            spawnWalls();
            completed = true;
        }
        if (completed) {


        }

    }

    #region building
    private void OnMouseDown() {
        clickPressed = clickPos();
        if (!building) {
            int xIndex = (int)math.floor((clickPressed.x - startX) * floor.GetLength(1) / width);
            int yIndex = (int)math.floor((clickPressed.z - startY) * floor.GetLength(0) / height);
            if (floor[yIndex, xIndex].isUsed) {
                if (swap) {
                    Room r1 = shownRoom;
                    Room r2 = floor[yIndex, xIndex].room;

                    r1.swap(r2);
                    Room.curRoom = entrance;
                    RoomSegment root = mySplitX(new RoomSegment(bl, tr, null));

                    //roomShifter(entrance);

                    clearBlocks();
                    //entityCreator(root.blocks);
                    assignedRoomBuilder(entrance);
                    findWalls();
                    spawnWalls();
                    optionalDisplay.enabled = false;
                } else {
                    optionalDisplay.enabled = true;
                    shownRoom = floor[yIndex, xIndex].room;
                    optionalDisplayData.text = "Type: " + floor[yIndex, xIndex].room + "\n"
                                         + "Size: " + math.trunc(floor[yIndex, xIndex].room.surface.Count * math.pow(blockSize, 2)) + "m^2";
                }
            } 
        }
        clicked = true;
        if (placing) {
            if (math.abs(selection.transform.position.x - clickPressed.x) <= selection.transform.localScale.x  &&
                math.abs(selection.transform.position.z - clickPressed.z) <= selection.transform.localScale.z) 
                selected = true;
           

            return;
        }
        if (clickPressed.y == -1000) return;



        // Draw selection 
        selection = GameObject.CreatePrimitive(PrimitiveType.Plane);
        selection.transform.localScale = math.abs(clickPressed - clickReleased) / 10;
        selection.transform.localScale = new float3(0);
        selection.GetComponent<Renderer>().material = bathroom;
        selection.transform.position = (clickPressed + clickReleased) / 2;

        clickStart = clickPressed;
        selectionMove = new float3(0);
    }

    public void swapPressed() {
        Debug.Log("Ready to swap");
        swap = true;
    }

    private void OnMouseUp() {
        clicked = false;
        if (placing) {
            selected = false;
            return;
        }
        clickReleased = clickPos();
        if (clickPressed.y == -1000) return;
        Debug.Log("Released");
        
        placing = true;
        clickEnd = clickReleased;

    }



    private Vector3 clickPos() {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out RaycastHit raycastHit)) {
            Vector3 click = raycastHit.point;
            if (click.x > -28 && click.x < -19 && click.z > -8 && click.z < 3.8) return new Vector3(0, -1000, 0);
            return raycastHit.point;
        } 

        Debug.Log("Invalid click location");
        return new Vector3(0, 0, 0);
    }

    private int drawRect() {
        Vector3 curPossition = clickPos();
        if (selected) {
            selectionMove += (float3)(curPossition - selection.transform.position);
            selection.transform.position = curPossition;
            return 0;
        }

        if (!placing) {
            selection.transform.localScale = math.abs(clickPressed - curPossition) / 10;
            selection.transform.position = (clickPressed + curPossition) / 2;
            return 0;
        }
        return 0;
    }

    private int drawTriangle() {
        Vector3 curPossition = clickPos();
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(clickPressed.x, clickPressed.y + 0.8f, clickPressed.z));
        lineRenderer.SetPosition(1, new Vector3(clickPressed.x, clickPressed.y + 0.8f, clickPressed.z + 2 * (curPossition.z - clickPressed.z)));
        lineRenderer.SetPosition(2, new Vector3(clickPressed.x + 2 * (curPossition.x - clickPressed.x), clickPressed.y + 0.8f, clickPressed.z));
        lineRenderer.SetPosition(3, new Vector3(clickPressed.x, clickPressed.y + 0.8f, clickPressed.z));

        return 0;
    }



    private int spawnBlocks() {
        placing = false;
        clickPressed = clickStart + selectionMove;
        clickReleased = clickEnd + selectionMove;
        minX = 100; minY = 100; maxX = -100; maxY = -100;
        float signX = math.sign(clickReleased.x - clickPressed.x), signY = math.sign(clickReleased.z - clickPressed.z);
        if (signX > 0) {
            if (clickPressed.x < minX) minX = clickPressed.x;
            if (clickReleased.x > maxX) maxX = clickReleased.x;
        } else {
            if (clickReleased.x < minX) minX = clickReleased.x;
            if (clickPressed.x > maxX) maxX = clickPressed.x;
        }
        if (signY > 0) {
            if (clickPressed.y < minY) minY = clickPressed.z;
            if (clickReleased.y > maxY) maxY = clickReleased.z;
        } else {
            if (clickReleased.y < minY) minY = clickReleased.z;
            if (clickPressed.y > maxY) maxY = clickPressed.z;
        }
        List<Block> toBuild = new List<Block>();
        int xIndex = (int) math.floor((minX - startX) * floor.GetLength(1) / width);
        int xEnd = (int) math.ceil((maxX - startX) * floor.GetLength(1) / width);
        int yIndex = (int) math.floor((minY - startY) * floor.GetLength(0) / height);
        int yEnd= (int) math.ceil((maxY - startY) * floor.GetLength(0) / height);
        for (int i = xIndex; i < xEnd; i++)
            for (int j = yIndex; j < yEnd; j++)
                if (floor[j, i].isContained(minX, maxX, minY, maxY)) {
                    updateMinMax(i, j);
                    toBuild.Add(floor[j, i]);
                }

        entityCreator(toBuild);
        uiSurface.text = "Surface: " + math.trunc(availableSurface * blockSize * blockSize) + "m^2";
        return 0;
    }

    private int spawnTriangles() {
        Debug.Log("Yes");
        Vector3 p1 = clickPressed, p2 = new Vector3(clickPressed.x, clickPressed.y + 0.8f, clickPressed.z + 2 * (clickReleased.z - clickPressed.z)),
            p3 = new Vector3(clickPressed.x + 2 * (clickReleased.x - clickPressed.x), clickPressed.y + 0.8f, clickPressed.z);

        List<Block> toBuild = new List<Block>();
        int signX = (int) math.sign(p3.x - p1.x), signY = (int) math.sign(p2.z - p1.z);
        int xIndex = (int) math.floor((p1.x - startX) * floor.GetLength(1) / width);
        int xEnd = (int)math.ceil((p3.x - startX) * floor.GetLength(1) / width);
        int yIndex = (int)math.floor((p1.z - startY) * floor.GetLength(0) / height);
        int yEnd = (int)math.ceil((p2.z - startY) * floor.GetLength(0) / height);
        float len = math.abs(xIndex - xEnd), incY = len / math.abs(yIndex - yEnd);

        for (int i = xIndex; compare(i, xEnd, signX); i += signX) 
            for (int j = yIndex; compare(j, yEnd, signY); j += signY) 
                if (math.abs(i - xIndex) <= len - (math.abs((j - yIndex) * incY))) {
                    updateMinMax(i, j);
                    toBuild.Add(floor[j, i]);
                }

        entityCreator(toBuild);

        return 0;
    }

    private bool compare(int i, int end, int sign) {
        if (sign > 0) return i < end;
        else return i > end;
    }

    private void entityCreator(List<Block> toBuild) {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(toBuild.Count, Allocator.Temp);
        entityManager.CreateEntity(blockArchetype, entityArray);

        for (int i = 0; i < entityArray.Length; i++) {
            Entity e = entityArray[i];
            if (!toBuild[i].isUsed) {
                surface += blockSize * blockSize;
                availableSurface++;
            }
            toBuild[i].isUsed = true;
            entityManager.SetComponentData(e, new BlockComponent {
                index = 0,
                covered = 0,
                isBorder = false
            });
            entityManager.SetComponentData(e, new Translation { Value = new float3(toBuild[i].x, clickPressed.y, toBuild[i].y) });
            entityManager.SetComponentData(e, new NonUniformScale { Value = new float3(blockSize) });
            entityManager.SetSharedComponentData(e, new RenderMesh {
                mesh = mesh,
                material = material
            });
        }
    }

    private void updateMinMax(int x, int y) {
        if (x < bl.x) bl.x = x;
        if (x > tr.x) tr.x = x;
        if (y < bl.y) bl.y = y;
        if (y > tr.y) tr.y = y;
    }

    private void assignedRoomBuilder(Room room) {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(room.surface.Count, Allocator.Temp);
        entityManager.CreateEntity(blockArchetype, entityArray);
        for (int i = 0; i < entityArray.Length; i++) {
            Entity e = entityArray[i];
            entityManager.SetComponentData(e, new BlockComponent {
                index = 0,
                covered = 0,
                isBorder = false
            });
            entityManager.SetComponentData(e, new Translation { Value = new float3(room.surface[i].x, clickPressed.y, room.surface[i].y) });
            entityManager.SetComponentData(e, new NonUniformScale { Value = new float3(blockSize) });
            entityManager.SetSharedComponentData(e, new RenderMesh {
                mesh = statMesh,
                material = roomMaterials[room.index]
            });
        }

        if (room is PrivateRoom) return;
        PublicRoom pRoom = (PublicRoom)room;

        foreach (PublicRoom i in pRoom.publicRooms) assignedRoomBuilder(i);
        foreach (PrivateRoom i in pRoom.privateRooms) assignedRoomBuilder(i);
    }

    private void clearBlocks() {
        NativeArray<Entity> entities = entityManager.GetAllEntities();
        foreach (Entity e in entities) entityManager.DestroyEntity(e);
        entities.Dispose();
    }

    #endregion building

    #region generate rooms

    private Foyer generateRooms(int counter2) {
        int[] tmpRoomCounter = new int[numberOfRooms.Length];
        Array.Copy(numberOfRooms, tmpRoomCounter, numberOfRooms.Length);
        Room.roomCounter = 0;
        List<PublicRoom> leaves = new List<PublicRoom>();
        Foyer root = new Foyer(null);
        leaves.Add(root);
        root.confirmRoom();
        Func<PublicRoom, Room>[] additions = {
            makeBathroom,
            makeBedroom,
            makeDiningRoom,
            makeHallway,
            makeKitchen,
            makeLivingRoom,
            makeOffice
        };
        List<int> roomMap = (new int[] { 0, 1, 2, 3, 4, 5, 6 }).OfType<int>().ToList();
        for (int i = tmpRoomCounter.Length - 1; i >= 0; i--) if (tmpRoomCounter[i] == 0) roomMap.RemoveAt(i);
        while (roomMap.Count > 0) {
            int i = UnityEngine.Random.Range(0, roomMap.Count);
            Room room = additions[roomMap[i]](null);

            int j = reroll(room is PublicRoom, leaves);
            if (j == -1) {
                continue;
            }
            tmpRoomCounter[roomMap[i]]--;
            if (tmpRoomCounter[roomMap[i]] == 0) {
                roomMap.RemoveAt(i);
            }

            leaves[j].add(room);
            room.parent = leaves[j];
            room.confirmRoom();
            if (leaves[j].publicRooms.Count + leaves[j].privateRooms.Count == 3) leaves.RemoveAt(j);
            if (room is PublicRoom) leaves.Add((PublicRoom)room);
            if (leaves.Count == 0 && roomMap.Count != 0) {
                Hallway seperator = makeHallway(null);
                leaves.Add(seperator);
                root.injectHallway(seperator);
                seperator.confirmRoom();
            }
        }
        if (counter2 > 30) {
            Debug.Log("CAN'T MAKE ROOMS");
            return root; 
        }
        if(roomMap.Count != 0) return generateRooms(counter2 + 1);
        return root;
    }

    private int reroll(bool isPublic, List<PublicRoom> leaves) {
        int roll, rng = 0;
        while (true) {
            roll = UnityEngine.Random.Range(0, leaves.Count);
            Debug.Log(roll + " into " + leaves.Count);
            if (leaves[roll].publicRooms.Count + leaves[roll].privateRooms.Count < 3) return roll;
            //if (isPublic && leaves[roll].publicRooms.Count < 3) return roll;
            //else if (!isPublic && leaves[roll].privateRooms.Count < 3) return roll;
            rng++;
            if (rng > 10) return -1;
        }
    }


    private void extract(List<RoomSegment> rooms, RoomSegment room) {
        if (room.room != null) rooms.Add(room);
        if (!room.rooms.Any()) return;

        foreach (RoomSegment i in room.rooms) extract(rooms, i);
    }

    #region garbage
    private Hallway makeHallway(PublicRoom f) { return new Hallway(f); }
    private LivingRoom makeLivingRoom(PublicRoom f) { return new LivingRoom(f); }
    private DiningRoom makeDiningRoom(PublicRoom f) { return new DiningRoom(f); }
    private Kitchen makeKitchen(PublicRoom f) { return new Kitchen(f); }
    private Bathroom makeBathroom(PublicRoom f) { return new Bathroom(f); }
    private Bedroom makeBedroom(PublicRoom f) { return new Bedroom(f); }
    private Office makeOffice(PublicRoom f) { return new Office(f); }

    #endregion garbage

    #endregion generate rooms

    #region seperation

    // Split the initial surface to equal sized squares
    private RoomSegment initialSplit(RoomSegment roomSegment) {
        bool direction = adjustWindow(roomSegment);
        int x = roomSegment.xLen(), y = roomSegment.yLen();
        if (direction) {
            int splitRatio = x / y;
            for (int i = 0; i < splitRatio; i++) 
                roomSegment.rooms.Add(baseSplit(
                    roomSegment: new RoomSegment(
                        bl : new Vector2Int(roomSegment.bl.x + i * y, roomSegment.bl.y),
                        tr : new Vector2Int(roomSegment.bl.x + (i + 1) * y, roomSegment.tr.y),
                        parent : roomSegment), 
                direction: false));
        } else {
            int splitRatio = y / x;
            for (int i = 0; i < splitRatio; i++)
                roomSegment.rooms.Add(baseSplit(
                    roomSegment: new RoomSegment(
                        bl: new Vector2Int(roomSegment.bl.x, roomSegment.bl.y + i * x),
                        tr: new Vector2Int(roomSegment.tr.x, roomSegment.bl.y + +(i + 1) * x),
                        parent: roomSegment),
                direction: false));
        }
        // Update contained blocks
        foreach (RoomSegment i in roomSegment.rooms) roomSegment.blocks.AddRange(i.blocks);

        return roomSegment;
    }

    // Fit window to a whole number divisable size
    private bool adjustWindow(RoomSegment roomSegment) {
        int x = roomSegment.xLen(), y = roomSegment.yLen();
        int xd = (int)math.pow(2, math.ceil(math.log2(x)));
        int yd = (int)math.pow(2, math.ceil(math.log2(y)));
        int incX = (int)(xd - x), incY = (int)(yd - y);
        roomSegment.bl.x -= incX / 2;
        roomSegment.tr.x += incX - incX / 2;
        roomSegment.bl.y -= incY / 2;
        roomSegment.tr.y += incY - incY / 2;
        return x >= y;
        /*
        if (x > y) {
            int inc = y - x % y;
            roomSegment.tr.x += inc;
            return true;
        } else {
            int inc = x - y % x;
            roomSegment.tr.y += inc;
            return false;
        }*/
    }

    // Split equal sized squares on smaller squares
    private RoomSegment baseSplit(RoomSegment roomSegment, bool direction) {
        if (direction) {
            int distance = roomSegment.tr.x - roomSegment.bl.x;
            int distance2 = roomSegment.tr.y - roomSegment.bl.y;

            if (distance == 0) {
                if (roomSegment.bl.y < floor.GetLength(0) && roomSegment.bl.x < floor.GetLength(1) && floor[roomSegment.bl.y, roomSegment.bl.x].isUsed) {
                    roomSegment.blocks.Add(floor[roomSegment.bl.y, roomSegment.bl.x]);

                }
                return roomSegment;
            }
            int split = distance / 2 - 1 + distance % 2;

            roomSegment.rooms.Add(baseSplit(new RoomSegment(roomSegment.bl, new Vector2Int(roomSegment.bl.x + split, roomSegment.tr.y), roomSegment), !direction));
            roomSegment.rooms.Add(baseSplit(new RoomSegment(new Vector2Int(roomSegment.bl.x + split + 1, roomSegment.bl.y), roomSegment.tr, roomSegment), !direction));
        }
        else {
            int distance = roomSegment.tr.y - roomSegment.bl.y;
            int distance2 = roomSegment.tr.x - roomSegment.bl.x;

            if (distance == 0) {
                if (roomSegment.bl.y < floor.GetLength(0) && roomSegment.bl.x < floor.GetLength(1) && floor[roomSegment.bl.y, roomSegment.bl.x].isUsed) {
                    roomSegment.blocks.Add(floor[roomSegment.bl.y, roomSegment.bl.x]);


                }
                return roomSegment;
            }
            int split = distance / 2 - 1 + distance % 2;

            roomSegment.rooms.Add(baseSplit(new RoomSegment(roomSegment.bl, new Vector2Int(roomSegment.tr.x, roomSegment.bl.y + split), roomSegment), !direction));
            roomSegment.rooms.Add(baseSplit(new RoomSegment(new Vector2Int(roomSegment.bl.x, roomSegment.bl.y + split + 1), roomSegment.tr, roomSegment), !direction));
        }

        foreach (RoomSegment i in roomSegment.rooms) roomSegment.blocks.AddRange(i.blocks);

        return roomSegment;
    }

    private RoomSegment mySplitX(RoomSegment roomSegment, int block = -1) {
        int leftInc = 0;
        Room curRoom = Room.curRoom;
        roomSegment.room = curRoom;
        if (curRoom is PublicRoom) {
            curRoom.getNext();
            Room subRoom = Room.curRoom;
            if(subRoom == null) {
                placeRoom(roomSegment.bl, roomSegment.tr, curRoom);
                return roomSegment;
            }
            int subSize = subRoom.size(), subWeight = subRoom.weight; 
            leftInc = roomSegment.bl.x + subSize / roomSegment.yLen();
            // Left split
            roomSegment.rooms.Add(mySplitY(
                new RoomSegment (
                    bl : roomSegment.bl,
                    tr : new Vector2Int(leftInc, roomSegment.tr.y),
                    parent : roomSegment
                )
            ));

            // Mid split
            curRoom.getNext();
            subRoom = Room.curRoom;
            if (subRoom == null) {
                placeRoom(new Vector2Int(leftInc, roomSegment.bl.y), roomSegment.tr, curRoom);
                return roomSegment;
            }
            subSize = subRoom.size(); subWeight = subRoom.weight;
            int a = (curRoom.weight + subSize) / roomSegment.yLen(), b = subSize / a, c = roomSegment.yLen() - b;
            roomSegment.rooms.Add(mySplitX(
                new RoomSegment(
                    bl: new Vector2Int(leftInc, roomSegment.bl.y + c),
                    tr: new Vector2Int(leftInc + a, roomSegment.tr.y),
                    parent: roomSegment
                ),
                block : 0
            ));

            // Right split
            curRoom.getNext();
            subRoom = Room.curRoom;
            if (subRoom == null) {
                placeRoom(new Vector2Int(leftInc, roomSegment.bl.y), new Vector2Int(roomSegment.tr.x, roomSegment.bl.y + c), curRoom);
                return roomSegment;
            }
            subSize = subRoom.size(); subWeight = subRoom.weight;
            roomSegment.rooms.Add(mySplitY(
                new RoomSegment(
                    bl: new Vector2Int(leftInc + a, roomSegment.bl.y),
                    tr: roomSegment.tr,
                    parent: roomSegment
                )
            ));
            placeRoom(new Vector2Int(leftInc, roomSegment.bl.y), new Vector2Int(leftInc + a, roomSegment.bl.y + c), curRoom);
        } else {
            placeRoom(new Vector2Int(roomSegment.bl.x, roomSegment.bl.y), roomSegment.tr, curRoom);
        }

        return roomSegment;
    }
    private RoomSegment mySplitY(RoomSegment roomSegment, int block = -1) {
        int leftInc = 0;
        Room curRoom = Room.curRoom;
        roomSegment.room = curRoom;
        if (curRoom is PublicRoom) {
            curRoom.getNext();
            Room subRoom = Room.curRoom;
            if (subRoom == null) {
                placeRoom(new Vector2Int(roomSegment.bl.x, roomSegment.bl.y), roomSegment.tr, curRoom);
                return roomSegment;
            }
            int subSize = subRoom.size(), subWeight = subRoom.weight;
            leftInc = roomSegment.bl.y + subSize / roomSegment.xLen();
            // Left split
            roomSegment.rooms.Add(mySplitX(
                new RoomSegment(
                    bl: roomSegment.bl,
                    tr: new Vector2Int(roomSegment.tr.x, leftInc),
                    parent: roomSegment
                )
            ));

            // Mid split
            curRoom.getNext();
            subRoom = Room.curRoom;
            if (subRoom == null) {
                placeRoom(new Vector2Int(roomSegment.bl.x, leftInc), roomSegment.tr, curRoom);
                return roomSegment;
            }
            subSize = subRoom.size(); subWeight = subRoom.weight;
            int a = (curRoom.weight + subSize) / roomSegment.xLen(), b = subSize / a, c = roomSegment.xLen() - b;
            roomSegment.rooms.Add(mySplitY(
                new RoomSegment(
                    bl: new Vector2Int(roomSegment.bl.x, leftInc),
                    tr: new Vector2Int(roomSegment.bl.x + b, leftInc + a),
                    parent: roomSegment
                )
            ));

            // Right split
            curRoom.getNext();
            subRoom = Room.curRoom;
            if (subRoom == null) {
                placeRoom(new Vector2Int(roomSegment.bl.x + b, leftInc), roomSegment.tr, curRoom);
                return roomSegment;
            }
            subSize = subRoom.size(); subWeight = subRoom.weight;
            roomSegment.rooms.Add(mySplitX(
                new RoomSegment(
                    bl: new Vector2Int(roomSegment.bl.x, leftInc + a),
                    tr: roomSegment.tr,
                    parent: roomSegment
                )
            ));
            placeRoom(new Vector2Int(roomSegment.bl.x + b, leftInc), new Vector2Int(roomSegment.tr.x, leftInc + a), curRoom);
        } else {
            placeRoom(new Vector2Int(roomSegment.bl.x, roomSegment.bl.y), roomSegment.tr, curRoom);
        }
        return roomSegment;
    }

    private void placeRoom(Vector2Int bl, Vector2Int tr, Room room) {
        room.surface = new List<Block>();
        for (int i = bl.x; i < tr.x; i++) {
            for (int j = bl.y; j < tr.y; j++) {
                room.surface.Add(floor[j, i]);
                floor[j, i].roomIndex = room.id;
                floor[j, i].room = room;
            }
        }
    }

    private void filterSplit(RoomSegment roomSegment) {
        List<int> toRemove = new List<int>();
        for (int i = 0; i < roomSegment.rooms.Count; i++)
            if (roomSegment.rooms[i].blocks.Count == 0) toRemove.Add(i);
            else filterSplit(roomSegment.rooms[i]);

        toRemove.Reverse();
        foreach (int i in toRemove) roomSegment.rooms.RemoveAt(i);
    }
    
    private void roomShifter(Room room) {
        int lastRoom = -1, lastAdded = -1;
        for (int i = bl.x - 1; i < tr.x + 1; i++) {
            lastRoom = -1;
            for (int j = bl.y - 1; j < tr.y + 1; j++) {
                Block block = floor[j, i];
                if (lastRoom != -1 && block.roomIndex != -1 && block.roomIndex != lastRoom && lastRoom != block.room.lastAdded) {
                    Debug.Log("Merging " + block.roomIndex + " and " + floor[j - 1, i].roomIndex + " Randomy " + lastRoom);
                    block.room.bot.Add(floor[j - 1, i].room);
                    floor[j - 1, i].room.top.Add(block.room);
                    block.room.lastAdded = floor[j - 1, i].roomIndex;
                }
                lastRoom = block.roomIndex;
            }
        }
        lastRoom = -1;
        for (int j = bl.y - 1; j < tr.y + 1; j++) {
            lastRoom = -1;
            for (int i = bl.x - 1; i < tr.x + 1; i++) {
                Block block = floor[j, i];
                if (lastRoom != -1 && block.roomIndex != -1 && block.roomIndex != lastRoom && lastRoom != block.room.lastAdded) {
                    Debug.Log("Merging " + block.roomIndex + " and " + floor[j - 1, i].roomIndex + " Randomy " + lastRoom);

                    block.room.left.Add(floor[j, i - 1].room);
                    floor[j, i - 1].room.right.Add(block.room);
                    block.room.lastAdded = floor[j - 1, i].roomIndex;
                }
                lastRoom = block.roomIndex;
            }
        }
    }
    
    private void findWalls() {
        int lastRoom = -1;
        for (int i = bl.x - 1; i < tr.x + 1; i++) {
            lastRoom = -1;
            for (int j = bl.y - 1; j < tr.y + 1; j++) {
                if (floor[j, i].roomIndex != lastRoom) floor[j, i].botWall();
                lastRoom = floor[j, i].roomIndex;
            }
        }
        lastRoom = -1;
        for (int j = bl.y - 1; j < tr.y + 1; j++) {
            lastRoom = -1;
            for (int i = bl.x - 1; i < tr.x + 1; i++) {
                if (floor[j, i].roomIndex != lastRoom) floor[j, i].leftWall();
                lastRoom = floor[j, i].roomIndex;
            }
        }
    }

    private void roomMerger(RoomSegment room) {
        if (room.subRooms == null) {
            progress += room.w;
            rooms[roomIndex].rooms.Add(room);
            return;
            //assignNewBorder(room);
        }

        for(int i = 0; i < room.subRooms.Length; i++) {
            roomMerger(room.subRooms[i]);
            
            if (progress >= rooms[roomIndex].w) {
                Debug.Log("Created a room: " + roomIndex + " With area: " + progress);
                rooms[roomIndex].w = progress;
                roomIndex++;
                progress = 0;
            } else Debug.Log("Added: " + progress);
        }
    }

    public void assignNewBorder(RoomSegment rs) {
        if (rooms[roomIndex].x1 > rs.x1) rooms[roomIndex].x1 = rs.x1;
        if (rooms[roomIndex].x2 < rs.x2) rooms[roomIndex].x2 = rs.x2;
        if (rooms[roomIndex].y1 > rs.y1) rooms[roomIndex].y1 = rs.y1;
        if (rooms[roomIndex].y2 < rs.y2) rooms[roomIndex].y2 = rs.y2;
    }

    private float roomVisitor(RoomSegment room) {
        Debug.Log("Visiting a room");
        if (room.subRooms == null) {
            Debug.Log("Found root room with W: " + room.w);
            //spawnWalls(room);
            return room.w;
        }

        float joinedW = 0;
        for (int i = 0; i < room.subRooms.Length; i++) {
            joinedW += roomVisitor(room.subRooms[i]);
        }
        Debug.Log("Joined W: " + joinedW);
        return joinedW;
    }


    private void spawnWalls() {
        NativeArray<Entity> entityArray = new NativeArray<Entity>(Block.activeWalls.Count, Allocator.Temp);
        entityManager.CreateEntity(wallArchetype, entityArray);
        for (int i = 0; i < entityArray.Length; i++) {
            Entity e = entityArray[i];
            entityManager.SetComponentData(e, new Translation { Value = new float3(Block.activeWalls[i].x, clickPressed.y + 0.1f, Block.activeWalls[i].y) });
            entityManager.SetComponentData(e, new NonUniformScale { Value = new float3(blockSize, wallSize, blockSize)});
            entityManager.SetSharedComponentData(e, new RenderMesh {
                mesh = mesh,
                material = wallMaterial
            });

        }

    }

    #endregion seperation

    #region ecs helpers
    public static void endRoomMaker(NativeArray<BlockComponent> q, NativeArray<Translation> t) {
        //completed = false;
        World.DefaultGameObjectInjectionWorld.GetExistingSystem<RoomMarkerBase>().Enabled = false;
        Debug.Log("Closing RoomMaker Base");



    }
    #endregion ecs helpers

    #region block coloring
    public void colorRooms(NativeArray<BlockComponent> q, NativeArray<Translation> t, NativeArray<NonUniformScale> s) {
        blockData = q.ToArray();
        blockPositions = t.ToArray();

        

        NativeArray<Entity> entityArray = new NativeArray<Entity>(q.Length, Allocator.Temp);
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        entityManager.GetComponentData<BlockComponent>(entityManager.GetAllEntities()[0]);
        em.CreateEntity(coloredBlockArchetype, entityArray);

        for (int i = 0; i < q.Length; i++) {
            Entity e = entityArray[i];
            em.SetComponentData(e, new Translation { Value = t[i].Value });
            em.SetComponentData(e, new NonUniformScale { Value = new float3(statBlockSize) });
            em.SetSharedComponentData(e, new RenderMesh {
                mesh = statMesh,
                material = roomMaterials[q[i].index]
            });
        }

        NativeArray<Entity> entities = entityManager.GetAllEntities();
        foreach (Entity i in entities) 
            if (entityManager.HasComponent<BlockComponent>(i) && 
                entityManager.GetComponentData<BlockComponent>(i).covered == 1 &&
                entityManager.GetComponentData<NonUniformScale>(i).Value.y < 0.3) entityManager.DestroyEntity(i);
    }
    #endregion block coloring
}
