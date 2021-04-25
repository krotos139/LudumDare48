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

    private bool isValidIndices(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

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

    private void fillLevelByNoise(CustomTileType[] materials, float[] materialsWeights, float perlinCoef, float xorg, float yorg)
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

        for (int x = 0; x < width; x++)
        {
            if (tiles[x, 0] == CustomTileType.empty) tiles[x, 0] = CustomTileType.ground;
            if (tiles[x, height - 1] == CustomTileType.empty) tiles[x, height - 1] = CustomTileType.ground;
        }

        for (int y = 0; y < height; y++)
        {
            float leftLedge = Random.Range(0.0f, 1.0f);
            int iLeftLedge = leftLedge > 0.5f ? 1 : 0;
            float rightLedge = Random.Range(-1.0f, 0.0f);
            int iRightLedge = rightLedge > -0.5f ? 0 : -1;

            tiles[0, y] = CustomTileType.metall;
            tiles[iLeftLedge, y] = CustomTileType.metall;

            tiles[width - 1, y] = CustomTileType.metall;
            tiles[width - 1 + iRightLedge, y] = CustomTileType.metall;
        }

        // add rust, if there are more than 3 neighbouring metall cells

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (tiles[x, y] == CustomTileType.metall)
            {
                x++;
            }
            int metallCellsThreshold = (int)(Mathf.Round(Random.Range(0.3f, 1.0f) * 3.0f));
            makeRust(x, y, metallCellsThreshold);

            x = width - 1;
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
        float[] weights = new float[] { 0.45f, 0.20f, 0.15f, 0.10f, 0.15f };

        // fill main area

        fillLevelByNoise(materials, weights, 12.0f, 12.0f, 0.0f);

        // fill left and right boundaries

        fillBoundaries();
    }

    // do late so that the player has a chance to move in update if necessary
    private void showLevel()
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentCell.x = x - width / 2;
                currentCell.y = height / 2 - y;
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
        return new Vector2((float)origin.x, (float)origin.y);
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
        bool canInteract = false;
        Vector2 playerPosition = player.getPosition();
        Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 relPosition = cursorPosition - highlightMap.origin;
        relPosition.y = height - relPosition.y;
        Vector2Int tileInds = new Vector2Int((int)Mathf.Round(relPosition.x - 0.5f), (int)Mathf.Round(relPosition.y - 0.5f));
        if (Mathf.Abs(playerPosition.x - relPosition.x) <= interactLength && Mathf.Abs(playerPosition.y - relPosition.y) <= interactLength)
        {
            canInteract = true;
            Cursor.SetCursor(pick, Vector2.zero, CursorMode.Auto);
        } else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        if (Input.GetMouseButtonDown(0) && canInteract)
        {
            Debug.LogWarning($"removing tile : ({tileInds.x}, {tileInds.y})");

            tiles[tileInds.x, tileInds.y] = CustomTileType.empty;
            showLevel();
        }

        if (Input.GetMouseButtonDown(1) && canInteract)
        {

            Debug.LogWarning($"adding tile : ({tileInds.x}, {tileInds.y})");

            tiles[tileInds.x, tileInds.y] = CustomTileType.ground;
            showLevel();
        }
    }
}
