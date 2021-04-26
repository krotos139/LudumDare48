using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YoudiedFader : MonoBehaviour
{

    public bool faded;
    float alpha = 0;
    public float speed = 0.1f;

    public SpriteRenderer youDied;

    // Start is called before the first frame update
    void Start()
    {
        youDied.color = new Color(117f, 131f, 174f, alpha);
        faded = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!faded && alpha < 1.0f)
        {
            alpha += Time.deltaTime * speed;
            youDied.color = new Color(117f, 131f, 174f, alpha);

            if (alpha > 1.0f) faded = true;
        }
    }
}
