using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData : MonoBehaviour
{
    //Fill in the distribution of squares on the six faces under the positive direction of the left hand coordinate axis
    //The opposite sides need to count the box distribution according to the direction of the same coordinate, otherwise an error will occur
    //使用Custom Tools/Binary to Decimal Converter进行换算较为方便
    public enum dirs 
    {
    right,
    up,
    front,
    back,
    down,
    left
    }
    public List<Vector3Int> monstersDistribution;
    //public Dictionary<int, int> propsDistribution;
    //x,y,z,-z,-y,-x
    public int[] sixDirBinary;
    public int distance=-1;
    //The calculation of the connection situation by Bit operation, it is applicable to the front and back or left and right, but not up and down
    public static bool JudgeConnectivity(int a,int b,bool allowparallel=true)
    {
        if (allowparallel)
        {
            if ((a == 0) && (b == 0))
                return true;
        }
        if ((a & b) > 0)
            return true;
        return false;
    }
    //some blocks only allowed to generate one time in the scene
    public bool forcesingle=false;
    
}
