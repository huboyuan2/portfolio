using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class CharacterPathFind : MonoBehaviour
{
    //four directions for the character to rotate toward
    public Vector3[] rotatedirs = new Vector3[4] { Vector3.right, Vector3.back, Vector3.left, Vector3.forward };
    //the accessibility of the four directions
    public bool[] dirAccess = new bool[4] { false, false, false, false };
    //four blocks which are adjacent to the character
    public GameObject[] moveTargets = new GameObject[4];
    public Vector3 curStairVector;
    private static CharacterPathFind _instance;
    //the event to move the character
    public static Action<int,bool> PlayerMove;
    //the block which the character is navigating to
    public GameObject NavTarget;
    public GameObject standcube;
    //aim to store the alternative Astar nodes which is prepared for recall
    public List<AstarNode> OpenList;
    //aim to store the selected Astar nodes which can form the path from the start block to the goal block
    public List<AstarNode> CloseList;
    private bool isNavigating;

    public static CharacterPathFind Instance
    {
        get
        {
            if (_instance == null)
                _instance = (CharacterPathFind)FindObjectOfType(typeof(CharacterPathFind));
            return _instance;
        }
    }

    public bool IsNavigating {
        get
        {
            return isNavigating;
        }
        set
        {
            isNavigating= value;
            if(CharacterM.Instance.Canrotate)
            MovePanelView.Instance.LockRockBtn(isNavigating);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        OpenList = new List<AstarNode>();
        CloseList = new List<AstarNode>();
    }
    //find the block which the character is standing on
    GameObject GetStandCube()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, Mathf.Infinity, 1 << 0))
        {
            return hit.collider.gameObject;
        }
        else
        {
            Debug.LogError("The character's standing cube cannot be found");
            return this.gameObject;
        }
    }
    //determine the type of a alternative block 
    public bool CheckTag(int index)
    {
        Quaternion quaternion = Quaternion.identity;
        quaternion.SetLookRotation(rotatedirs[(CamFollow.Instance.target + index) % 4]);
        transform.rotation = quaternion;

        switch (moveTargets[index].tag)
        {
            case "Enemy":
                int enmindex = moveTargets[index].GetComponent<Enemy>().Enemyindex;
                BattleMgr.Instance.currentEnm = moveTargets[index];
                EnemyUIView.Instance.CurTarget = moveTargets[index].transform;
                BattleMgr.Instance.GetEnemyInfoFromConfig(enmindex);
                return false;

            case "NPC":
                if (CharacterPathFind.Instance.JudgeAdjacent(moveTargets[index]))
                {
                    int npcindex = moveTargets[index].GetComponent<NPC>().index;
                    BattleMgr.Instance.DealWithNPC(npcindex);
                }
                //StorePage.SetActive(true);
                return false;

            case "Door":
                int doorindex = moveTargets[index].GetComponent<DoorTrigger>().DoorType;
                BattleMgr.Instance.currentDoor = moveTargets[index];
                BattleMgr.Instance.OpenDoorUI(doorindex);
                return false;

            case "triggeritem":
                dirAccess[index] = true;
                return true;

            case "Stair":
                curStairVector = moveTargets[index].transform.forward + moveTargets[index].transform.up;
                return true;

            default:
                return true;
        }
    }
    //determine whether the block is adjacent to the character
    public bool JudgeAdjacent(GameObject go,bool turn = true)
    {
        testraycast.Instance.RayUpdate();
        for (int i = 0; i < 4; i++)
        {
            if (CharacterPathFind.Instance.dirAccess[i])
            {
                if (CharacterPathFind.Instance.moveTargets[i] == go)
                {
                    if (turn)
                    {
                        Quaternion quaternion = Quaternion.identity;
                        quaternion.SetLookRotation(rotatedirs[(CamFollow.Instance.target + i) % 4]);
                        transform.rotation = quaternion;
                    }
                    return true;
                }
            }
        }
        return false;
    }
    //turn the character toward the target block and move the character to the target block
    void MovetoTarget(int index,bool isnav=false)
    {
        //rotate the character toward the next block
        Quaternion quaternion = Quaternion.identity;
        quaternion.SetLookRotation(rotatedirs[(CamFollow.Instance.target + index) % 4]);
        transform.rotation = quaternion;
        //involke all the methods which subscribe the event
         PlayerMove?.Invoke(index,isnav);
    }

    //Calculate the Manhattan distance between two points in the current perspective
    public float IsometricManhattanDistance(Vector3 pos1, Vector3 pos2)
    {
        //get the point in screen space
        Vector2 p1 = Camera.main.WorldToScreenPoint(pos1)-Camera.main.WorldToScreenPoint(pos2);
        //transform from isometric coordinate system to rectangular coordinate system
        p1.y *= 1.732f;
        Vector2 point1 = new Vector2(p1.y - p1.x, p1.y + p1.x);
        return Mathf.Abs(point1.x) + Mathf.Abs(point1.y);
    }

    public void StartNavagation()
    {
        if (IsNavigating)
        {
            OpenList.Clear();
            CloseList.Clear();
        }
        else
        {
            standcube = GetStandCube();
            if (NavTarget == standcube)
                return;
            var firstcube = new AstarNode(standcube.transform, null);

            firstcube.FindAdjacentAccessible();
        }
    }
    //While reaching the goal block, move the character along the path and refresh the close list
    public void WalkAlongNavRoad()
    {
        //¡°CloseList.Count > 0¡±means still not reach the goal block
        if (CloseList.Count > 0)
        {
            if (CloseList[0].directionindex < 0)
            CloseList.RemoveAt(0);
            else
            {
                moveTargets[CloseList[0].directionindex] = CloseList[0].cubepos.gameObject;
                    MovetoTarget(CloseList[0].directionindex, true) ;
                CloseList.RemoveAt(0);
            }
        }
        //already reach the goal block
        else
        {
            CloseList.Clear();
            OpenList.Clear();
            IsNavigating = false;
            SoundMgr.Instance. StopStepSound();
            if (GetStandCube() == NavTarget)
            {
                CharacterAnim.Instance.SetNavRun(false);
                SaveMgr.Instance.SavePosData();
            }
            else StartNavagation();
        }
    }
    //While reaching a dead end, recall to the last fork in the road and select another path from open list
    public void RecallNavRoad()
    {
        if (OpenList.Count > 0)
        {
            while (CloseList[CloseList.Count - 1].Father != OpenList[OpenList.Count - 1].Father)
            {
                CloseList.RemoveAt(CloseList.Count - 1);
            }
            
            {
                CloseList.RemoveAt(CloseList.Count - 1);
                CloseList.Add(OpenList[OpenList.Count - 1]);
                OpenList.RemoveAt(OpenList.Count - 1);
            }
            CloseList[CloseList.Count - 1].FindAdjacentAccessible();
        }
    }
    //Climbing on the sides of stairs blocks is not allowed, and bridge blocks with balustrades are not allowed to pass through the sides
    public bool JudgeWalkable(GameObject cube, Vector3 walkdir)
    {

        if (Mathf.Abs(Vector3.Dot(cube.transform.forward, walkdir)) > 0.99f)
            return false;
        else return true;
    }

    //the class of each node in Astar path finding
    public class AstarNode
    {
        //f=Manhattan Distance to the start block,g=Manhattan Distance to the goal block, h=the sum Manhattan Distance
        public float f, g, h;
        public Transform cubepos;
        //store the father node of the current node
        public AstarNode Father;
        //store the surrounding blocks accessibility
        bool[] aroundaccessible = { false,false,false,false };
        //store the surrounding blocks Astar node which are accessible
        public List<AstarNode> childrenlist;
        //direction index 0=upleft,1=upright,2=downright,3=downleft
        public int directionindex=-1;

        public AstarNode(Transform cubepos, AstarNode Father=null,int directionindex=-1)
        {
            this.Father = Father;
            this.cubepos = cubepos;
            this.CountFGH();
            if (directionindex >= 0)
                this.directionindex = directionindex;
        }
        //f=Manhattan Distance to the start block,g=Manhattan Distance to the goal block, h=the sum Manhattan Distance
        void CountFGH()
        {
            f = CharacterPathFind.Instance.IsometricManhattanDistance(cubepos.position + new Vector3(0, 1.5f, 0), CharacterPathFind.Instance.standcube.transform.position);
            g = CharacterPathFind.Instance.IsometricManhattanDistance(cubepos.position + new Vector3(0, 1.5f, 0), CharacterPathFind.Instance.NavTarget.transform.position);
            h = f + g;
        }
        //return the direction index of opposite direction
        int DiagonalIndex( int id)
        {
            int result = id switch
            {
                1 => 3,
                2 => 0,
                3 => 1,
                0 => 2,
                _ => -1
            };
            return result;
        }
        //determine whether the surrounding blocks is a path while A* pathfinding 
        public void FindAdjacentAccessible()
       {
            childrenlist = new List<AstarNode>();
            //Vector3 virtualcampos = this.cubepos.position + new Vector3(0, 1.5f, 0) - Camera.main.gameObject.transform.forward * 200f;
            for (int i = 0; i < 4; i++)
            {
                bool isstair = cubepos.gameObject.CompareTag("Stair");
                if (isstair && i != directionindex)
                    aroundaccessible[i] = false;
                else if(i== DiagonalIndex(directionindex))
                    aroundaccessible[i] = false;
                else
                {
                    aroundaccessible[i] = testraycast.Instance.TestAcceess(i, this.cubepos.position, out GameObject cube, isstair);
                    if (aroundaccessible[i])
                    {
                        //avoid going through the same father node again
                        if (Father != null)
                        {
                            if (cube.transform != Father.cubepos)
                                childrenlist.Add(new AstarNode(cube.transform, this, i));
                        }
                        else childrenlist.Add(new AstarNode(cube.transform, this, i));

                    }
                }
            }
            if (childrenlist.Count == 0)
            {

                if (CharacterPathFind.Instance.OpenList.Count == 0)
                {
                    //encounter a dead end and no alternative path, stop the path finding
                    CharacterPathFind.Instance.CloseList.Clear();
                    CharacterPathFind.Instance.OpenList.Clear();
                    return;
                }
                else 
                {
                    //encounter a dead end, recalling to the last fork in the road
                    CharacterPathFind.Instance.RecallNavRoad();
                    return;
                }


            }
            foreach (var node in childrenlist)
            {

                //already reach the goal block
                if (node.cubepos == CharacterPathFind.Instance.NavTarget.transform)
                {
                    //Debug.LogError("passageway");
                    CharacterPathFind.Instance.CloseList.Add(node);
                    
                    CharacterPathFind.Instance.IsNavigating = true;
                    if (!SoundMgr.Instance.StepSoundLooping)
                    {
                        SoundMgr.Instance.PlayStepSound();
                    }
                    CharacterAnim.Instance.SetNavRun(true);
                    CharacterPathFind.Instance.WalkAlongNavRoad();
                    return;
                }
            }
            if(childrenlist.Count>1)
            //sort the alternative Astar nodes according to the sum Manhattan Distance
            childrenlist.Sort((x,y)=>x.h.CompareTo(y.h));
            //avoid going through the same node again by checking the CloseList
            if (!CharacterPathFind.Instance.CloseList.Any(node => node.cubepos == childrenlist[0].cubepos))
                CharacterPathFind.Instance.CloseList.Add(childrenlist[0]);
            else 
            {
                //if the node has been gone through, stop the path finding
                //Debug.LogError("dead end");
                CharacterPathFind.Instance.CloseList.Clear();
                CharacterPathFind.Instance.OpenList.Clear();
                return;
            }
            for (int i = 1; i < childrenlist.Count; i++)
            {
                //add other alternative Astar nodes to the open list
                CharacterPathFind.Instance.OpenList.Add(childrenlist[i]);
            }
            //recursively find the next block.childrenlist[0] means the block with the smallest sum Manhattan Distance which is selected to move to
            childrenlist[0].FindAdjacentAccessible();
        }
    }
}
