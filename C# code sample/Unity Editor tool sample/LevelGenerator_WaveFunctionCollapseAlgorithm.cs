using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;


public class LevelGenerator_WaveFunctionCollapseAlgorithm : EditorWindow
{
    private int tempcount = 0;
    private int blockPrefabCount = 0;
    //the width and height of the level
    int width = 5;
    int height = 5;
    //level generation start row(i) and column(j)
    int starti = 3;
    int startj = 3;
    //player start row(i) and column(j)
    int playerstarti =3;
    int playerstartj = 3;
    //the scriptable object that stores the monster data
    public ChaMScriptObject enemyDataObj;
    //the storage of the blocks GameObjects that have been instantiated in the
    private GameObject[,] blockInstances = new GameObject[5, 5];
    //storage of the blocks that can only be instantiated once and already instantiated
    private List<int> InstanceSingleList;
    //storage of the blocks that can only be instantiated once
    private List<int> TemplateSingleList;
    //the storage of index of blocks in the 2D array which is used to instantiate the blocks in the level
    //empty blocks are represented by -1
    private int[,] blocks = new int[5, 5]
{
    {-1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1}
};
    //Store options for each cell in the 2D array grid 
    Dictionary<Vector2Int, int[]> blockOptions = new Dictionary<Vector2Int, int[]>();
    //selectable block prefab templates.
    private List<GameObject> prefabsToInstantiate = new List<GameObject>();
    //selectable monster prefab templates.
    private List<GameObject> monstersToInstantiate = new List<GameObject>();
    [MenuItem("Custom Tools/Level Generator_Wave Function")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(LevelGenerator_WaveFunctionCollapseAlgorithm));
    }


    private void OnGUI()
    {
        GUILayout.Label("Level Generator_Wave Function", EditorStyles.boldLabel);
        int curwidth=width;
        int curheight = height;
        width = EditorGUILayout.IntField("levelwidth", width);
        height = EditorGUILayout.IntField("levelheight", height);
        if ((curwidth != width) || (curheight != height))
        {
            curwidth = width;
            curheight = height;
            blocks = new int[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    blocks[i, j] = -1;
                }
            }
            blockInstances = new GameObject[height, width];
        }
        blockPrefabCount = EditorGUILayout.IntField("blockPrefabCount", blockPrefabCount);
        
        for (int i = 0; i < blockPrefabCount; i++)
        {
            if (i >= prefabsToInstantiate.Count)
                prefabsToInstantiate.Add(null);
            prefabsToInstantiate[i] = EditorGUILayout.ObjectField("Prefab to Instantiate", prefabsToInstantiate[i], typeof(GameObject), false) as GameObject;
        }
        if(prefabsToInstantiate.Count > blockPrefabCount)
        prefabsToInstantiate.RemoveRange(blockPrefabCount, prefabsToInstantiate.Count-blockPrefabCount);
        if (GUILayout.Button("Add Selected BlockPrefabs"))
        {
            blockPrefabCount += Selection.gameObjects.Length;
            foreach (var obj in Selection.gameObjects)
            {
                prefabsToInstantiate.Add(obj);
            }
        }

        for (int i = 0; i < height; i++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int j = 0; j < width; j++)
            {
                var options = GetOptionalBlocks(i, j);
                if (options.Length == 0)
                {
                    blocks[i, j] = -1;
                    blocks[i, j] = EditorGUILayout.IntField(blocks[i, j]);
                }
                else if (options.Length == 1)
                {
                    blocks[i, j] = int.Parse(options[0]);
                    blocks[i, j] = EditorGUILayout.IntField(blocks[i, j]);
                }
                else
                    blocks[i, j] = EditorGUILayout.Popup(blocks[i, j], options);
            }

            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("GenerateBlocks"))
        {
            GenerateBlocks();
        }

        starti=EditorGUILayout.IntField("Level Generate start x", starti);
        startj=EditorGUILayout.IntField("Level Generate start y", startj);
        if (GUILayout.Button("GenerateBlocks_WaveFunction"))
        {
            //add the blocks that can only be instantiated once to the list
            for(int i= 0;i<prefabsToInstantiate.Count;i++)
            {
                if (prefabsToInstantiate[i].GetComponent<BlockData>().forcesingle)
                {
                    TemplateSingleList.Add(i);
                }
            }
            WaveFunctionCollapse(starti,startj);
            GenerateBlocks();
        }
        playerstarti = EditorGUILayout.IntField("player start x", playerstarti);
        playerstartj = EditorGUILayout.IntField("player start y", playerstartj);
        if (GUILayout.Button("Count All Blocks distances"))
        {
            ClearAllDistances();
            CountDistance(playerstarti, playerstartj,new List<Vector2Int>());
            HideAllUnApproachableBlocks();
        }
        //distribute the monsters based on the distance from the palyer
        if (GUILayout.Button("Spawn All Blocks monsters"))
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (blockInstances[i, j] != null)
                    {
                        var curBlockData = blockInstances[i, j].GetComponent<BlockData>();
                        SpawnMonsters(curBlockData);
                    }
                }
            }
        }
        if (GUILayout.Button("statistic Scene Data"))
        {
            SceneObjectIterator.ModifyParameter();
        }
        if (GUILayout.Button("ClearAllBlocksDistances"))
        {
            ClearAllDistances();
        }
        if (GUILayout.Button("ClearAllToggles"))
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    blocks[i, j] = -1;
                }
            }
        }
        if (GUILayout.Button("ClearAllInstances&Toggles"))
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    blocks[i, j] = -1;
                    //blockInstances[i, j].SetActive(false);
                    DestroyImmediate(blockInstances[i, j]);
                    blockInstances[i, j] = null;
                }
            }
        }
        
    }
    void HideAllUnApproachableBlocks()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if(blockInstances[i, j]!=null)
                if (blockInstances[i, j].GetComponent<BlockData>().distance < 0)
                {
                    blockInstances[i, j].SetActive(false);
                }
            }
        }
    }
    void ClearAllDistances()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if(blockInstances[i, j]!=null)
                blockInstances[i, j].GetComponent<BlockData>().distance = -1;
            }
        }
    }
    //Persist the data of the prefab dragged into the editor
    private void OnDisable()
    {
        StringBuilder sb=new StringBuilder();
        if (prefabsToInstantiate.Count > 0)
        {
            for (int i= 0;i<prefabsToInstantiate.Count;i++)
            {
                var id = prefabsToInstantiate[i].GetInstanceID();
                sb.Append(id);
                if(i < prefabsToInstantiate.Count-1)
                    sb.Append("=");
            }
            PlayerPrefs.SetString("blockids", sb.ToString());
        }
    }
    //Read the previously saved prefab dragged in
    private void OnEnable()
    {
        InstanceSingleList = new List<int>();
        TemplateSingleList = new List<int>();
        enemyDataObj = AssetDatabase.LoadAssetAtPath<ChaMScriptObject>("Assets/EnemyData_Local.asset");
        if (enemyDataObj == null)
            Debug.LogError("enemyDataObj == null");
        else
        {
            monstersToInstantiate = new List<GameObject>();
            for (int i = 0; i < enemyDataObj.tests.Length; i++)
            {
                monstersToInstantiate.Add(enemyDataObj.tests[i].go);
            }
            //Debug.LogError("enemyDataObj != null"+enemyDataObj.tests.Length.ToString());
        }
        if (PlayerPrefs.HasKey("blockids"))
        {
            string blockIDstr= PlayerPrefs.GetString("blockids");
            var blockIDs = blockIDstr.Split("=");
            blockPrefabCount = blockIDs.Length;
            prefabsToInstantiate.Clear();
            for (int i = 0; i < blockPrefabCount; i++)
            {
                prefabsToInstantiate.Add(EditorUtility.InstanceIDToObject(int.Parse(blockIDs[i])) as GameObject);
            }
        }
    }
    //a depth-first search algorithm, the Heuristic term is least of the number of optional blocks in the adjacent cells
    //Each time a block is identified, the number of options for adjacent blocks is reduced and propagated according to these changes
    private void WaveFunctionCollapse(int i = 0,int j = 0)
    {
        var options = GetOptionalBlocks(i,j);
        if (blocks[i, j] == -1)
        {
            if (options.Length == 0)
                return;
            blocks[i, j] = int.Parse(options[Random.Range(0, options.Length)]);
            if (prefabsToInstantiate[blocks[i, j]].GetComponent<BlockData>().forcesingle)
            {
                InstanceSingleList.Add(blocks[i, j]);
            }
        }
        else return;
        List<Vector2Int> nearblocks = new List<Vector2Int>();
        if(i<height-1)
            nearblocks.Add(new Vector2Int(i + 1, j));
        if (i > 0)
            nearblocks.Add(new Vector2Int(i - 1, j));
        if (j <width-1)
            nearblocks.Add(new Vector2Int(i , j+1));
        if (j > 0)
            nearblocks.Add(new Vector2Int(i , j-1));
        ComplexityComparer cpr = new ComplexityComparer(this);
        //Sort by how many options are available (PathFinding heurist trem)
        nearblocks.Sort(cpr);
        //depth-first search
        for (int n=0;n<nearblocks.Count;n++)
        WaveFunctionCollapse(nearblocks[n].x, nearblocks[n].y);       
    }
    //The Equal Function of Vector2 has been overwritten by Unity, so equal values means totally equal, so it can be determined directly using Contains function
    private void CountDistance(int i = 0, int j = 0, List<Vector2Int> path=null)
    {
        Vector2Int curxy = new Vector2Int(i, j);
        var curpath = new List<Vector2Int>();
        curpath.AddRange(path);
        if (path.Contains(curxy))
        {
            return;
        }
        else 
        {
            curpath.Add(curxy);
        }
        int dist = path.Count;
        //Debug.LogError("CountDistance"+dist.ToString());
        int[] binaries;
        if (blockInstances[i, j] != null)
        {
            BlockData curBlockData = blockInstances[i, j].GetComponent<BlockData>();

            binaries = curBlockData.sixDirBinary;
            //Debug.LogError("CountDistance#" + curBlockData.distance.ToString());
            if (curBlockData.distance < 0)
            {
                //Debug.LogError(i.ToString() + "setdistance" + j.ToString());
                curBlockData.distance = dist;
            }
            else if (curBlockData.distance > dist)
            {
                curBlockData.distance = dist;
            }
            else return;

        }
        else
        {
            return;
            //binaries = new int[6]{ 0,0,0,0,0,0};
        }
        if (i < height - 1)
        {
            if(judge2Blocks(binaries,i,j, i + 1, j,false))
            CountDistance(i + 1, j, curpath); 
        }
        if (i > 0)
        {
            if (judge2Blocks(binaries, i, j, i - 1, j, false))
                CountDistance(i - 1, j, curpath); 
        }
        if (j < width - 1)
        {
            if (judge2Blocks(binaries, i, j, i, j+1, false))
                CountDistance(i, j + 1, curpath); 
        }
        if (j > 0)
        {
            if (judge2Blocks(binaries, i, j, i , j-1, false))
                CountDistance(i, j - 1, curpath); 
        }
    }
    public class ComplexityComparer : IComparer<Vector2Int>
    {
        private LevelGenerator_WaveFunctionCollapseAlgorithm owner;
        public ComplexityComparer(LevelGenerator_WaveFunctionCollapseAlgorithm owner)
        {
            this.owner = owner;
        }
        //Compare the number of option blocks in two cells
        public int Compare(Vector2Int v1, Vector2Int v2)
        {
            int complex1 = owner.GetOptionalBlocks(v1.x, v1.y).Length;
            int complex2 = owner.GetOptionalBlocks(v2.x, v2.y).Length;
            if (complex1 < complex2) return -1;
            if (complex1 > complex2) return 1;
            return 0;
        }
    }
    //Generate all blocks in the level according to the data in the blocks array
    private void GenerateBlocks()
    {
        //Debug.LogError(" GenerateBlocks");
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (blocks[i, j]>=0&& blocks[i, j]< prefabsToInstantiate.Count)
                {
                    GameObject block = GetAppropriateBlock(blocks[i, j]);
                    if(null==block)
                     block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.position = new Vector3(j * 15f,0, i*-15f);
                    blockInstances[i, j] = block;
                    Undo.RegisterCreatedObjectUndo(block, "Create Block");
                }
            }               
        }
        InstanceSingleList.Clear();
        TemplateSingleList.Clear();
    }
    //Blocks that occur only once are allowed to have more weight when selected at random
    private string[] GetOptionalBlocks(int i = 0, int j = 0,int singleWeightIndex=2)
    {
        List<string> indexlist = new List<string>();
        for (int n=0;n<prefabsToInstantiate.Count;n++)
        {
            if (InstanceSingleList.Contains(n))
                continue;
            if (JudgeConnectivity(i, j, n))
            {
                if (TemplateSingleList.Contains(n))
                {
                    for (int w = 0; w < singleWeightIndex; w++)
                    {
                        indexlist.Add(n.ToString());
                    }
                }
                indexlist.Add(n.ToString());
            }
        }
        
        return indexlist.ToArray();
    }

    //Row i, column j, the Nth prefab template
    private bool JudgeConnectivity(int i = 0, int j = 0,int n=0)
    {
        if (prefabsToInstantiate[n] == null)
            return true;
        BlockData curBlockData = prefabsToInstantiate[n].GetComponent<BlockData>();
        var binaries = curBlockData.sixDirBinary;
        return (judge2Blocks(binaries,i,j,i+1,j)&& judge2Blocks(binaries, i, j, i - 1, j)&& judge2Blocks(binaries, i, j, i , j+1)&& judge2Blocks(binaries, i, j, i , j-1));

    }
    //judge if two blocks can be located in the nearby position by comparing the binary data the adjacent edge of the two blocks
    private bool judge2Blocks(int[] binaries,int i = 0, int j = 0, int i1 = 0, int j1 = 0,bool allowparallel=true)
    {

        {            
            int[] block1binaries;
            if (i1 >= height || i1 < 0 || j1 >= width || j1 < 0)
            {
                //The location is out of bounds, temporarily avoid the judgment
                block1binaries = new int[6] { 0, 0, 0, 0, 0, 0 };
            }
            else 
            {                
                if (blocks[i1, j1] < 0 || blocks[i1, j1] >= prefabsToInstantiate.Count)
                    return true;
                var block1 = prefabsToInstantiate[blocks[i1, j1]];
                if (block1 == null)
                    return true;
                block1binaries = block1.GetComponent<BlockData>().sixDirBinary;
            }
            //right£¬x
            if (j1 - j == 1)
            {
                return BlockData.JudgeConnectivity(binaries[0],block1binaries[5], allowparallel);
            }
            //left£¬-x
            else if (j1 - j == -1)
            {
                return BlockData.JudgeConnectivity(binaries[5], block1binaries[0], allowparallel);
            }
            //back£¬-z
            else if (i1 - i == 1)
            {
                return BlockData.JudgeConnectivity(binaries[3], block1binaries[2], allowparallel);
            }
            //front,z
            else if (i1 - i == -1)

            {
                return BlockData.JudgeConnectivity(binaries[2], block1binaries[3], allowparallel);
            }
            else 
            {
                Debug.LogError("Comparison error, not up, down, left or right");
                return true;
            }
        }
    }
    //Instantiate and get the appropriate Block prefab accroding to index
    private GameObject GetAppropriateBlock(int index=0,int i=0,int j=0)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefabsToInstantiate[index]) as GameObject;
        return go;
    }
    //spawn monsters in the block accroding to the distance from player initiate place and monstersDistribution data record
    void SpawnMonsters(BlockData bd)
    {
        Transform curtrans = bd.transform;
        var monsterspoints = bd.monstersDistribution;
        int distance = bd.distance;
        if (distance < 0)
        {
            return;
        }
        foreach (var point in monsterspoints)
        {
            var monsterinstance= GetAppropriateMonster(distance,1,curtrans);
            if (monsterinstance == null)
                continue;
            monsterinstance.transform.position += point*3;
            monsterinstance.transform.position += new Vector3(0f,1.5f,0f);
        }
    }
    // get suitable monsteres accroding to the distance from player initiate place
    private GameObject GetAppropriateMonster(int distance,int offset,Transform parent)
    {
        int index = Random.Range(Mathf.Max(distance-offset,0),Mathf.Min(distance+offset,monstersToInstantiate.Count));

        //int index = distance;
        //Debug.LogError(index.ToString() + " " + monstersToInstantiate.Count.ToString()) ;
        if (index > 0 && index < monstersToInstantiate.Count)
            return PrefabUtility.InstantiatePrefab(monstersToInstantiate[index], parent) as GameObject;
        else return null;
    }
}