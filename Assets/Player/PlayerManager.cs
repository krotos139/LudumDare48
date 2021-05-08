using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class TimeWatcher
{
    private float prevTime = 0.0f;
    private float timeLimit = 1.0f; // 1 second

    public TimeWatcher(float _timeLimit)
    {
        timeLimit = _timeLimit;
        prevTime = 0.0f;
    }

    public void SetLimit(float _timeLimit)
    {
        timeLimit = _timeLimit;
    }

    public bool Allowed()
    {
        float curTime = Time.time;
        float dt = curTime - prevTime;
        return dt > timeLimit;
    }

    public void Reset()
    {
        prevTime = Time.time;
    }
}

public class TimeManager
{
    static Dictionary<string, TimeWatcher> timers = new Dictionary<string, TimeWatcher>();

    public static TimeWatcher GetTimer(string name)
    {
        if (!timers.ContainsKey(name))
        {
            timers[name] = new TimeWatcher(1.0f);
        }
        return timers[name];
    }
}

public class PlayerManager : MonoBehaviour
{
    private float x;
    private float y;

    private float accelX;
    private float accelY;

    private float velx;
    private float vely;

    private float gravity = -9.8f;
    private float gravityScale = 3.0f;

    private float jumpVel = 0.0f;
    private float jumpHeight = 2.1f;
    private float runVel = 5.0f;
    private float fallMaxVel = -20.0f;

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
    //private AudioSource[] audioSources = new AudioSource[2];

    private AudioSource bgMusic;

    public AudioClip clipsDigGround;
    public AudioClip clipsDigMetal;
    public AudioClip clipsDigRock;

    private AudioSource digAudioSource;

    public AudioClip clipsJump1;
    public AudioClip clipsJump2;

    public AudioClip clipsBurning;

    public AudioClip clipsHurt;
    public AudioClip clipsDiy;

    private AudioSource burningAudioSource;
    private AudioSource stateAudioSource;

    public bool mortal = true;
    public bool damaged;
    public int damagedDelay = 0;
    public bool isDead = false;

    public int depthAccelDifference = 10;
    TimeWatcher jumpTimer;

    private enum MovementDirection
    {
        Right,
        Left,
        Top,
        Bottom
    };

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        x = 5.5f;
        y = 144.0f;

        accelX = 0;
        accelY = 0;

        velx = 0.0f;
        vely = 0.0f;

        jumpVel = Mathf.Sqrt(2.0f * (-gravity) * gravityScale * jumpHeight);
        jumpTimer = TimeManager.GetTimer("jump0");
        jumpTimer.SetLimit(0.5f);

        map = world.GetComponent<CreatorBehaviour>();

        choosers.Add(MovementDirection.Right, chooseRight);
        choosers.Add(MovementDirection.Left, chooseLeft);
        choosers.Add(MovementDirection.Top, chooseTop);
        choosers.Add(MovementDirection.Bottom, chooseBottom);

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
        /*
        for (int i = 0; i < 2; i++)
        {
            audioSources[i] = gameObject.AddComponent<AudioSource>();
            audioSources[i].outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
        }
        */

