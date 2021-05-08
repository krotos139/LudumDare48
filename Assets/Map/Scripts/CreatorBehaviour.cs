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
    EnvCellType cellType = EnvCellType.empty;
    public int neighbours = 0;
    public int garbage = -1;

    public InteractiveCell(EnvCellType _cellType)
    {
        cellType = _cellType;
        durability = typeDurability[cellType];
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
    public void setType(EnvCellType type)
    {
        cellType = type;
    }
}

public class CreatorBehaviour : MonoBehaviour
{

    public Tile emptyTile;
    public Tilemap levelTilemap;
    public Tilemap decalTilemap;
    public Tilemap gbTilemap;
    public PlayerManager player;

    public GameObject selectedCell;

    public int width = 24;
    public int height = 10;

    public List<InteractiveCell[]> level = null;

    private int levelXSeed = 0;

    public int tilePixelSize = 48;


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

    private void recalcNeighbours()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                InteractiveCell cell = level[y][x];
                cell.neighbours = GetTileOrthoNeighbours(x, y, cell.getType());
                level[y][x] = cell;
            }
        }
    }

    private void fillLevelByNoise(int yStart, int yEnd, EnvCellType[] materials, float[] materialsWeights, float perlinCoefX, float perlinCoefY, float xorg, float yorg)
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
            for (int y = yStart; y < (yEnd- yStart); y++)
            {
                float fx = ((float)x) / width * perlinCoefX;
                float fy = ((float)y) / height * perlinCoefY;
                float noiseValue = Mathf.PerlinNoise(xorg + fx, yorg + fy);
                level[y][x] = new InteractiveCell(getMaterialFromNoise(ref materials, ref materialsThresholds, noiseValue));
            }
        }
    }

    private void makeRust(int x, int y, int threshold)
    {
        List<int> neighs = GetNeighbours(x, y);
        int metalCells = 0;
        for (int i = 0; i < neighs.Count; i += 2)
        {
            int nx = neighs[i];
            int ny = neighs[i + 1];
            if (level[ny][nx].getType() == EnvCellType.metal)
            {
                metalCells++;
                if (metalCells > threshold)
                {
                    level[y][x].setType(EnvCellType.rust);
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

    private void fillBoundaries(int yStart, int yEnd)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        
        // add metal boundaries

        for (int y = yStart; y < (yEnd- yStart); y++)
        {
            float leftLedge = Random.Range(0.0f, 1.0f);
            int iLeftLedge = leftLedge > 0.5f ? 1 : 0;
            float rightLedge = Random.Range(-1.0f, 0.0f);
            int iRightLedge = rightLedge > -0.5f ? 0 : -1;

            level[y][0].setType(EnvCellType.metal);
            level[y][iLeftLedge].setType(EnvCellType.metal);

            level[y][width - 1].setType(EnvCellType.metal);
            level[y][width - 1 + iRightLedge].setType(EnvCellType.metal);
        }

        // add rust, if there are more than 3 neighbouring metal cells

        int metalCellsThreshold = 0;

        for (int y = yStart; y < (yEnd - yStart); y++)
        {
            int x = 0;
            while (x < width && level[y][x].getType() == EnvCellType.metal)
            {
                x++;
            }
            if (x < width)
            {
                metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
                makeRust(x, y, metalCellsThreshold);
            }

            x = width - 1;
            while (x >= 0 && level[y][x].getType() == EnvCellType.metal)
            {
                x--;
            }
            if (x >= 0)
            {
                metalCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
                makeRust(x, y, metalCellsThreshold);
            }
        }
    }

    private void makeLevelPerlin(int yStart, int yEnd)
    {
        EnvCellType[] materials = new EnvCellType[] { EnvCellType.empty, EnvCellType.ground, EnvCellType.rock, EnvCellType.rust, EnvCellType.metal };
        float[] weights = new float[] { 0.2f, 0.25f, 0.15f, 0.10f, 0.30f };

        // fill main area

        fillLevelByNoise(yStart, yEnd, materials, weights, 5.0f, 5.0f * (height / width), 5.0f * levelXSeed, 0);

        // fill left and right boundaries

        fillBoundaries(yStart, yEnd);
    }

    public void addNewLevel()
    {
        for (int y=0; y<height; y++)
        {
            InteractiveCell[] levelTiles = new InteractiveCell[width];
            for (int x = 0; x < width; x++)
            {
                levelTiles[x] = new InteractiveCell(EnvCellType.empty);
            }
            level.Add(levelTiles);
        }

        makeLevelPerlin(0, height);

        showLevel(0, height);
    }

    
    public void addNewLineOfLevel()
    {
        InteractiveCell[] levelTiles = new InteractiveCell[width];
        for (int x = 0; x < width; x++)
        {
            levelTiles[x] = new InteractiveCell(EnvCellType.empty);
        }
        level.Add(levelTiles);

        int _height = level.Count;

        makeLevelPerlin(height-1, height);

        showLevel(height - 1, height);
    }

    public float curZoneDeep()
    {
        return  height;
    }

    public float curZoneStart()
    {
        return 0;
    }

    public float currentLevelMiddle()
    {
        return ((float)height) / 2;
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

        InteractiveCell cell = level[y][x];

        switch (cell.getType())
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
    private void showLevel(int yStart, int yEnd)
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);
        for (int x = 0; x < width; x++)
        {
            for (int y = yStart; y < (yEnd- yStart); y++)
            {
                currentCell.x = x - width / 2;
                currentCell.y = height / 2 - y;

                InteractiveCell cell = level[y][x];

                switch (cell.getType())
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
        return new Vector2((float)origin.x, (float)origin.y);
    }

    public bool isValidTileIndices(int tileX, int tileY)
    {
        if (tileX >= 0 && tileX < width && tileY >= 0)
        {
            if (tileY >= height)
            {
                return false;
            }
            return true;
        }
        return false;
    }

    public EnvCellType getTileType(int tileX, int tileY)
    {

        return level[tileY][tileX].getType();
    }

    public EnvCellType getTileType(Vector2Int tile)
    {
        return level[tile.y][tile.x].getType();
    }

    public void setTileType(int tileX, int tileY, EnvCellType someType)
    {
        level[tileY][tileX].setType(someType);
    }

    public ref InteractiveCell getCell(int tileX, int tileY)
    {
        return ref level[tileY][tileX];
    }

    public bool needLevelAddition(float curDeep)
    {
        if (curDeep > currentLevelMiddle())
        {
            return true;
        }
        return false;
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


    public void Init()
    {
        if (level == null)
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

            level = new List<InteractiveCell[]>();

            addNewLevel();

            prepareBeginning();

            recalcNeighbours();
        }
        showLevel(0, height);
    }

    // Update is called once per frame
    void Update()
    {
        if (!player.isDead)
        {
            bool canInteract = false;
            Vector2 playerPosition = player.getPosition();

            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 zoneBottom = getZoneBottomLeft();
            Vector3 relPosition = cursorPosition - new Vector3(zoneBottom.x, zoneBottom.y, 0.0f);
            // relPosition.y += height * (levelIndex);
            relPosition.y = height - relPosition.y;
            Vector2Int tileInds = new Vector2Int((int)Mathf.Round(relPosition.x - 0.5f), (int)Mathf.Round(relPosition.y - 0.5f));
            Vector2 tileCoord = new Vector2(tileInds.x + zoneBottom.x + 0.5f, zoneBottom.y + height - tileInds.y - 0.5f);

            if (isValidTileIndices(tileInds.x, tileInds.y))
            {
                InteractiveCell curCell = getCell(tileInds.x, tileInds.y);
                EnvCellType cellType = curCell.getType();
                int cellDurability = curCell.getDurability();

                if (cellType != EnvCellType.empty)
                {
                    Vector2Int playerTile = player.getPlayerCenterTile();
                    List<int> neighs = GetNeighbours(playerTile.x, playerTile.y);

                    for (int i = 0; i < neighs.Count; i += 2)
                    {
                        if (neighs[i] == tileInds.x && neighs[i+1] == tileInds.y)
                        {
                            canInteract = true;
                            break;
                        }
                    }
                }

                if (canInteract)
                {
                    selectedCell.SetActive(true);
                    selectedCell.transform.position = new Vector3(tileCoord.x, tileCoord.y, -1.2f);
                }
                else
                {
                    selectedCell.SetActive(false);
                }

                bool leftMouseButtonPressed = Input.GetMouseButtonDown(0);

                if (leftMouseButtonPressed)
                {
                    //Debug.LogWarning($"chosen tile : ({tileInds.x}, {tileInds.y}), relpos({relPosition.x}, {relPosition.y}), pos({cursorPosition.x}, {cursorPosition.y})");
                }

                if (leftMouseButtonPressed && canInteract)
                {
                    // Debug.LogWarning($"removing tile : ({tileInds.x}, {tileInds.y})");
                    player.playSFX(curCell.getType());
                    if (cellType != EnvCellType.metal)
                    {
                        if (!curCell.Hit())
                        {
                            setTileType(tileInds.x, tileInds.y, EnvCellType.empty);
                            //showLevel(curCell.getZone());
                            showLevelTile(tileInds.x, tileInds.y);
                            showDecalEmpty(tileInds.x, tileInds.y);
                            showGarbageEmpty(tileInds.x, tileInds.y);
                        }
                        else
                        {
                            showDecal(tileInds.x, tileInds.y, ref curCell);
                        }
                    }
                    player.Dig();
                }
            }
        }
    }
}
