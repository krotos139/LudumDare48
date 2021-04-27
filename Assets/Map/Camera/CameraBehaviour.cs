using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public PlayerManager player;
    private Camera cam;

    private bool fixedSize = false;
    private bool started = false;


    public void startGame()
    {
        started = true;
    }

    public void stopGame()
    {
        started = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(0.0f, 155.5f, -10.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!fixedSize)
        {
            GameObject world = player.world;
            if (world != null)
            {
                CreatorBehaviour map = world.GetComponent<CreatorBehaviour>();
                if (map != null)
                {
                    cam = GetComponent<Camera>();
                    float halfWorldWidth = map.width / 2.0f + 8.0f;
                    cam.orthographicSize = halfWorldWidth / cam.aspect;
                    fixedSize = true;
                }
            }
        }

        Vector3 pos;
        if (started)
        {
            pos = new Vector3(
                0.0f,
                transform.position.y + (player.transform.position.y - transform.position.y) * 0.01f,
                -10);
        }
        else
        {
            pos = new Vector3(
                0.0f,
                transform.position.y + (155.5f - transform.position.y) * 0.01f,
                -10);

        }        
        transform.position = pos;
    }
}
