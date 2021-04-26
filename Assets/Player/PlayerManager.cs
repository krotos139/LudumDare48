using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class PlayerManager : MonoBehaviour
{
    private float accelForce = 0.002f;
    private float accelDecay = 0.02f;
    private float jumpForce = 0.2f;
    private float gravity = 0.15f;

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
    public int TopToUndergroundY = 0;
    public int UndergroundToTopY = -10;
    public AudioClip[] clipsBGT = new AudioClip[2];
    public AudioClip[] clipsBGU = new AudioClip[2];
    private AudioSource[] audioSourcesBGT ;
    private AudioSource[] audioSourcesBGU ;

    // Music
    public AudioClip[] clips = new AudioClip[2];
    private int clipsVolume = -20;

    private int flip = 0;
    private int clipIndex = 0;
    private AudioSource[] audioSources = new AudioSource[2];

    private enum MovementDirection
    {
        Right,
        Left,
        Top,
        Bottom
    };

    private Dictionary<MovementDirection, List<float>> movementBlockPointShift = new Dictionary<MovementDirection, List<float>>();

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

    }


    private void movementBlock(Vector2 curPos, MovementDirection dir)
    {
        // curPos is player center

        List<float> pointShifts = movementBlockPointShift[dir];

        Vector2 firstPoint = new Vector2(curPos.x + pointShifts[0], curPos.y + pointShifts[1]);
        Vector2 secondPoint = new Vector2(curPos.x + pointShifts[2], curPos.y + pointShifts[3]);

        Vector2Int iFirstPoint = new Vector2Int((int)firstPoint.x, (int)firstPoint.y);
        Vector2Int iSecondPoint = new Vector2Int((int)secondPoint.x, (int)secondPoint.y);

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
        if (Input.GetKeyDown(KeyCode.F))
        {
            Dig();
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

        Vector2 relPosition = new Vector2(x, y) - map.getBottomLeft();
        relPosition.y = map.height - relPosition.y;
        grounded = isGrounded(relPosition);
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            SceneManager.LoadScene("MainMenu");
        }
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
        if (accelX > (accelForce / 2) || (accelX < -accelForce / 2))
        {
            anim.SetBool("run", true);
        }
        else
        {
            anim.SetBool("run", false);
        }
        accelX = Mathf.Lerp(accelX, 0, accelDecay * Time.deltaTime * 100.0f);
        accelY = Mathf.Lerp(accelY, -gravity, accelDecay * Time.deltaTime * 100.0f);

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
            if (water.tileHasWater((int)pos.x, (int)pos.y))
            {
                SceneManager.LoadScene("Death");
            }
        }

        float levelTU = myClamp(y, TopToUndergroundY, UndergroundToTopY);
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
        

        if (!audioSources[1- clipIndex].isPlaying)
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


    }

    void CheckTiles()
    {
        Vector2 relPosition = new Vector2(x, y + accelY) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;

        if (accelY < 0.0f)
        {
            movementBlock(relPosition, MovementDirection.Bottom);
        }
        else
        {
            movementBlock(relPosition, MovementDirection.Top);
        }
        
        relPosition = new Vector2(x + accelX, y) - map.getZoneBottomLeft();
        relPosition.y = map.height - relPosition.y;

        if (accelX > 0.0f)
        {
            movementBlock(relPosition, MovementDirection.Right);
        }
        else
        {
            movementBlock(relPosition, MovementDirection.Left);
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

    public void Dig()
    {
        anim.SetTrigger("dig");
    }

    private void OnGUI()
    {
        //GUI.Button(new Rect(50, 50, 100, 100), "test");
        accelForce = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), accelForce, .0001f, .01f);
        GUI.TextField(new Rect(150, 25, 200, 20), "uskorenie: " + accelForce.ToString("0.0000"));
        accelDecay = GUI.HorizontalSlider(new Rect(25, 75, 100, 30), accelDecay, 0f, 1f);
        GUI.TextField(new Rect(150, 75, 200, 20), "zatuhanie uskoreniya: " + accelDecay.ToString("0.00"));
        jumpForce = GUI.HorizontalSlider(new Rect(25, 125, 100, 30), jumpForce, .01f, 1f);
        GUI.TextField(new Rect(150, 125, 200, 20), "pryzhok: " + jumpForce.ToString("0.000"));
        gravity = GUI.HorizontalSlider(new Rect(25, 175, 100, 30), gravity, .01f, 1f);
        GUI.TextField(new Rect(150, 175, 200, 20), "gravitacia: " + gravity.ToString("0.000"));
    }
}