        bgMusic = gameObject.AddComponent<AudioSource>();
        bgMusic.outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];

        clipIndex = Random.Range(0, clips.Length-1);
        mixer.SetFloat("Music", clipsVolume);

        digAudioSource = gameObject.AddComponent<AudioSource>();
        digAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];

        stateAudioSource = gameObject.AddComponent<AudioSource>();
        stateAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];

        burningAudioSource = gameObject.AddComponent<AudioSource>();
        burningAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];

        playMenu();
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
        if (type == EnvCellType.empty)
        {
            return;
        }
        switch(type)
        {
            case EnvCellType.ground:
                digAudioSource.clip = clipsDigGround;
                break;
            case EnvCellType.rust:
                digAudioSource.clip = clipsDigGround;
                break;
            case EnvCellType.rock:
                digAudioSource.clip = clipsDigRock;
                break;
            case EnvCellType.metal:
                digAudioSource.clip = clipsDigMetal;
                break;
        }
        digAudioSource.loop = false;
        digAudioSource.Play(0);
    }

    public void playJump()
    {
        if (jumpTimer.Allowed())
        {
            jumpTimer.Reset();
            int curindex = Random.Range(0, 50) > 25 ? 1 : 0;
            switch (curindex)
            {
                case 0:
                    stateAudioSource.clip = clipsJump1;
                    break;
                case 1:
                    stateAudioSource.clip = clipsJump2;
                    break;
            }
            stateAudioSource.loop = false;
            stateAudioSource.Play(0);
        }
    }

    public void playHurt()
    {
        stateAudioSource.clip = clipsHurt;
        stateAudioSource.loop = false;
        stateAudioSource.Play(0);
    }

    public void playDeath()
    {
        stateAudioSource.clip = clipsDiy;
        stateAudioSource.loop = false;
        stateAudioSource.Play(0);
    }

    public void playBurning()
    {
        burningAudioSource.clip = clipsBurning;
        burningAudioSource.loop = false;
        burningAudioSource.Play(0);
    }

    public void playMenu()
    {
        if (bgMusic.clip != clips[0])
        {
            bgMusic.Stop();
            bgMusic.clip = clips[0];
            bgMusic.loop = true;
            bgMusic.Play(0);
        }
    }

    public void playGame()
    {
        bgMusic.Stop();
        bgMusic.clip = clips[1];
        bgMusic.loop = true;
        bgMusic.Play(0);
    }

    public void playCredits()
    {
        bgMusic.Stop();
        bgMusic.clip = clips[2];
        bgMusic.loop = true;
        bgMusic.Play(0);
    }

    public void playDeathMusic()
    {
        bgMusic.Stop();
        bgMusic.clip = clips[3];
        bgMusic.loop = true;
        bgMusic.Play(0);
    }

    float lastGroundStand = 0.0f;
    float jumpActiveTime = 0.1f;
    bool  jumpPressed = false;

    private bool isGrounded(Vector2 curPos)
    {
        return blockMovement(new Vector2(curPos.x, curPos.y - 0.05f), MovementDirection.Bottom);
    }

    private bool canJump(Vector2 curPos)
    {
        bool onGround = blockMovement(new Vector2(curPos.x, curPos.y - 0.1f), MovementDirection.Bottom);
        bool wasOnGround = false;
        if (onGround)
        {
            jumpPressed = false;
            lastGroundStand = Time.time;
        }
        else
        {
            float timeInAir = Time.time - lastGroundStand;
            if (timeInAir < jumpActiveTime)
            {
                // cartoon effect - you can jump if you are not on ground, but was on it
                wasOnGround = true;
            }
        }
        return onGround || (wasOnGround && !jumpPressed);
    }

    float myClamp(float val, float min, float max)
    {
        float v = (val - min) / (max - min);
        return Mathf.Min(1.0f, Mathf.Max(0.0f, v));
    }

    void Update()
    {
        if (!water.isPlaying()) return;
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

        Vector2 curPos = new Vector2(x, y);
        Vector2 relPosition = curPos - map.getBottomLeft();
        relPosition.y = map.height - relPosition.y;
        grounded = isGrounded(curPos);   // for animation
        bool jumpAble = canJump(curPos); // for jump control

        if (!isDead)
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (vely > 0.0f)
                {
                    // player in jump state
                    float timeInAir = Time.time - lastGroundStand;
                    if (timeInAir < 0.2f)
                    {
                        // it helps us to get lower and higher jumps
                        vely *= 0.5f;
                    }
                }
            }
            if (Input.GetKey(KeyCode.Space))
            {
                if (jumpAble)
                {
                    jumpPressed = true;
                    playJump();
                    vely = jumpVel;
                    anim.SetInteger("accel_y", 1);
                    anim.SetBool("grounded", false);
                    anim.SetTrigger("jump");
                }
            }

            if (Input.GetKey(KeyCode.D))
            {
                velx = runVel;
            }
            if (Input.GetKey(KeyCode.A))
            {
                velx = -runVel;
            }
        }
        
        if (velx > 0.5 || velx < -0.5)
        {
            anim.SetBool("run", true);
        }
        else
        {
            anim.SetBool("run", false);
        }
        

        float dt = Time.deltaTime;

        // before checking everything should be known, but not yet appleid

        CheckTiles(ref velx, ref vely, dt);

        x += velx * dt;
        y += vely * dt;

        handleLevelEnlargement();

        vely += gravity * gravityScale * dt;

        if (vely < fallMaxVel)
        {
            // limited
            vely = fallMaxVel;
        }

        velx *= 0.001f; // fast decay

        if (velx > 0)
        {
            right = true;
        }
        else if (velx < 0)
        {
            right = false;
        }
        sr.flipX = !right;

        if (vely < 0)
        {
            anim.SetInteger("accel_y", -1);
        }
        else if (vely > 0)
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
                if (!isDead)
                {
                    playBurning();
                }

                if (health == 0)
                {
                    anim.SetTrigger("death");
                    if (mortal)
                    {
                        if (!isDead)
                        {
                            playDeathMusic();
                            playDeath();
                        }
                        isDead = true;
                    }
                    damaged = true;
                    damagedDelay = 800;
                }
                if (health == 1)
                {
                    playHurt();
                    health = 0;
                    anim.SetInteger("health", health);
                    anim.SetTrigger("health_0");
                    anim.SetTrigger("damaged");
                    damaged = true;
                    damagedDelay = 200;
                }
                if (health == 2)
                    {
                    playHurt();
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
        //Debug.Log("relPos.y:" + relPos.y + " TopToUndergroundY:" + TopToUndergroundY + " UndergroundToTopY:" + UndergroundToTopY);
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

        /*
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
        */

        if (damaged)
        {
            if (--damagedDelay <= 0)
            {
                damaged = false;
            }
        }

        // if player is playing well, increase water speed for him

        Vector2Int ptile = getPlayerCenterTile();
        int depth = water.GetDepth();

        if (ptile.y > depth)
        {
            int mult = ((ptile.y / depthAccelDifference) - (depth / depthAccelDifference)) * 2;
            mult = mult > 0 ? mult : 1;
            if (mult != water.GetWaterStepsPerUpdate())
            {
                Debug.LogWarning($"Increase water speed: {mult}");
                water.SetWaterStepsPerUpdate(mult);
            }
        }
    }

    Vector2Int[] indShifts = new Vector2Int[8] {
        new Vector2Int(-1, 0),
        new Vector2Int(-1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(1, -1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 1),
    };

    bool chooseBottom(Vector2Int shift)
    {
        return shift.y > 0;
    }

    bool chooseTop(Vector2Int shift)
    {
        return shift.y < 0;
    }

    bool chooseLeft(Vector2Int shift)
    {
        return shift.x < 0;
    }

    bool chooseRight(Vector2Int shift)
    {
        return shift.x > 0;
    }

    Dictionary <MovementDirection, System.Func<Vector2Int, bool>> choosers = new Dictionary<MovementDirection, System.Func<Vector2Int, bool>>();

    List<Vector2Int> getNeighbours(Vector2Int ind, MovementDirection dir)
    {
        System.Func<Vector2Int, bool> chooser = choosers[dir];
        List<Vector2Int> neighs = new List<Vector2Int>();

        foreach (Vector2Int curShift in indShifts)
        {
            if (chooser(curShift))
            {
                Vector2Int curInd = ind + curShift;
                if (map.isValidTileIndices(curInd.x, curInd.y))
                {
                    if (map.getTileType(curInd) != EnvCellType.empty)
                    {
                        neighs.Add(curInd);
                    }
                }
            }
        }

        return neighs;
    }

    bool blockMovement(Vector2 pos, MovementDirection dir)
    {
        // aabb algo
        // x, y - center of player sprite
        Vector2 relpos = pos - map.getZoneBottomLeft();
        relpos.y = map.height - relpos.y;
        Vector2Int tileInds = new Vector2Int((int)Mathf.Round(relpos.x - 0.5f), (int)Mathf.Round(relpos.y - 0.5f));
        //
        List<Vector2Int> neighs = getNeighbours(tileInds, dir);
        
        // width and height are equal to 1.0f

        int tileSize = map.tilePixelSize;

        int minx = (int)((relpos.x - 0.35f) * tileSize);
        int maxx = (int)((relpos.x + 0.35f) * tileSize);
        int miny = (int)((relpos.y - 0.35f) * tileSize);
        int maxy = (int)((relpos.y + 0.50f) * tileSize);

        foreach (Vector2Int neigh in neighs)
        {
            int nboxminx =  neigh.x * tileSize;
            int nboxmaxx = (neigh.x + 1) * tileSize;
            int nboxminy =  neigh.y * tileSize;
            int nboxmaxy = (neigh.y + 1) * tileSize;
            //
            if (maxx > nboxminx && minx < nboxmaxx && maxy > nboxminy && miny < nboxmaxy)
            {
                return true;
            }
        }
        return false;
    }

    void handleLevelEnlargement()
    {
        // TODO: make it endless
        Vector2 relPosition = new Vector2(x, y) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;

        if (map.needLevelAddition(relPosition.y + map.curZoneStart()))
        {
            map.addNewLineOfLevel();
        }
    }

    void CheckTiles(ref float _velx, ref float _vely, float dt)
    {
        
        Vector2 nextX = new Vector2(x + _velx * dt, y);
        Vector2 nextY = new Vector2(x, y + _vely * dt);

        if (_velx < 0.0f)
        {
            if (blockMovement(nextX, MovementDirection.Left))
            {
                _velx = 0.0f;
            }
        }

        if (_velx > 0.0f)
        {
            if (blockMovement(nextX, MovementDirection.Right))
            {
                _velx = 0.0f;
            }
        }

        if (_vely < 0.0f)
        {
            if (blockMovement(nextY, MovementDirection.Bottom))
            {
                _vely = 0.0f;
            }
        }

        if (_vely > 0.0f)
        {
            if (blockMovement(nextY, MovementDirection.Top))
            {
                _vely = 0.0f;
            }
        }
    }

    public Vector2 getPosition()
    {
        Vector2 relPosition = new Vector2(x, y) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;
        return relPosition;
    }

    public Vector2Int getPlayerCenterTile()
    {
        Vector2 pos = new Vector2(x, y);
        Vector2 relpos = pos - map.getZoneBottomLeft();
        relpos.y = map.height - relpos.y;
        Vector2Int tileInds = new Vector2Int((int)Mathf.Round(relpos.x - 0.5f), (int)Mathf.Round(relpos.y - 0.5f));
        return tileInds;
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
