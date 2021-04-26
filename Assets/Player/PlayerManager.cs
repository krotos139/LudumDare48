using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class PlayerManager : MonoBehaviour
{
    private float accelForce = 0.008f;
    private float accelXDecay = 0.1f;
    private float accelYDecay = 0.02f;
    private float jumpForce = 0.2f;
    private float gravity = 0.29f;

    private float x;
    private float y;
    private float accelX;
    private float accelY;

    private int health = 2;
    private bool right = true;
    private bool grounded = true;

    public Sprite playerSprite;
    private SpriteRenderer sr;
    public GameObject world;
    CreatorBehaviour map;
    Animator anim;

    public WaterManager water;

    // Ambient
    public AudioMixer mixer;
    public int TopToUndergroundY = 15;
    public int UndergroundToTopY = 5;
    public AudioClip[] clipsBGT = new AudioClip[2];
    public AudioClip[] clipsBGU = new AudioClip[2];
    private AudioSource[] audioSourcesBGT ;
    private AudioSource[] audioSourcesBGU ;

    // Music
    public AudioClip[] clips = new AudioClip[2];
    private int clipsVolume = -10;

    private int flip = 0;
    private int clipIndex = 1;
    private AudioSource[] audioSources = new AudioSource[2];

    public AudioClip clipsDigGround;
    public AudioClip clipsDigRust;
    public AudioClip clipsDigRock;
    private AudioSource digAudioSource;

    public bool mortal = true;
    public bool damaged;
    public int damagedDelay = 0;
    public bool isDead = false;

    private enum MovementDirection
    {
        Right,
        Left,
        Top,
        Bottom
    };

    private Dictionary<MovementDirection, List<float>> movementBlockPointShift = new Dictionary<MovementDirection, List<float>>();
    private Dictionary<MovementDirection, List<int>> movementPixelBlockPointShift = new Dictionary<MovementDirection, List<int>>();

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        x = 5;
        y = 144;

        accelX = 0;
        accelY = 0;

        map = world.GetComponent<CreatorBehaviour>();

        movementBlockPointShift.Add(MovementDirection.Right, new List<float>()  {  0.33f, 0.35f,   0.33f, -0.35f  });
        movementBlockPointShift.Add(MovementDirection.Left, new List<float>()   { -0.33f, 0.35f,  -0.33f, -0.35f  });
        movementBlockPointShift.Add(MovementDirection.Top, new List<float>()    {  0.33f, -0.5f, -0.33f, -0.5f  });
        movementBlockPointShift.Add(MovementDirection.Bottom, new List<float>() {  0.33f, 0.5f,  -0.33f,  0.5f  });

        movementPixelBlockPointShift.Add(MovementDirection.Right, new List<int>() { 1, 1, 1, -1 });
        movementPixelBlockPointShift.Add(MovementDirection.Left, new List<int>() { -1, 1, -1, -1 });
        movementPixelBlockPointShift.Add(MovementDirection.Top, new List<int>() { 1, -1, -1, -1 });
        movementPixelBlockPointShift.Add(MovementDirection.Bottom, new List<int>() { 1, 1, -1, 1 });

        anim = GetComponent<Animator>();
        health = 2;
        anim.SetTrigger("health_2");
        anim.SetBool("run", false);

        // Ambient

        audioSourcesBGT = new AudioSource[clipsBGT.Length];
        for (int i = 0; i < clipsBGT.Length; i++)
        {
            audioSourcesBGT[i] = gameObject.AddComponent<AudioSource>();
            audioSourcesBGT[i].clip = clipsBGT[i];
            audioSourcesBGT[i].loop = true;
            string _OutputMixer = string.Format("BGT{0}", (i + 1));
            audioSourcesBGT[i].outputAudioMixerGroup = mixer.FindMatchingGroups(_OutputMixer)[0];
            audioSourcesBGT[i].Play(0);
        }
        audioSourcesBGU = new AudioSource[clipsBGU.Length];
        for (int i = 0; i < clipsBGU.Length; i++)
        {
            audioSourcesBGU[i] = gameObject.AddComponent<AudioSource>();
            audioSourcesBGU[i].clip = clipsBGU[i];
            audioSourcesBGU[i].loop = true;
            string _OutputMixer = string.Format("BGU{0}", (i + 1));
            audioSourcesBGU[i].outputAudioMixerGroup = mixer.FindMatchingGroups(_OutputMixer)[0];
            audioSourcesBGU[i].Play(0);
        }

        // Music
        for (int i = 0; i < 2; i++)
        {
            audioSources[i] = gameObject.AddComponent<AudioSource>();
            audioSources[i].outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
        }
        clipIndex = Random.Range(0, clips.Length-1);
        mixer.SetFloat("Music", clipsVolume);

        digAudioSource = gameObject.AddComponent<AudioSource>();
        digAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
    }

    private Vector2Int getPixelPosition(Vector2 curPos)
    {
        Vector2Int curPixelPos = new Vector2Int((int)(curPos.x * map.tilePixelSize), (int)(curPos.y * map.tilePixelSize));
        return curPixelPos;
    }

    private void getMovementSizeShifts(MovementDirection dir, ref int xdec, ref int ydec)
    {
        switch (dir)
        {
            case MovementDirection.Bottom:
                xdec = -5;
                break;
            case MovementDirection.Top:
                xdec = -5;
                ydec = -6;
                break;
            case MovementDirection.Left:
                ydec = -6;
                xdec = -5;
                break;
            case MovementDirection.Right:
                ydec = -6;
                xdec = -5;
                break;
        }
    }

    public void playSFX(EnvCellType type)
    {
        if (type == EnvCellType.empty || type == EnvCellType.metal)
        {
            return;
        }
        switch(type)
        {
            case EnvCellType.ground:
                digAudioSource.clip = clipsDigGround;
                break;
            case EnvCellType.rust:
                digAudioSource.clip = clipsDigRust;
                break;
            case EnvCellType.rock:
                digAudioSource.clip = clipsDigRock;
                break;
            }
        digAudioSource.loop = false;
        digAudioSource.Play(0);
    }

    private void movementPixelBlock(Vector2 curPos, MovementDirection dir)
    {
        // curPos is player center

        Vector2Int curPixelPos = getPixelPosition(curPos);

        List<int> pointShifts = movementPixelBlockPointShift[dir];

        int xdec = 0;
        int ydec = 0;

        getMovementSizeShifts(dir, ref xdec, ref ydec);

        int halfXSize = map.tilePixelSize / 2 + xdec;
        int halfYSize = map.tilePixelSize / 2 + ydec;

        Vector2Int firstPixel = new Vector2Int(curPixelPos.x + pointShifts[0] * halfXSize, curPixelPos.y + pointShifts[1] * halfYSize);
        Vector2Int secondPixel = new Vector2Int(curPixelPos.x + pointShifts[2] * halfXSize, curPixelPos.y + pointShifts[3] * halfYSize);

        Vector2Int firstTile = map.getTileFromPixel(firstPixel);
        Vector2Int secondTile = map.getTileFromPixel(secondPixel);

        Vector2Int firstTileWidth = new Vector2Int(firstTile.x * map.tilePixelSize, (firstTile.x + 1) * map.tilePixelSize);
        Vector2Int firstTileHeight = new Vector2Int(firstTile.y * map.tilePixelSize, (firstTile.y + 1) * map.tilePixelSize);

        //Debug.Log($"movementPixelBlock: accel ({accelX}, {accelY}), position ({curPos.x}, {curPos.y}), tiles to check ({firstTile.x}, {firstTile.y}), ({secondTile.x}, {secondTile.y})");

        if (map.isPixelValid(firstPixel))
        {
            if (map.getTileType(firstTile) != EnvCellType.empty)
            {
                //Debug.Log($"movementPixelBlock: tile ({firstTile.x}, {firstTile.y}) - blocker");
                if (dir == MovementDirection.Left)
                {
                    x += 0.001f;
                    accelX = 0f;
                }
                else if (dir == MovementDirection.Right)
                {
                    x -= 0.001f;
                    accelX = 0f;
                }
                else
                {
                    accelY = 0;
                }
            }
        }

        if (map.isPixelValid(secondPixel))
        {
            if (map.getTileType(secondTile) != EnvCellType.empty)
            {
                //Debug.Log($"movementPixelBlock: tile ({secondTile.x}, {secondTile.y}) - blocker");
                if (dir == MovementDirection.Left)
                {
                    x += 0.001f;
                    accelX = 0f;
                }
                else if (dir == MovementDirection.Right)
                {
                    x -= 0.001f;
                    accelX = 0f;
                }
                else
                {
                    accelY = 0;
                }
            }
        }
    }

    private bool isPixelGrounded(Vector2 curPos)
    {
        Vector2Int curPixelPos = getPixelPosition(curPos);

        List<int> pointShifts = movementPixelBlockPointShift[MovementDirection.Bottom];

        int halfSize = map.tilePixelSize / 2;

        int xdec = 0;
        int ydec = 0;

        getMovementSizeShifts(MovementDirection.Bottom, ref xdec, ref ydec);

        int halfXSize = map.tilePixelSize / 2 + xdec;
        int halfYSize = map.tilePixelSize / 2;

        Vector2Int firstPixel = new Vector2Int(curPixelPos.x + pointShifts[0] * halfXSize, curPixelPos.y + pointShifts[1] * halfYSize + 1);
        Vector2Int secondPixel = new Vector2Int(curPixelPos.x + pointShifts[2] * halfXSize, curPixelPos.y + pointShifts[3] * halfYSize + 1);

        Vector2Int firstTile = map.getTileFromPixel(firstPixel);
        Vector2Int secondTile = map.getTileFromPixel(secondPixel);

        bool grounded = isGrounded(curPos);

        bool answer = false;

        //Debug.Log($"isPixelGrounded: accel ({accelX}, {accelY}), position ({curPos.x}, {curPos.y}), tiles to check ({firstTile.x}, {firstTile.y}), ({secondTile.x}, {secondTile.y})");

        if (map.isPixelValid(firstPixel))
        {
            if (map.getTileType(firstTile) != EnvCellType.empty)
            {
                //Debug.Log($"isPixelGrounded: tile ({firstTile.x}, {firstTile.y}) - ground");
                answer = true;
            }
        }

        if (map.isPixelValid(secondPixel))
        {
            if (map.getTileType(secondTile) != EnvCellType.empty)
            {
                //Debug.Log($"isPixelGrounded: tile ({secondTile.x}, {secondTile.y}) - ground");
                answer = true;
            }
        }


        return answer;
    }

    private void movementBlock(Vector2 curPos, MovementDirection dir)
    {
        // curPos is player center

        List<float> pointShifts = movementBlockPointShift[dir];

        Vector2 firstPoint = new Vector2(curPos.x + pointShifts[0], curPos.y + pointShifts[1]);
        Vector2 secondPoint = new Vector2(curPos.x + pointShifts[2], curPos.y + pointShifts[3]);

        Vector2Int iFirstPoint = new Vector2Int(Mathf.RoundToInt(firstPoint.x), Mathf.RoundToInt(firstPoint.y));
        Vector2Int iSecondPoint = new Vector2Int(Mathf.RoundToInt(secondPoint.x), Mathf.RoundToInt(secondPoint.y));

        if (map.isValidTileIndices(iFirstPoint.x, iFirstPoint.y))
        {
            if (map.getTileType(iFirstPoint.x, iFirstPoint.y) != EnvCellType.empty)
            {
                if (dir == MovementDirection.Left || dir == MovementDirection.Right)
                {
                    accelX = 0;
                }
                else
                {
                    accelY = 0;
                }
            }
        }

        if (map.isValidTileIndices(iSecondPoint.x, iSecondPoint.y))
        {
            if (map.getTileType(iSecondPoint.x, iSecondPoint.y) != EnvCellType.empty)
            {
                if (dir == MovementDirection.Left || dir == MovementDirection.Right)
                {
                    accelX = 0;
                }
                else
                {
                    accelY = 0;
                }
            }
        }
    }

    private bool isGrounded(Vector2 curPos)
    {
        List <float> pointShifts = movementBlockPointShift[MovementDirection.Bottom];

        Vector2 firstPoint = new Vector2(curPos.x + pointShifts[0], curPos.y + pointShifts[1] + 0.25f);
        Vector2 secondPoint = new Vector2(curPos.x + pointShifts[2], curPos.y + pointShifts[3] + 0.25f);

        Vector2Int iFirstPoint = new Vector2Int((int)firstPoint.x, (int)firstPoint.y);
        Vector2Int iSecondPoint = new Vector2Int((int)secondPoint.x, (int)secondPoint.y);

        if (map.isValidTileIndices(iFirstPoint.x, iFirstPoint.y))
        {
            if (map.getTileType(iFirstPoint.x, iFirstPoint.y) != EnvCellType.empty)
            {
                return true;
            }
        }

        if (map.isValidTileIndices(iSecondPoint.x, iSecondPoint.y))
        {
            if (map.getTileType(iSecondPoint.x, iSecondPoint.y) != EnvCellType.empty)
            {
                return true;
            }
        }

        return false;
    }

    float myClamp(float val, float min, float max)
    {
        float v = (val - min) / (max - min);
        return Mathf.Min(1.0f, Mathf.Max(0.0f, v));
    }

    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.F))
        {
            Dig();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            Die();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Damage();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (health == 2)
            {
                health = 1;
                anim.SetTrigger("health_1");
            }
            else if (health == 1)
            {
                health = 0;
                anim.SetTrigger("health_0");
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (health == 0)
            {
                health = 1;
                anim.SetTrigger("health_1");
            }
            else if (health == 1)
            {
                health = 2;
                anim.SetTrigger("health_2");
            }
        }
        */

        Vector2 relPosition = new Vector2(x, y) - map.getBottomLeft();
        relPosition.y = map.height - relPosition.y;
        grounded = isPixelGrounded(relPosition);

        if (!isDead)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                if (grounded)
                {
                    accelY = jumpForce;
                    anim.SetInteger("accel_y", 1);
                    anim.SetBool("grounded", false);
                    anim.SetTrigger("jump");
                }
            }

            if (Input.GetKey(KeyCode.S))
            {
                accelY -= accelForce;
            }
            if (Input.GetKey(KeyCode.D))
            {
                accelX += accelForce;
            }
            if (Input.GetKey(KeyCode.A))
            {
                accelX -= accelForce;
            }
        }
        if (accelX > (accelForce / 10) || (accelX < -accelForce / 10))
        {
            anim.SetBool("run", true);
        }
        else
        {
            anim.SetBool("run", false);
        }
        accelX = Mathf.Lerp(accelX, 0, accelXDecay * Time.deltaTime * 100.0f);
        accelY = Mathf.Lerp(accelY, -gravity, accelYDecay * Time.deltaTime * 100.0f);

        CheckTiles();

        x += accelX * Time.deltaTime * 100.0f;
        y += accelY * Time.deltaTime * 100.0f;

        if (accelX > 0)
        {
            right = true;
        }
        else if (accelX < 0)
        {
            right = false;
        }
        sr.flipX = !right;

        if (accelY< 0)
        {
            anim.SetInteger("accel_y", -1);
        }
        else if (accelY > 0)
        {
            anim.SetInteger("accel_y", 1);
        }
        else
        {
            anim.SetInteger("accel_y", 0);
        }
        anim.SetBool("grounded", grounded);

        transform.position = new Vector3(x, y, -2);

        var pos = getPosition();

        if (map.isValidTileIndices((int)pos.x, (int)pos.y))
        {
            if (water.tileHasWater((int)pos.x, (int)pos.y) && !damaged)
            {
                if (health == 0)
                {
                    anim.SetTrigger("death");
                    if (mortal)
                    {
                        isDead = true;
                    }
                    damaged = true;
                    damagedDelay = 800;
                }
                if (health == 1)
                {
                    health = 0;
                    anim.SetInteger("health", health);
                    anim.SetTrigger("health_0");
                    anim.SetTrigger("damaged");
                    damaged = true;
                    damagedDelay = 200;
                }
                if (health == 2)
                    {
                    health = 1;
                    anim.SetInteger("health", health);
                    anim.SetTrigger("health_1");
                    anim.SetTrigger("damaged");
                    damaged = true;
                    damagedDelay = 200;
                }
            }
        }

        Vector2 relPos = getPosition();

        float levelTU = myClamp(relPos.y, TopToUndergroundY, UndergroundToTopY);
        Debug.Log("relPos.y:" + relPos.y + " TopToUndergroundY:" + TopToUndergroundY + " UndergroundToTopY:" + UndergroundToTopY);
        if (levelTU > 1.0f) levelTU = 1.0f;
        if (levelTU < 0.0f) levelTU = 0.0f;
        mixer.SetFloat("BGT", -80.0f * levelTU);
        mixer.SetFloat("BGU", levelTU * 80.0f - 80.0f);

        float noiseValue = Mathf.PerlinNoise(x / 30, Time.time * 0.05f);
        float countEmbs = 5.0f;
        
        float emb1 = myClamp(noiseValue, 1.0f / countEmbs, 0.0f);
        float emb2 = myClamp(noiseValue, 2.0f / countEmbs, 1.0f / countEmbs) - myClamp(noiseValue, 1.0f / countEmbs, 0.0f);
        float emb3 = myClamp(noiseValue, 3.0f / countEmbs, 2.0f / countEmbs) - myClamp(noiseValue, 2.0f / countEmbs, 1.0f / countEmbs);
        float emb4 = myClamp(noiseValue, 4.0f / countEmbs, 3.0f / countEmbs) - myClamp(noiseValue, 3.0f / countEmbs, 2.0f / countEmbs);
        float emb5 = myClamp(noiseValue, 5.0f / countEmbs, 4.0f / countEmbs) - myClamp(noiseValue, 4.0f / countEmbs, 3.0f / countEmbs);

        mixer.SetFloat("BGT1", emb1 * 80.0f - 80.0f);
        mixer.SetFloat("BGT2", emb2 * 80.0f - 80.0f);
        mixer.SetFloat("BGT3", emb3 * 80.0f - 80.0f);
        mixer.SetFloat("BGT4", emb4 * 80.0f - 80.0f);
        mixer.SetFloat("BGT5", emb5 * 80.0f - 80.0f);
        mixer.SetFloat("BGU1", emb1 * 80.0f - 80.0f);
        mixer.SetFloat("BGU2", emb2 * 80.0f - 80.0f);
        mixer.SetFloat("BGU3", emb3 * 80.0f - 80.0f);
        mixer.SetFloat("BGU4", emb4 * 80.0f - 80.0f);
        mixer.SetFloat("BGU5", emb5 * 80.0f - 80.0f);

        if (!audioSources[1 - flip].isPlaying)
        {
            audioSources[flip].clip = clips[clipIndex];
            audioSources[flip].Play(0);
            Debug.Log("Scheduled source " + flip);
            flip = 1 - flip;
            clipIndex = clipIndex + 1;
            if (clipIndex+1 >= clips.Length)
            {
                clipIndex = 0;
            }
        }

        if (damaged)
        {
            if (--damagedDelay <= 0)
            {
                damaged = false;
            }
        }
    }

    void CheckTiles()
    {
        Vector2 relPosition = new Vector2(x, y + accelY) - map.getZoneBottomLeft();
        //Debug.Log($"before: {relPosition.x}, {relPosition.y}");
        relPosition.y = map.height - relPosition.y;


        var pos = getPositionInTile();

        if (accelY < 0.0f)
        {
            movementPixelBlock(relPosition, MovementDirection.Bottom);
        }
        else
        {
            //Debug.Log($"after: {relPosition.x}, {relPosition.y}");
            movementPixelBlock(relPosition, MovementDirection.Top);
        }
        
        relPosition = new Vector2(x + accelX, y) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;

        if (accelX > 0.0f)
        {
            movementPixelBlock(relPosition, MovementDirection.Right);
        }
        else
        {
            movementPixelBlock(relPosition, MovementDirection.Left);
        }

        if (map.needLevelAddition(relPosition.y + map.curZoneStart()))
        {
            map.addNewLevel();
        }

        /*
        
        if (relPosition.y > map.curZoneDeep() + map.height / 4)
        {
            map.setCurrentLevel(map.nextLevelIndex());
        }

        */
        
    }

    public Vector2 getPosition()
    {
        Vector2 relPosition = new Vector2(x, y) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;
        return relPosition;
    }

    public Vector2Int getPositionInTile()
    {
        var v = new Vector2Int();

        v.x = (int) (x - map.getZoneBottomLeft().x)*48;
        v.y = (int) (map.height - y) * 48;

        return v;
    }

    public void Dig()
    {
        anim.SetInteger("health", health);
        anim.SetTrigger("dig");
    }

    public void Die()
    {
        anim.SetTrigger("death");
    }

    private void OnGUI()
    {
        //GUI.Button(new Rect(50, 50, 100, 100), "test");
        //accelForce = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), accelForce, .0001f, .01f);
        //GUI.TextField(new Rect(150, 25, 200, 20), "uskorenie: " + accelForce.ToString("0.0000"));
        //accelXDecay = GUI.HorizontalSlider(new Rect(25, 75, 100, 30), accelXDecay, 0f, 1f);
        //GUI.TextField(new Rect(150, 75, 200, 20), "zatuhanie uskoreniya: " + accelXDecay.ToString("0.00"));
        //accelYDecay = GUI.HorizontalSlider(new Rect(25, 125, 100, 30), accelYDecay, 0f, 1f);
        //GUI.TextField(new Rect(150, 125, 200, 20), "zatuhanie uskoreniya: " + accelYDecay.ToString("0.00"));
        //jumpForce = GUI.HorizontalSlider(new Rect(25, 175, 100, 30), jumpForce, .01f, 1f);
        //GUI.TextField(new Rect(150, 175, 200, 20), "pryzhok: " + jumpForce.ToString("0.000"));
        //gravity = GUI.HorizontalSlider(new Rect(25, 225, 100, 30), gravity, .01f, 1f);
        //GUI.TextField(new Rect(150, 225, 200, 20), "gravitacia: " + gravity.ToString("0.000"));
        //mortal = GUI.Toggle(new Rect(150, 250, 200, 20), mortal, "mortality: " + mortal.ToString());
    }

    private void Damage()
    {
        anim.SetTrigger("damaged");
    }
}
