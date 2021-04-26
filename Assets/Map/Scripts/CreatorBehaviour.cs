using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum EnvCellType
{
    ground, empty, rock, metal, rust
}

public class InteractiveCell
{
    static Dictionary<EnvCellType, int> typeDurability = new Dictionary<EnvCellType, int>() { { EnvCellType.empty, 0 }, { EnvCellType.ground, 2 }, { EnvCellType.rock, 4 }, { EnvCellType.rust, 2 }, { EnvCellType.metal, 999 } };
    int durability = 0;
    int zone = 0;
    EnvCellType cellType = EnvCellType.empty;

    public InteractiveCell(EnvCellType _cellType, int curZone)
    {
        cellType = _cellType;
        durability = typeDurability[cellType];
        zone = curZone;
    }

    public bool Hit()
    {
        durability--;
        if (durability < 1)
        {
            cellType = EnvCellType.empty;
            // broken
            return false;
        }
        return true;
    }
    public int getDurability()
    {
        return durability;
    }
    public EnvCellType getType()
    {
        return cellType;
    }
    public int getZone()
    {
        return zone;
    }
}

public class CreatorBehaviour : MonoBehaviour
{

    public Tile emptyTile;
    public Tile groundTile;
    public Tile rockTile;
    public Tile metalTile;
    public Tile rustTile;
    public Tilemap levelTilemap;
    public PlayerManager player;

    public Texture2D pick;
    public Texture2D pick1;
    public Texture2D pick2;
    public Texture2D pick3;
    public Texture2D pick4;

    public Texture2D shovel;
    public Texture2D shovel1;
    public Texture2D shovel2;
    public Texture2D shovel3;
    public Texture2D shovel4;

    public int width = 50;
    public int height = 50;
    public float interactLength = 1.8f;

    public EnvCellType[,] tiles = null;

    public int levelIndex = 0;
    private int levelXSeed = 0;

    // need to merge it into one thing

    public List<EnvCellType[,]> levels = new List<EnvCellType[,]>();
    public List<InteractiveCell[,]> cells = new List<InteractiveCell[,]>();

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

