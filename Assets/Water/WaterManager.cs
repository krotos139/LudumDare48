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

        spr = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.0f, 0.0f), (float) cellsPerUnit);
        rend.sprite = spr;

        // position in the world
        Vector2 tileMapBottomLeft = map.getBottomLeft();
        transform.position = new Vector3(tileMapBottomLeft.x, tileMapBottomLeft.y, -1.0f);

        // wg
        waterGrid = new WaterGrid((uint)width, (uint)height);

        syncLevel();
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
                            case CreatorBehaviour.CustomTileType.empty:
                                if (waterGrid.cells[i, j] != WaterGrid.cellType.water)
                                {
                                    waterGrid.cells[i, j] = WaterGrid.cellType.empty;
                                }
                                break;
                            case CreatorBehaviour.CustomTileType.ground:
                                 waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case CreatorBehaviour.CustomTileType.rock:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case CreatorBehaviour.CustomTileType.metall:
                                waterGrid.cells[i, j] = WaterGrid.cellType.ground;
                                break;
                            case CreatorBehaviour.CustomTileType.rust:
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
                    green = 255;
                    alpha = (byte)Random.Range(40, 120);
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

            if (Random.Range(1, 11) == 4)
            {
                waterGrid.cells[width /4, height / 3] = WaterGrid.cellType.water;
            }
            
            waterGrid.nextStep();
            rendedTexture();
        }
    }
}
