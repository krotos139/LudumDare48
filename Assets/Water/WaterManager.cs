using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterManager : MonoBehaviour
{

    int width, height;
    byte[] table;
    Sprite spr;
    Texture2D tex;
    SpriteRenderer rend;
    WaterGrid waterGrid;

    public GameObject world;
    CreatorBehaviour map;
    public int waterQuality;
    public int waterDelay = 0;
    private int waterCurDelay = 0;
    float time;

    public float waterVolume;

    void Start()
    {
        map = world.GetComponent<CreatorBehaviour>();

        map.Init();

        Camera camera = Camera.main;

        int cellsPerUnit = waterQuality;

        height = map.height * waterQuality;
        width = map.width * waterQuality;

        rend = GetComponent<SpriteRenderer>();
        table = new byte[4 * width * height];
        tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        spr = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.0f, 0.0f), (float)cellsPerUnit);
        rend.sprite = spr;

        // position in the world
        Vector2 tileMapBottomLeft = map.getZoneBottomLeft();
        transform.position = new Vector3(tileMapBottomLeft.x, tileMapBottomLeft.y, -1.0f);

        // wg
        waterGrid = new WaterGrid((uint)width, (uint)height);

        syncLevel();
        time = 0.0f;
    }

    public bool tileHasWater(int tileX, int tileY)
    {
        var wManager = GetComponent<WaterManager>();

        for (int i = tileX * waterQuality; i < (tileX + 1) * waterQuality; ++i)
        {
            for (int j = tileY * waterQuality; j < (tileY + 1) * waterQuality; ++j)
            {
                if (waterGrid.cells[i, j] == WaterGrid.cellType.water) return true;
            }
        }

        return false;
    }

    private void syncLevel()
    {
        Vector3Int currentCell = new Vector3Int(0, 0, 0);
        for (int x = 0; x < map.width; x++)
        {
            for (int y = 0; y < map.height; y++)
            {
                for (int i = x * waterQuality; i < (x + 1) * waterQuality; i++)
                {
                    for (int j = y * waterQuality; j < (y + 1) * waterQuality; j++)
                    {
                        switch (map.tiles[x, y])
                        {
                            case EnvCellType.empty:
                                if (waterGrid.cells[i, j] != WaterGrid.cellType.water)
                                {
                                    waterGrid.cells[i, j] = WaterGrid.cellType.empty;
                                }
                                break;
                            case EnvCellType.ground:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case EnvCellType.rock:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case EnvCellType.metal:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case EnvCellType.rust:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                        }
                    }
                }
            }
        }
    }

    void rendedTexture()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                byte red = 0;
                byte green = 0;
                byte blue = 0;
                byte alpha = 0;
                if (waterGrid.cells[i, height - j - 1] == WaterGrid.cellType.water)
                {
                    red = 117;
                    green = 171;
                    blue = 34;
                    float arg = (float)(j * height + i) + time * (float)(i * j);
                    alpha = (byte)(Random.Range(240, 255));
                }

                table[j * width * 4 + i * 4 + 0] = red;
                table[j * width * 4 + i * 4 + 1] = green;
                table[j * width * 4 + i * 4 + 2] = blue;
                table[j * width * 4 + i * 4 + 3] = alpha;
            }
        }
        tex.SetPixelData(table, 0);
        tex.Apply();
    }
    // Update is called once per frame

    void Update()
    {
        if (map != null)
        {
            syncLevel();

            if (waterCurDelay == waterDelay)
            {
                if (Random.Range(1, 6) == 4)
                {
                    waterGrid.cells[45, 5] = WaterGrid.cellType.water;
                    waterVolume += (1.0f / (float)(waterQuality * waterQuality)) * 3.78f;
                }
                waterGrid.nextStep();
                waterCurDelay = 0;
            }
            else
            {
                waterCurDelay++;
            }
            rendedTexture();
        }
        time += 0.001f;
    }
}
