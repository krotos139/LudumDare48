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
    public Tilemap levelTilemap;
    public PlayerManager player;

    public Texture2D pick;
    public Texture2D pick1;
    public Texture2D pick2;
    public Texture2D pick3;
    public Texture2D pick4;

    public int width = 50;
    public int height = 50;
    public float interactLength = 1.8f;

    public enum CustomTileType
    {
        ground, empty, rock, metall, rust
    }

    public CustomTileType[,] tiles = null;

    public int levelIndex = 0;
    private int levelXSeed = 0;
    public List<CustomTileType[,]> levels = new List<CustomTileType[,]>();

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

    private CustomTileType getMaterialFromNoise(ref CustomTileType[] materials, ref float[] materialsThresholds, float noiseValue)
    {
        //return CustomTileType.ground;
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

    private void fillLevelByNoise(ref CustomTileType [,] levelTiles, CustomTileType[] materials, float[] materialsWeights, float perlinCoefX, float perlinCoefY, float xorg, float yorg)
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

    private void makeRust(ref CustomTileType[,] levelTiles, int x, int y, int threshold)
    {
        List<int> neighs = GetNeighbours(x, y);
        int metallCells = 0;
        for (int i = 0; i < neighs.Count; i += 2)
        {
            int nx = neighs[i];
            int ny = neighs[i + 1];
            if (levelTiles[nx, ny] == CustomTileType.metall)
            {
                metallCells++;
                if (metallCells > threshold)
                {
                    levelTiles[x, y] = CustomTileType.rust;
                    break;
                }
            }
        }
    }

    private void fillBoundaries(ref CustomTileType[,] levelTiles)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);

        // add metall

        for (int x = 0; x < width; x++)
        {
            if (levelTiles[x, 0] == CustomTileType.empty) levelTiles[x, 0] = CustomTileType.ground;
            if (levelTiles[x, height - 1] == CustomTileType.empty) levelTiles[x, height - 1] = CustomTileType.ground;
        }

        for (int y = 0; y < height; y++)
        {
            float leftLedge = Random.Range(0.0f, 1.0f);
            int iLeftLedge = leftLedge > 0.5f ? 1 : 0;
            float rightLedge = Random.Range(-1.0f, 0.0f);
            int iRightLedge = rightLedge > -0.5f ? 0 : -1;

            levelTiles[0, y] = CustomTileType.metall;
            levelTiles[iLeftLedge, y] = CustomTileType.metall;

            levelTiles[width - 1, y] = CustomTileType.metall;
            levelTiles[width - 1 + iRightLedge, y] = CustomTileType.metall;
        }

        // add rust, if there are more than 3 neighbouring metall cells

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (levelTiles[x, y] == CustomTileType.metall)
            {
                x++;
            }
            int metallCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(ref levelTiles, x, y, metallCellsThreshold);

            x = width - 1;
            while (levelTiles[x, y] == CustomTileType.metall)
            {
                x--;
            }
            metallCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(ref levelTiles, x, y, metallCellsThreshold);
        }
    }

    private void makeLevelPerlin(ref CustomTileType[,] levelTiles)
    {
        CustomTileType[] materials = new CustomTileType[] { CustomTileType.empty, CustomTileType.ground, CustomTileType.rock, CustomTileType.rust, CustomTileType.metall };
        float[] weights = new float[] { 0.15f, 0.40f, 0.15f, 0.15f, 0.15f };

        // fill main area

        fillLevelByNoise(ref levelTiles, materials, weights, 12.0f, 12.0f * (height / width), 12.0f * levelXSeed, 12.0f * levels.Count * (height / width));

        // fill left and right boundaries

        fillBoundaries(ref levelTiles);
    }

    public void addNewLevel()
    {
        CustomTileType[,] levelTiles = new CustomTileType[width, height];
        makeLevelPerlin(ref levelTiles);
        levels.Add(levelTiles);
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
                    case CustomTileType.empty:
                        levelTilemap.SetTile(currentCell, emptyTile);
                        break;
                    case CustomTileType.ground:
                        levelTilemap.SetTile(currentCell, groundTile);
                        break;
                    case CustomTileType.rock:
                        levelTilemap.SetTile(currentCell, rockTile);
                        break;
                    case CustomTileType.metall:
                        levelTilemap.SetTile(currentCell, metallTile);
                        break;
                    case CustomTileType.rust:
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

    public CustomTileType getTileType(int tileX, int tileY)
    {
        int curTileZone = tileY / height + levelIndex;
        tileY %= height;

        return levels[curTileZone][tileX, tileY];
    }

    public int setTileType(int tileX, int tileY, CustomTileType someType)
    {
        int curTileZone = tileY / height + levelIndex;
        tileY %= height;

        levels[curTileZone][tileX, tileY] = someType;
        return curTileZone;
    }

    void prepareBeginning()
    {
        // clear layer for beginning
        for (int i = 3; i < width -3; ++i)
        {
            for (int j = 0; j < 5; ++j)
            {
                setTileType(i, j, CustomTileType.empty);                             
            }
        }

        for (int i = 10; i < 20; ++i)
        {
            for (int j = 5; j < 10; ++j)
            {
                setTileType(i, j, CustomTileType.empty);
            }
        }
    }

    public void Init()
    {
        if (tiles == null)
        {
            Random.InitState((int)System.DateTime.Now.Ticks);

            levelXSeed = Random.Range(0, 10000);

            tiles = new CustomTileType[width, height];

            addNewLevel();
            setCurrentLevel(0);

            prepareBeginning();
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

            if (Mathf.Abs(playerPosition.x - relPosition.x) <= interactLength && Mathf.Abs(playerPosition.y - relPosition.y) <= interactLength)
            {
                canInteract = true;
                Cursor.SetCursor(pick, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            if (Input.GetMouseButtonDown(0) && canInteract)
            {
                Debug.LogWarning($"removing tile : ({tileInds.x}, {tileInds.y})");

                int tileZone = setTileType(tileInds.x, tileInds.y, CustomTileType.empty);
                showLevel(tileZone);
            }

            if (Input.GetMouseButtonDown(1) && canInteract)
            {

                Debug.LogWarning($"adding tile : ({tileInds.x}, {tileInds.y})");

                int tileZone = setTileType(tileInds.x, tileInds.y, CustomTileType.ground);
                showLevel(tileZone);
            }
        }
    }
}
