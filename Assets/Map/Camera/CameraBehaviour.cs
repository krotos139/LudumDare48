using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
    public PlayerManager player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = new Vector3(
            transform.position.x + (player.transform.position.x - transform.position.x) * 0.01f,
            transform.position.y + (player.transform.position.y - transform.position.y) * 0.01f, 
            -10);
        transform.position = pos;
    }
}
