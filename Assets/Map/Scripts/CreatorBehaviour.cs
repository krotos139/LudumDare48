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
    public static Dictionary<EnvCellType, int> typeDurability = new Dictionary<EnvCellType, int>() { { EnvCellType.empty, 0 }, { EnvCellType.ground, 2 }, { EnvCellType.rock, 4 }, { EnvCellType.rust, 2 }, { EnvCellType.metal, 999 } };
    int durability = 0;
    int zone = 0;
    EnvCellType cellType = EnvCellType.empty;
    public int neighbours = 0;
    public int garbage = -1;

    public InteractiveCell(EnvCellType _cellType, int curZone)
    {
        cellType = _cellType;
        durability = typeDurability[cellType];
        zone = curZone;
        if (_cellType == EnvCellType.ground && Random.Range(0,100) < 10)
        {
            garbage = Random.Range(0, 9);
        }
        
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
    public Tilemap levelTilemap;
    public Tilemap decalTilemap;
    public Tilemap gbTilemap;
    public PlayerManager player;

    public int width = 50;
    public int height = 50;
    public float interactLength = 1.8f;

    public EnvCellType[,] tiles = null;

    public int levelIndex = 0;
    private int levelXSeed = 0;

    public int tilePixelSize = 48;

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

    private int GetTileOrthoNeighbours(int ix, int iy, EnvCellType type)
    {
        int[] neighShifts = new int[] { 0, -1, 1, 0, 0, 1, -1, 0 };

        int neighs = 0;

        for (int i = 0; i < neighShifts.Length; i += 2)
        {
            int curX = ix + neighShifts[i];
            int curY = iy + neighShifts[i + 1];

            if (isValidTileIndices(curX, curY))
            {
                if (type == getTileType(curX, curY))
                {
                    neighs |=  1 << (i / 2);
                }
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

    private void recalcNeighbours(ref InteractiveCell[,] levelCells)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InteractiveCell cell = levelCells[x, y];
                cell.neighbours = GetTileOrthoNeighbours(x, y, cell.getType());
                levelCells[x, y] = cell;
            }
        }
    }

    private void fillLevelByNoise(ref EnvCellType[,] levelTiles, EnvCellType[] materials, float[] materialsWeights, float perlinCoefX, float perlinCoefY, float xorg, float yorg)
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

    private void clearLevelTiles(ref EnvCellType[,] levelTiles)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (levelTiles[x, y] != EnvCellType.metal)
                {
                    levelTiles[x, y] = EnvCellType.empty;
                }
            }
        }
    }

    private void fillBoundaries(ref EnvCellType[,] levelTiles)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        // bottom part

        for (int x = 0; x < width; x++)
        {
            if (levelTiles[x, height - 1] != EnvCellType.metal) levelTiles[x, height - 1] = EnvCellType.ground;
        }

        int endingStart = 6;

        int conditionXmin = 0;
        int conditionXmax = width - 1;

        for (int y = height - endingStart; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x < conditionXmin || x >= conditionXmax)
                {
                    levelTiles[x, y] = EnvCellType.metal;
                }
            }
            conditionXmin += 3;
            conditionXmax -= 3;
        }


        // add metal boundaries

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

        int metalCellsThreshold = 0;

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width && levelTiles[x, y] == EnvCellType.metal)
            {
                x++;
            }
            if (x < width)
            {
                metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
                makeRust(ref levelTiles, x, y, metalCellsThreshold);
            }

            x = width - 1;
            while (x >= 0 && levelTiles[x, y] == EnvCellType.metal)
            {
                x--;
            }
            if (x >= 0)
            {
                metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
                makeRust(ref levelTiles, x, y, metalCellsThreshold);
            }
        }
#if WATER_TEST
        clearLevelTiles(ref levelTiles);
