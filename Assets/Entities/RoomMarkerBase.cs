using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RoomMarkerBase : SystemBase {
    int a = 0;
    protected override void OnUpdate() {
        return;
        if (!FloorPlacer.completed) return;
        if (a > 0) return;
        a++;
        //Debug.Log("Started RoomMakerBase");
        NativeArray<float4> rooms = new NativeArray<float4>(FloorPlacer.roomArray, Allocator.TempJob);
        NativeArray<float2> doors = new NativeArray<float2>(FloorPlacer.doors, Allocator.TempJob);
        NativeArray<int> roomTypes= new NativeArray<int>(FloorPlacer.roomTypes, Allocator.TempJob);
        float4 border = FloorPlacer.wallBorder;
        
        Entities
            .WithAll<BlockComponent>()
            .WithReadOnly(rooms)
            .ForEach((Entity block, ref BlockComponent blockComponent, ref NonUniformScale blockScale, ref Translation blockPosition) => {
                float x1 = blockPosition.Value.x - blockScale.Value.x / 2, x2 = blockPosition.Value.x + blockScale.Value.x / 2;
                float y1 = blockPosition.Value.z - blockScale.Value.x / 2, y2 = blockPosition.Value.z + blockScale.Value.x / 2;
                //Debug.Log("Square: " + blockPosition.Value);
                for (int j = 0; j < rooms.Length; j++) {
                    float4 i = rooms[j];
                    if (i.x <= x2 && i.z >= x1 && i.y <= y2 && i.w >= y1) {
                       blockComponent.covered++;
                       blockComponent.index = roomTypes[j];
                    }
            }

            bool isDoorBlock = false;
            foreach (float2 i in doors) if (i.x >= x1 && i.x <= x2 && i.y >= y1 && i.y <= y2) isDoorBlock = true;

            if (isDoorBlock) blockComponent.covered = -1;
            else if (blockComponent.covered > 1) {
                blockScale.Value = new float3(blockScale.Value.x, blockScale.Value.y * 10, blockScale.Value.z);
                //blockPosition.Value.y += blockScale.Value.y * blockComponent.covered;
            } else if (blockComponent.isBorder) {
                blockScale.Value = new float3(blockScale.Value.x, blockScale.Value.y * 10, blockScale.Value.z);

            }
            }).Run();
        rooms.Dispose();
        doors.Dispose();
        roomTypes.Dispose();

        EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<BlockComponent>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<NonUniformScale>());
        NativeArray<BlockComponent> q = query.ToComponentDataArray<BlockComponent>(Allocator.TempJob);
        NativeArray<Translation> t = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        NativeArray<NonUniformScale> s = query.ToComponentDataArray<NonUniformScale>(Allocator.TempJob);

        NativeArray<Entity> entityArray = new NativeArray<Entity>(q.Length, Allocator.Temp);
        

        FloorPlacer.instance.colorRooms(q, t,s);
        q.Dispose();
        t.Dispose();
        s.Dispose();
    }

}