            if (isValidTileIndices(curX, curY))
            {
                neighs.Add(curX);
                neighs.Add(curY);
            }
        }

        return neighs;
    }

    private EnvCellType getMaterialFromNoise(ref EnvCellType[] materials, ref float[] materialsThresholds, float noiseValue)
    {
        //return EnvCellType.ground;
        EnvCellType curType = EnvCellType.ground;

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

    private void fillLevelByNoise(ref EnvCellType [,] levelTiles, EnvCellType[] materials, float[] materialsWeights, float perlinCoefX, float perlinCoefY, float xorg, float yorg)
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
                float fx = ((float)x) / width * perlinCoefX;
                float fy = ((float)y) / height * perlinCoefY;
                float noiseValue = Mathf.PerlinNoise(xorg + fx, yorg + fy);
                levelTiles[x, y] = getMaterialFromNoise(ref materials, ref materialsThresholds, noiseValue);
            }
        }
    }

    private void makeRust(ref EnvCellType[,] levelTiles, int x, int y, int threshold)
    {
        List<int> neighs = GetNeighbours(x, y);
        int metalCells = 0;
        for (int i = 0; i < neighs.Count; i += 2)
        {
            int nx = neighs[i];
            int ny = neighs[i + 1];
            if (levelTiles[nx, ny] == EnvCellType.metal)
            {
                metalCells++;
                if (metalCells > threshold)
                {
                    levelTiles[x, y] = EnvCellType.rust;
                    break;
                }
            }
        }
    }

    private void fillBoundaries(ref EnvCellType[,] levelTiles)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        // add metal

        for (int x = 0; x < width; x++)
        {
            if (levelTiles[x, 0] == EnvCellType.empty) levelTiles[x, 0] = EnvCellType.ground;
            if (levelTiles[x, height - 1] == EnvCellType.empty) levelTiles[x, height - 1] = EnvCellType.ground;
        }

        for (int y = 0; y < height; y++)
        {
            float leftLedge = Random.Range(0.0f, 1.0f);
            int iLeftLedge = leftLedge > 0.5f ? 1 : 0;
            float rightLedge = Random.Range(-1.0f, 0.0f);
            int iRightLedge = rightLedge > -0.5f ? 0 : -1;

            levelTiles[0, y] = EnvCellType.metal;
            levelTiles[iLeftLedge, y] = EnvCellType.metal;

            levelTiles[width - 1, y] = EnvCellType.metal;
            levelTiles[width - 1 + iRightLedge, y] = EnvCellType.metal;
        }

        // add rust, if there are more than 3 neighbouring metal cells

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (levelTiles[x, y] == EnvCellType.metal)
            {
                x++;
            }
            int metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(ref levelTiles, x, y, metalCellsThreshold);

            x = width - 1;
            while (levelTiles[x, y] == EnvCellType.metal)
            {
                x--;
            }
            metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(ref levelTiles, x, y, metalCellsThreshold);
        }
    }

    private void makeLevelPerlin(ref EnvCellType[,] levelTiles)
    {
        EnvCellType[] materials = new EnvCellType[] { EnvCellType.empty, EnvCellType.ground, EnvCellType.rock, EnvCellType.rust, EnvCellType.metal };
        float[] weights = new float[] { 0.15f, 0.40f, 0.15f, 0.15f, 0.15f };

        // fill main area

        fillLevelByNoise(ref levelTiles, materials, weights, 12.0f, 12.0f * (height / width), 12.0f * levelXSeed, 12.0f * levels.Count * (height / width));

        // fill left and right boundaries

        fillBoundaries(ref levelTiles);
    }

    public void addNewLevel()
    {
        EnvCellType[,] levelTiles = new EnvCellType[width, height];
        InteractiveCell[,] levelCells = new InteractiveCell[width, height];

        makeLevelPerlin(ref levelTiles);
        SetInteractiveCells(ref levelTiles, ref levelCells, levels.Count);

        levels.Add(levelTiles);
        cells.Add(levelCells);
        showLevel(levels.Count - 1);
    }

    public float curZoneDeep()
    {
        return height * levelIndex + height;
    }

    public float curZoneStart()
    {
        return height * levelIndex;
    }

    public float currentLevelMiddle()
    {
        return height * levelIndex + ((float)height) / 2;
    }

    public bool needLevelAddition(float curDeep)
    {
        if (curDeep > currentLevelMiddle())
        {
            if (levelIndex == levels.Count - 1)
            {
                return true;
            }
        }
        return false;
    }

    public void setCurrentLevel(int index)
    {
        levelIndex = index;
        tiles = levels[index];
        showLevel(index);
    }

    public int nextLevelIndex()
    {
        return levelIndex + 1;
    }

    // do late so that the player has a chance to move in update if necessary
    private void showLevel(int currentLevelIndex)
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentCell.x = x - width / 2;
                currentCell.y = height / 2 - y - currentLevelIndex * height;
                switch (levels[currentLevelIndex][x, y])
                {
                    case EnvCellType.empty:
                        //levelTilemap.SetTile(currentCell, emptyTile);
                        levelTilemap.SetTile(currentCell, null); // just don't draw it
                        break;
                    case EnvCellType.ground:
                        levelTilemap.SetTile(currentCell, groundTile);
                        break;
                    case EnvCellType.rock:
                        levelTilemap.SetTile(currentCell, rockTile);
                        break;
                    case EnvCellType.metal:
                        levelTilemap.SetTile(currentCell, metalTile);
                        break;
                    case EnvCellType.rust:
                        levelTilemap.SetTile(currentCell, rustTile);
                        break;
                }
            }
        }
    }

    public Vector2 getBottomLeft()
    {
        Vector3Int origin = levelTilemap.origin;
        return new Vector2((float)origin.x, (float)origin.y);
    }

    public Vector2 getZoneBottomLeft()
    {
        Vector3Int origin = levelTilemap.origin;
        origin.y += height * (levels.Count - 1);
        return new Vector2((float)origin.x, (float)origin.y);
    }

    public bool isValidTileIndices(int tileX, int tileY)
    {
        if (tileX >= 0 && tileX < width && tileY >= 0)
        {
            if (tileY >= height)
            {
                if (levels.Count > levelIndex + 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    public EnvCellType getTileType(int tileX, int tileY)
    {
        int curTileZone = tileY / height + levelIndex;
        tileY %= height;

        return levels[curTileZone][tileX, tileY];
    }

    public int setTileType(int tileX, int tileY, EnvCellType someType)
    {
        int curTileZone = tileY / height + levelIndex;
        tileY %= height;

        levels[curTileZone][tileX, tileY] = someType;
        return curTileZone;
    }

    public ref InteractiveCell getCell(int tileX, int tileY)
    {
        int curTileZone = tileY / height + levelIndex;
        tileY %= height;

        return ref cells[curTileZone][tileX, tileY];
    }

    void prepareBeginning()
    {
        // clear layer for beginning
        for (int x = 2; x < width-2; ++x)
        {
            for (int y = 0; y < 5; ++y)
            {
                setTileType(x, y, EnvCellType.empty);
            }
        }
        
        for (int y = 0; y < 5; y++)
        {
            if (getTileType(0, y) != EnvCellType.metal)
            {
                setTileType(0, y, EnvCellType.empty);
            }
            if (getTileType(1, y) != EnvCellType.metal)
            {
                setTileType(1, y, EnvCellType.empty);
            }
            if (getTileType(width-1, y) != EnvCellType.metal)
            {
                setTileType(width-1, y, EnvCellType.empty);
            }
            if (getTileType(width-2, y) != EnvCellType.metal)
            {
                setTileType(width-2, y, EnvCellType.empty);
            }
        }
        
        int xmid = width / 2;
        int holeWidth = width / 4;

        int holeStart = 5;
        int holeStop = 9;

        for (int x = xmid - holeWidth / 2; x < xmid + holeWidth / 2 + holeWidth % 2; x++)
        {
            for(int y = holeStart; y < holeStop; y++)
            {
                setTileType(x, y, EnvCellType.empty);
            }
        }
    }

    private void SetInteractiveCells(ref EnvCellType[,] levelTiles, ref InteractiveCell[,] levelCells, int zoneIndex)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                levelCells[x, y] = new InteractiveCell(levelTiles[x, y], zoneIndex);
            }
        }
    }

    public void Init()
    {
        if (tiles == null)
        {
            Random.InitState((int)System.DateTime.Now.Ticks);

            levelXSeed = Random.Range(0, 10000);

            tiles = new EnvCellType[width, height];

            addNewLevel();
            setCurrentLevel(0);

            prepareBeginning();

            InteractiveCell[,] levelCells = cells[0];

            SetInteractiveCells(ref tiles, ref levelCells, 0);

            cells[0] = levelCells;
        }
        showLevel(0);
    }

    // Update is called once per frame
    void Update()
    {
        bool canInteract = false;
        Vector2 playerPosition = player.getPosition();
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 zoneBottom = getZoneBottomLeft();
        Vector3 relPosition = cursorPosition - new Vector3(zoneBottom.x, zoneBottom.y, 0.0f);
        // relPosition.y += height * (levelIndex);
        relPosition.y = height - relPosition.y;
        Vector2Int tileInds = new Vector2Int((int)Mathf.Round(relPosition.x - 0.5f), (int)Mathf.Round(relPosition.y - 0.5f));

        if (isValidTileIndices(tileInds.x, tileInds.y))
        {
            InteractiveCell curCell = getCell(tileInds.x, tileInds.y);
            EnvCellType cellType = curCell.getType();
            int cellDurability = curCell.getDurability();

            if (cellType != EnvCellType.metal && cellType != EnvCellType.empty)
            {
                if (Mathf.Abs(playerPosition.x - relPosition.x) <= interactLength && Mathf.Abs(playerPosition.y - relPosition.y) <= interactLength)
                {
                    // only detroying 

                    canInteract = true;

                    if (cellType == EnvCellType.ground || cellType == EnvCellType.rust)
                    {
                        switch(cellDurability)
                        {
                            case 1:
                                Cursor.SetCursor(shovel4, Vector2.zero, CursorMode.Auto);
                                break;
                            case 2:
                                Cursor.SetCursor(shovel, Vector2.zero, CursorMode.Auto);
                                break;
                        }
                    }
                    if (cellType == EnvCellType.rock)
                    {
                        switch (cellDurability)
                        {
                            case 1:
                                Cursor.SetCursor(pick4, Vector2.zero, CursorMode.Auto);
                                break;
                            case 2:
                                Cursor.SetCursor(pick3, Vector2.zero, CursorMode.Auto);
                                break;
                            case 3:
                                Cursor.SetCursor(pick1, Vector2.zero, CursorMode.Auto);
                                break;
                            case 4:
                                Cursor.SetCursor(pick, Vector2.zero, CursorMode.Auto);
                                break;
                        }
                    }
                    
                }
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            if (Input.GetMouseButtonDown(0) && canInteract)
            {
                Debug.LogWarning($"removing tile : ({tileInds.x}, {tileInds.y})");

                if (!curCell.Hit())
                {
                    int tileZone = setTileType(tileInds.x, tileInds.y, EnvCellType.empty);
                    showLevel(curCell.getZone());
                }
                player.Dig();
            }

            /*

            if (Input.GetMouseButtonDown(1) && canInteract)
            {

                Debug.LogWarning($"adding tile : ({tileInds.x}, {tileInds.y})");

                int tileZone = setTileType(tileInds.x, tileInds.y, EnvCellType.ground);
                showLevel(tileZone);
                player.Dig();
            }

            */
        }
    }
}
