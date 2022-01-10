using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
public struct BlockComponent : IComponentData {
    public int covered;
    public int index;
    public bool isBorder;
}
