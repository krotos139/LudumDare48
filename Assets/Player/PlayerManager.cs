using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public Sprite playerSprite;
    private SpriteRenderer sr;
    public GameObject world;
    CreatorBehaviour map;

    public WaterManager water;

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
        sr.sprite = playerSprite;

        x = 5;
        y = 144;

        accelX = 0;
        accelY = 0;

        map = world.GetComponent<CreatorBehaviour>();

        movementBlockPointShift.Add(MovementDirection.Right, new List<float>()  {  0.33f, 0.35f,   0.33f, -0.35f  });
        movementBlockPointShift.Add(MovementDirection.Left, new List<float>()   { -0.33f, 0.35f,  -0.33f, -0.35f  });
        movementBlockPointShift.Add(MovementDirection.Top, new List<float>()    {  0.33f, -0.5f, -0.33f, -0.5f  });
        movementBlockPointShift.Add(MovementDirection.Bottom, new List<float>() {  0.33f, 0.5f,  -0.33f,  0.5f  });
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
            if (map.getTileType(iFirstPoint.x, iFirstPoint.y) != CreatorBehaviour.CustomTileType.empty)
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
            if (map.getTileType(iSecondPoint.x, iSecondPoint.y) != CreatorBehaviour.CustomTileType.empty)
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
            if (map.getTileType(iFirstPoint.x, iFirstPoint.y) != CreatorBehaviour.CustomTileType.empty)
            {
                return true;
            }
        }

        if (map.isValidTileIndices(iSecondPoint.x, iSecondPoint.y))
        {
            if (map.getTileType(iSecondPoint.x, iSecondPoint.y) != CreatorBehaviour.CustomTileType.empty)
            {
                return true;
            }
        }

        return false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector2 relPosition = new Vector2(x, y) - map.getZoneBottomLeft();

            relPosition.y = map.height - relPosition.y;

            if (isGrounded(relPosition))
            {
                accelY += jumpForce;
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
        accelX = Mathf.Lerp(accelX, 0, accelDecay * Time.deltaTime * 100.0f);
        accelY = Mathf.Lerp(accelY, -gravity, accelDecay * Time.deltaTime * 100.0f);

        CheckTiles();

        x += accelX * Time.deltaTime * 100.0f;
        y += accelY * Time.deltaTime * 100.0f;

        transform.position = new Vector3(x, y, -2);

        var pos = getPosition();

        if (map.isValidTileIndices((int)pos.x, (int)pos.y))
        {
            if (water.tileHasWater((int)pos.x, (int)pos.y))
            {
                SceneManager.LoadScene("Death");
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

    void Dig()
    {

    }
}
