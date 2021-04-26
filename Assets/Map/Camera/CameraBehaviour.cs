using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public PlayerManager player;
    private Camera cam;

    private bool fixedSize = false;

    // Start is called before the first frame update
    void Start()
    {
        
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
                    float halfWorldWidth = map.width / 2.0f;
                    cam.orthographicSize = halfWorldWidth / cam.aspect;
                    fixedSize = true;
                }
            }
        }
        Vector3 pos = new Vector3(
            //transform.position.x + (player.transform.position.x - transform.position.x) * 0.01f,
            0.0f,
            transform.position.y + (player.transform.position.y - transform.position.y) * 0.01f, 
            -10);
        transform.position = pos;
    }
}