#endif
    }

    private void makeLevelPerlin(ref EnvCellType[,] levelTiles)
    {
        EnvCellType[] materials = new EnvCellType[] { EnvCellType.empty, EnvCellType.ground, EnvCellType.rock, EnvCellType.rust, EnvCellType.metal };
        float[] weights = new float[] { 0.15f, 0.35f, 0.15f, 0.10f, 0.25f };

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

    Dictionary<EnvCellType, Tile[]> tilesheet = new Dictionary<EnvCellType, Tile[]>();
    Dictionary<EnvCellType, Tile[]> decals = new Dictionary<EnvCellType, Tile[]>();
    Tile[] garbages = new Tile[9];

    string getNeighTileName(EnvCellType curType, bool [] neighs)
    {
        string answer = "";
        switch(curType)
        {
            case EnvCellType.ground:
                answer += "ground";
                break;
            case EnvCellType.rock:
                answer += "rock";
                break;
            case EnvCellType.rust:
                answer += "rust";
                break;
            case EnvCellType.metal:
                answer += "metal";
                break;
        }
        string [] suffixes = new string[4] { "U", "R", "D", "L" };
        answer += "_tilesheet_";
        for (int i = 0; i < 4; i++)
        {
            if (neighs[i])
            {
                answer += suffixes[i];
            }
        }
        return answer;
    }

    private void loadTileAsset(string type, EnvCellType curType)
    {

        string folderName = type+"_tilesheet_";
        tilesheet.Add(curType, new Tile[16]);

        tilesheet[curType][0] = Resources.Load<Tile>(folderName + "0");

        for (int i = 1; i < 16; i++)
        {
            string tilename = folderName;

            bool u = (i & 1) == 1;
            bool r = (i & 2) == 2;
            bool d = (i & 4) == 4;
            bool l = (i & 8) == 8;

            if (u) tilename += "U";
            if (r) tilename += "R";
            if (d) tilename += "D";
            if (l) tilename += "L";

            tilesheet[curType][i] = Resources.Load<Tile>(tilename);
        }
    }

    private void loadDecalAsset(string type, EnvCellType curType)
    {

        string folderName = "dc_"+type;
        decals.Add(curType, new Tile[3]);

        for (int i = 1; i < 4; i++)
        {
            decals[curType][i-1] = Resources.Load<Tile>(folderName+"_"+i);
        }
    }

    private void loadGarbagesAsset()
    {
        string folderName = "gb_tilesheet";

        for (int i = 0; i < 9; i++)
        {
            garbages[i] = Resources.Load<Tile>(folderName + "_" + i);
        }
    }

    private void showDecalEmpty(int x, int y)
    {
        int rx = x - width / 2;
        int ry = height / 2 - y;
        decalTilemap.SetTile(new Vector3Int(rx, ry, 0), null);
    }

    private void showDecal(int x, int y, ref InteractiveCell cell)
    {
        int rx = x - width / 2;
        int ry = height / 2 - y ;
        if (cell.getType() == EnvCellType.metal || cell.getType() == EnvCellType.empty)
        {
            decalTilemap.SetTile(new Vector3Int(rx, ry, 0), null);
            return;
        }
        int maxDurability = InteractiveCell.typeDurability[cell.getType()];
        int decalIndex = maxDurability - cell.getDurability() - 1;
        Tile tile = decals[cell.getType()][decalIndex];
        decalTilemap.SetTile(new Vector3Int(rx, ry, 0), tile);
        
    }

    private void showGarbageEmpty(int x, int y)
    {
        int rx = x - width / 2;
        int ry = height / 2 - y;
        gbTilemap.SetTile(new Vector3Int(rx, ry, 0), null);
    }

    private void showGarbage(int x, int y, ref InteractiveCell cell)
    {
        int rx = x - width / 2;
        int ry = height / 2 - y;
        if (cell.getType() != EnvCellType.ground || cell.garbage == -1)
        {
            gbTilemap.SetTile(new Vector3Int(rx, ry, 0), null);
            return;
        }
        Tile tile = garbages[cell.garbage];
        gbTilemap.SetTile(new Vector3Int(rx, ry, 0), tile);

    }

    // do late so that the player has a chance to move in update if necessary
    private void showLevelTile(int x, int y)
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);

        currentCell.x = x - width / 2;
        currentCell.y = height / 2 - y ;

        InteractiveCell cell = cells[0][x, y];

        switch (levels[0][x, y])
        {
            case EnvCellType.empty:
                if (y >= 5)
                {
                    levelTilemap.SetTile(currentCell, emptyTile);
                }
                else
                {
                    levelTilemap.SetTile(currentCell, null); // just don't draw it
                }
                break;
            case EnvCellType.ground:
                levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.ground][cell.neighbours]);
                break;
            case EnvCellType.rock:
                levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.rock][cell.neighbours]);
                break;
            case EnvCellType.metal:
                levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.metal][cell.neighbours]);
                break;
            case EnvCellType.rust:
                levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.rust][cell.neighbours]);
                break;
        }
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

                InteractiveCell cell = cells[currentLevelIndex][x, y];

                switch (levels[currentLevelIndex][x, y])
                {
                    case EnvCellType.empty:
                        if (y >= 5)
                        {
                            levelTilemap.SetTile(currentCell, emptyTile);
                        }
                        else
                        {
                            levelTilemap.SetTile(currentCell, null); // just don't draw it
                        }
                        break;
                    case EnvCellType.ground:
                        levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.ground][cell.neighbours]);
                        
                        break;
                    case EnvCellType.rock:
                        levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.rock][cell.neighbours]);
                        break;
                    case EnvCellType.metal:
                        levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.metal][cell.neighbours]);
                        break;
                    case EnvCellType.rust:
                        levelTilemap.SetTile(currentCell, tilesheet[EnvCellType.rust][cell.neighbours]);
                        break;
                }
                showGarbage(x, y, ref cell);
                showDecalEmpty(x, y);
            }
        }
    }

    public Vector2Int getTileFromPixel(Vector2Int pixel)
    {
        return new Vector2Int(pixel.x / tilePixelSize, pixel.y / tilePixelSize);
    }

    public bool isPixelValid(Vector2Int pix)
    {
        bool xcorrect = pix.x >= 0 && pix.x < width * tilePixelSize;
        bool ycorrect = pix.y >= 0 && pix.y < tilePixelSize * height;
        return xcorrect && ycorrect;
    }

    public EnvCellType getPixelType(Vector2Int pix)
    {
        return getTileType(pix.x / tilePixelSize, pix.y / tilePixelSize);
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

    public EnvCellType getTileType(Vector2Int tile)
    {
        int curTileZone = tile.y / height + levelIndex;

        return levels[curTileZone][tile.x, tile.y % height];
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
        for (int x = 1; x < width-1; ++x)
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
        int holeWidth = 3;

        int holeStart = 5;
        int holeStop = 8;

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
            loadGarbagesAsset();
            loadTileAsset("rust", EnvCellType.rust);
            loadDecalAsset("rust", EnvCellType.rust);
            loadTileAsset("ground", EnvCellType.ground);
            loadDecalAsset("ground", EnvCellType.ground);
            loadTileAsset("rock", EnvCellType.rock);
            loadDecalAsset("rock", EnvCellType.rock);
            loadTileAsset("metal", EnvCellType.metal);

            Random.InitState((int)System.DateTime.Now.Ticks);

            levelXSeed = Random.Range(0, 10000);

            tiles = new EnvCellType[width, height];

            addNewLevel();
            setCurrentLevel(0);

            prepareBeginning();

            InteractiveCell[,] levelCells = cells[0];

            SetInteractiveCells(ref tiles, ref levelCells, 0);

            recalcNeighbours(ref levelCells);

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
                }
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            if (Input.GetMouseButtonDown(0) && canInteract)
            {
                Debug.LogWarning($"removing tile : ({tileInds.x}, {tileInds.y})");
                player.playSFX(curCell.getType());
                if (!curCell.Hit())
                {
                    int tileZone = setTileType(tileInds.x, tileInds.y, EnvCellType.empty);
                    //showLevel(curCell.getZone());
                    showLevelTile(tileInds.x, tileInds.y);
                    showDecalEmpty(tileInds.x, tileInds.y);
                    showGarbageEmpty(tileInds.x, tileInds.y);
                } else
                {
                    showDecal(tileInds.x, tileInds.y, ref curCell);
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
