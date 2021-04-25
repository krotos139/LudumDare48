using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreatorBehaviour : MonoBehaviour
{

	public Tile emptyTile;
	public Tile groundTile;
	public Tile rockTile;
    public Tile metallTile;
    public Tile rustTile;
    public Tilemap highlightMap;

	public int width;
	public int height;
    /*
    public int anisotropic;

    public int rocks;
    public int rust;
    public int hollows;
    */
    public enum CustomTileType {
        ground, empty, rock, metall, rust
	}
    private int tile_type_size = 5;
    private int logic_size = 5;


    public CustomTileType[,] tiles = null;

    private bool isValidIndices(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }
    /*
    private CustomTileType GetRandomType()
    {
        int typeIndex = Random.Range(0, tile_type_size);
        CustomTileType curType = CustomTileType.ground;
        switch (typeIndex)
        {
            case 0:
                curType = CustomTileType.empty;
                break;
            case 1:
                curType = CustomTileType.ground;
                break;
            case 2:
                curType = CustomTileType.rock;
                break;
            case 3:
                curType = CustomTileType.metall;
                break;
            case 4:
                curType = CustomTileType.rust;
                break;
            default:
                curType = CustomTileType.empty;
                break;
        }
        return curType;
    }

    private CustomTileType GetRandomDiverseType()
    {
        // means not ground

        int typeIndex = Random.Range(1, tile_type_size);
        CustomTileType curType = CustomTileType.ground;
        switch (typeIndex)
        {
            case 1:
                curType = CustomTileType.empty;
                break;
            case 2:
                curType = CustomTileType.rock;
                break;
            case 3:
                curType = CustomTileType.metall;
                break;
            case 4:
                curType = CustomTileType.rust;
                break;
            default:
                curType = CustomTileType.empty;
                break;
        }
        return curType;
    }
    */
    private List<int> GetNeighbours(int ix, int iy)
    {
        int[] neighShifts = new int[] { 1, 0, -1, 0, 0, 1, 0, -1, 1, 1, -1, -1, -1, 1, 1, -1 };

        int randomStartIndex = Random.Range(0, 8) * 2;

        List<int> neighs = new List<int>();

        //for (int i = 0; i < neighShifts.Length; i += 2) // random neighs addition
        for (int i = randomStartIndex; i < neighShifts.Length + randomStartIndex; i += 2)
        {
            int curInd = i % neighShifts.Length;

            int curX = ix + neighShifts[curInd];
            int curY = iy + neighShifts[curInd + 1];

            if (isValidIndices(curX, curY))
            {
                neighs.Add(curX);
                neighs.Add(curY);
            }
        }

        return neighs;
    }
    /*
    private void FillWithType(int desiredCount, int tileCount, int parts, CustomTileType curType, ref CustomTileType [,] used)
    {
        int typeCount = 0;
        int usedByType = 0;
        for (int atIdx = 0; atIdx < parts; atIdx++)
        {
            while (typeCount < desiredCount / parts && usedByType < tileCount / 2)
            {
                int ix = Random.Range(0, width);
                int iy = Random.Range(0, height);

                Vector2 prevailDir = new Vector2(Random.Range(0.1f, 1.0f), Random.Range(0.0f, 1.0f)).normalized;

                Queue<int> connected = new Queue<int>();

                connected.Enqueue(ix);
                connected.Enqueue(iy);

                while (connected.Count != 0 && typeCount < desiredCount && usedByType < tileCount / 2)
                {
                    int curX = connected.Dequeue();
                    int curY = connected.Dequeue();

                    used[curX, curY] = curType;
                    usedByType++;
                    tiles[curX, curY] = curType;
                    typeCount++;

                    List<int> neighs = GetNeighbours(curX, curY);

                    for (int i = 0; i < neighs.Count; i += 2)
                    {
                        int neighX = neighs[i];
                        int neighY = neighs[i + 1];

                        if (used[neighX, neighY] != curType)
                        {
                            used[neighX, neighY] = curType;
                            usedByType++;

                            Vector2 curVec = (new Vector2(neighX - ix, neighY - iy)).normalized;
                            float chance = Vector2.Dot(curVec, prevailDir);

                            float anyChance = Random.Range(0.0f, 1.0f);

                            if (chance > 0.3f && anyChance > 0.6f)
                            {
                                tiles[neighX, neighY] = curType;

                                typeCount++;

                                if (typeCount >= usedByType)
                                {
                                    break;
                                }
                                connected.Enqueue(neighX);
                                connected.Enqueue(neighY);
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void makeLevelDim()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        int tileCount = width * height;

        CustomTileType [,] used = new CustomTileType [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = CustomTileType.ground;
                used[x, y] = CustomTileType.ground;
            }
        }

        // start other zones addition - rock and rust

        int desiredRocksCount = rocks * tileCount / 100;
        int desiredRustCount = rust * tileCount / 100;
        int desiredHollowCount = hollows * tileCount / 100;

        FillWithType(desiredRocksCount, tileCount, 8, CustomTileType.rock, ref used);
        FillWithType(desiredRustCount, tileCount, 8, CustomTileType.rust, ref used);
        FillWithType(desiredHollowCount, tileCount, 8, CustomTileType.empty, ref used);

        float singleMetalProbabilityBlock = 0.03f;

        if (Random.Range(0.0f, 1.0f) > singleMetalProbabilityBlock) // literally always
        {
            // metal part 

            List<int> metalCenters = new List<int>();

            int firstMetalX = Random.Range(0, width);
            int firstMetalY = Random.Range(0, height);

            int metalDesiredCount = Random.Range(0, 2);

            float fwidth = ((float)width);
            float fheight = ((float)height);

            float minSize = (fwidth < fheight ? fwidth : fheight);

            float metalMaxRadius = minSize / 4.0f; // in indices, not tile sizes

            if (metalDesiredCount > 0)
            {
                float metalShift = metalMaxRadius * 2.0f + minSize / 10.0f;

                Vector2 secondDiverseVec = new Vector2(1.0f, 0.0f);

                float initialAngle = Random.Range(0.0f, Mathf.PI * 2.0f);
                float currentAngle = initialAngle;

                Vector2 prevCenter = new Vector2((float)firstMetalX, (float)firstMetalY);

                float angleStep = Mathf.PI / 32.0f;

                while(currentAngle < initialAngle + Mathf.PI * 2.0f && metalCenters.Count < metalDesiredCount * 2)
                {
                    float x = firstMetalX + Mathf.Round(metalShift * Mathf.Cos(currentAngle));
                    float y = firstMetalY + Mathf.Round(metalShift * Mathf.Sin(currentAngle));

                    int ix = (int)x;
                    int iy = (int)y;

                    if (isValidIndices(ix, iy))
                    {
                        bool allowAddition = true;

                        Vector2 curCenter = new Vector2(x, y);

                        for (int k = 0; k < metalCenters.Count; k += 2)
                        {
                            prevCenter.x = metalCenters[k];
                            prevCenter.y = metalCenters[k+1];

                            float dist = (curCenter - prevCenter).magnitude;

                            if (dist < metalShift)
                            {
                                allowAddition = false;
                                break;
                            }
                        }

                        if (allowAddition)
                        {
                            metalCenters.Add(ix);
                            metalCenters.Add(iy);
                        }
                    }
                    currentAngle += angleStep;
                }
            }

            metalCenters.Add(firstMetalX);
            metalCenters.Add(firstMetalY);

            // metal centers are known

            CustomTileType curZoneType = CustomTileType.metall;

            for (int i = 0; i < metalCenters.Count; i += 2)
            {
                int ix = metalCenters[i];
                int iy = metalCenters[i + 1];

                tiles[ix, iy] = curZoneType;

                List<int> connected = new List<int>();
                connected.Add(ix);
                connected.Add(iy);

                used[ix, iy] = CustomTileType.metall;

                float widthChance = Random.Range(0.9f, 1.0f);
                float heightChance = Random.Range(0.7f, 1.0f);

                float maxWidth = Random.Range(Mathf.Round(widthChance * metalMaxRadius), metalMaxRadius);
                float maxHeight = Random.Range(Mathf.Round(heightChance * metalMaxRadius), metalMaxRadius);

                float minX = ix;
                float maxX = ix;

                float minY = iy;
                float maxY = iy;

                bool rustyMetal = Random.Range(0.0f, 1.0f) > 0.5f;

                while (connected.Count != 0)
                {
                    int curX = connected[0];
                    int curY = connected[1];

                    connected.RemoveAt(0);
                    connected.RemoveAt(0);

                    List<int> neighs = GetNeighbours(curX, curY);

                    for(int j = 0; j < neighs.Count; j += 2)
                    {
                        int neighX = neighs[j];
                        int neighY = neighs[j+1];

                        if (used[neighX, neighY] != CustomTileType.metall)
                        {
                            used[neighX, neighY] = CustomTileType.metall; // anyway, don't touch it again

                            Vector2 curNeighVec = new Vector2(ix, iy) - new Vector2(neighX, neighY);

                            float neighDist = curNeighVec.magnitude;

                            if (neighDist >= metalMaxRadius)
                            {
                                if (rustyMetal)
                                {
                                    tiles[neighX, neighY] = CustomTileType.rust;
                                }
                                continue;
                            }

                            minX = (minX < neighX) ? minX : neighX;
                            minY = (minY < neighY) ? minY : neighY;

                            maxX = (maxX > neighX) ? maxX : neighX;
                            maxY = (maxY > neighY) ? maxY : neighY;

                            float curWidth = maxX - minX;
                            float curHeight = maxY - minY;

                            if (curHeight > maxHeight || curWidth > maxWidth)
                            {
                                if (rustyMetal)
                                {
                                    tiles[neighX, neighY] = CustomTileType.rust;
                                }
                                continue;
                            }

                            float curChance = Random.Range(0.0f, 1.0f);

                            if (curChance < 0.3f)
                            {
                                if (rustyMetal)
                                {
                                    // near metal, make it rust
                                    tiles[neighX, neighY] = CustomTileType.rust;
                                }

                                continue;
                            }

                            tiles[neighX, neighY] = curZoneType;

                            connected.Add(neighX);
                            connected.Add(neighY);
                        }
                    }
                }

            }

        }

        // metal part ended

    }
    */

    private CustomTileType getMaterialFromNoise(ref CustomTileType[] materials, ref float[] materialsThresholds, float noiseValue)
    {
        
        CustomTileType curType = CustomTileType.ground;

        for (int i = 0; i < materialsThresholds.Length; i++)
        {
            if (noiseValue < materialsThresholds[i])
            {
                curType = materials[i];
                break;
            }
        }

        return curType;
    }
    
    private void fillLevelByNoise(CustomTileType [] materials, float [] materialsWeights, float perlinCoef, float xorg, float yorg)
    {
        // use xorg and yorg for shifting perlin surface
        // materialsWeights - sum should be equal to 1.0

        float[] materialsThresholds = new float[materialsWeights.Length];
        float summ = 0.0f;
        for (int i = 0; i < materialsWeights.Length; i++)
        {
            summ += materialsWeights[i];
        }
        float prevValue = 0.0f;
        for (int i = 0; i < materialsWeights.Length; i++)
        {
            materialsThresholds[i] = prevValue + materialsWeights[i] / summ;
            prevValue = materialsThresholds[i];
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float fx = ((float)x) / width * perlinCoef;
                float fy = ((float)y) / height * perlinCoef;
                float noiseValue = Mathf.PerlinNoise(xorg + fx, yorg + fy);
                tiles[x, y] = getMaterialFromNoise(ref materials, ref materialsThresholds, noiseValue);
            }
        }
    }

    private void makeRust(int x, int y, int threshold)
    {
        List<int> neighs = GetNeighbours(x, y);
        int metallCells = 0;
        for (int i = 0; i < neighs.Count; i += 2)
        {
            int nx = neighs[i];
            int ny = neighs[i + 1];
            if (tiles[nx, ny] == CustomTileType.metall)
            {
                metallCells++;
                if (metallCells > threshold)
                {
                    tiles[x, y] = CustomTileType.rust;
                    break;
                }
            }
        }
    }

    private void fillBoundaries()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        // add metall

        for (int y = 0; y < height; y++)
        {
            float leftLedge = Random.Range(0.0f, 1.0f);
            int iLeftLedge = leftLedge > 0.5f ? 1 : 0;
            float rightLedge = Random.Range(-1.0f, 0.0f);
            int iRightLedge = rightLedge > -0.5f ? 0 : -1;

            tiles[0, y] = CustomTileType.metall;
            tiles[iLeftLedge, y] = CustomTileType.metall;

            tiles[width-1, y] = CustomTileType.metall;
            tiles[width-1+iRightLedge, y] = CustomTileType.metall;
        }

        // add rust, if there are more than 3 neighbouring metall cells

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while(tiles[x,y] == CustomTileType.metall)
            {
                x++;
            }
            int metallCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(x, y, metallCellsThreshold);

            x = width-1;
            while (tiles[x, y] == CustomTileType.metall)
            {
                x--;
            }
            metallCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(x, y, metallCellsThreshold);
        }
    }

    private void makeLevelPerlin()
    {
        CustomTileType[] materials = new CustomTileType[] { CustomTileType.empty, CustomTileType.ground, CustomTileType.rock, CustomTileType.rust, CustomTileType.metall };
        float[] weights = new float[] { 0.45f, 0.20f, 0.05f, 0.15f, 0.15f };

        // fill main area

        fillLevelByNoise(materials, weights, 12.0f, 12.0f, 0.0f);

        // fill left and right boundaries

        fillBoundaries();
    }

    /*

    private void makeLevel()
    {
        for (int x = 0; x<width; x++) {
            for (int y = 0; y < height; y++) {
                tiles[x, y] = CustomTileType.ground;
            }
        }

        for (int i = 0; i < anisotropic; i++)
        {
            int x1 = Random.Range(1, width - 5);
            int x2 = x1 + Random.Range(5, width/4);
            if (x2 > width)
            {
                x2 = width - 1;
            }
            int y1 = Random.Range(1, height - 5);
            int y2 = y1 + Random.Range(1, height/4);
            if (y2 > height)
            {
                y2 = height - 1;
            }
            switch (Random.Range(1, logic_size+1))
            {
                case 1:
                    makeLevelLogic1(x1, y1, x2, y2);
                    break;
                case 2:
                    makeLevelLogic2(x1, y1, x2, y2);
                    break;
                case 3:
                    makeLevelLogic3(x1, y1, x2, y2);
                    break;
                case 4:
                    makeLevelLogic4(x1, y1, x2, y2);
                    break;
                case 5:
                    makeLevelLogic5(x1, y1, x2, y2);
                    break;
            }
        }
    }

    private void makeLevelLogic1(int x1, int y1, int x2, int y2)
    {
        for (int x = x1; x < x2; x++)
        {
            for (int y = y1; y < y2; y++)
            {
                tiles[x, y] = CustomTileType.rock;

            }
        }
    }

    private void makeLevelLogic2(int x1, int y1, int x2, int y2)
    {
        for (int x = x1; x < x2; x++)
        {
            for (int y = y1; y < y2; y++)
            {
                tiles[x, y] = CustomTileType.empty;

            }
        }
    }
    private void makeLevelLogic3(int x1, int y1, int x2, int y2)
    {
        for (int x = x1; x < x2; x++)
        {
            for (int y = y1; y < y2; y++)
            {
                switch (Random.Range(0, tile_type_size))
                {
                    case 0:
                        tiles[x, y] = CustomTileType.empty;
                        break;
                    case 1:
                        tiles[x, y] = CustomTileType.ground;
                        break;
                    case 2:
                        tiles[x, y] = CustomTileType.rock;
                        break;
                    case 3:
                        tiles[x, y] = CustomTileType.metall;
                        break;
                }
                

            }
        }
    }
    private void makeLevelLogic4(int x1, int y1, int x2, int y2)
    {
        int w = x2 - x1;
        //int h = y2 - y1;
        
        for (int y = y1; y < y2; y++)
        {
            float g = Mathf.Sin(y * 0.1f);
            int x = (int)Mathf.Round(x1 + g * w);
            if (x < 0) x = 0;
            if (x > width) x = width-1;
            tiles[x, y] = CustomTileType.rock;
            tiles[x-1, y] = CustomTileType.rock;
            tiles[x+1, y] = CustomTileType.rock;

        }
    }
    private void makeLevelLogic5(int x1, int y1, int x2, int y2)
    {
        int w = x2 - x1;
        //int h = y2 - y1;

        for (int y = y1; y < y2; y++)
        {
            float g = Mathf.Sin(y * 0.1f);
            int x = (int)Mathf.Round(x1 + g * w);
            if (x < 0) x = 0;
            if (x > width) x = width - 1;
            tiles[x, y] = CustomTileType.metall;

        }
    }

    */

    // do late so that the player has a chance to move in update if necessary
    private void showLevel()
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentCell.x = x - width/2;
                currentCell.y = height/2 - y;
                switch (tiles[x, y])
                {
                    case CustomTileType.empty:
                        highlightMap.SetTile(currentCell, emptyTile);
                        break;
                    case CustomTileType.ground:
                        highlightMap.SetTile(currentCell, groundTile);
                        break;
                    case CustomTileType.rock:
                        highlightMap.SetTile(currentCell, rockTile);
                        break;
                    case CustomTileType.metall:
                        highlightMap.SetTile(currentCell, metallTile);
                        break;
                    case CustomTileType.rust:
                        highlightMap.SetTile(currentCell, rustTile);
                        break;
                }
                
            }
        }
    }

    public Vector2 getBottomLeft()
    {
        Vector3Int origin = highlightMap.origin;
        return new Vector2((float) origin.x, (float) origin.y);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public void Init()
    {
        if (tiles == null)
        {
            tiles = new CustomTileType[width, height];
            //makeLevel();
            //makeLevelDim();
            makeLevelPerlin();
        }
        showLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
