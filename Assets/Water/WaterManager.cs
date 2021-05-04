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

    public float accelTime = 30.0f;
    private float timePassed = 0.0f;
    private float prevTime = 0.0f;

    float time = 0.0f;
    float waterTime = 1.0f / 60.0f; // 60 fps limit
    float waterTimePassed = 0.0f;
    public int speed = 3;

    public float waterVolume;

    private bool started = false;
    private int waterStepsPerUpdate = 1;

    public int GetDepth()
    {
        return waterGrid.GetDepth() / waterQuality;
    }

    public bool isPlaying()
    {
        return started;
    }

    public void startGame()
    {
        started = true;
        
    }

    public void stopGame()
    {
        started = false;
    }


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
        speed = 3;
        timePassed = 0.0f;
        prevTime = Time.time;
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
                        switch (map.level[y][x].getType())
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

    public void SetWaterStepsPerUpdate(int value)
    {
        waterStepsPerUpdate = value;
    }

    public int GetWaterStepsPerUpdate()
    {
        return waterStepsPerUpdate;
    }


    // Update is called once per frame

    void Update()
    {
        // 60 fps limit
        waterTimePassed += Time.deltaTime;
        if (waterTimePassed >= waterTime)
        {
            waterTimePassed = 0.0f;
            if (map != null && started)
            {
                syncLevel();

                if (waterCurDelay >= waterDelay)
                {
                    for (int i = 0; i < waterStepsPerUpdate; i++)
                    { 
                        if (Random.Range(0, 100 / speed) < 4)
                        {
                            waterGrid.cells[(map.width - 1) * waterQuality / 2, 1] = WaterGrid.cellType.water;
#if WATER_TEST
                        for (int i = 0; i < 15; i++)
                        {
                            waterGrid.cells[(map.width - 1) * waterQuality / 2 - 25 + i * 2, 1] = WaterGrid.cellType.water;
                        }
#endif
                            waterVolume += (1.0f / (float)(waterQuality * waterQuality)) * 3.78f;
                        }
                        waterGrid.nextStep();
                    }
                    waterCurDelay = 0;
                }
                else
                {
                    waterCurDelay++;
                }
                rendedTexture();
            }
        }
        time += 0.001f;
        float curTime = Time.time;
        timePassed += curTime - prevTime;
        prevTime = curTime;

        if (timePassed > accelTime)
        {
            if (waterDelay > 0) waterDelay--;
        }
    }

    void OnGUI()
    {
        //waterGrid.vyazkost = (int)GUI.HorizontalSlider(new Rect(25, 275, 100, 30), waterGrid.vyazkost, 0, 100);
        //GUI.TextField(new Rect(150, 275, 200, 20), "voda vyazkost: " + waterGrid.vyazkost.ToString("00"));
        //speed = (int)GUI.HorizontalSlider(new Rect(25, 325, 100, 30), speed, 2, 10);
        //GUI.TextField(new Rect(150, 325, 200, 20), "voda skorost: " + speed.ToString("00"));
    }
}
