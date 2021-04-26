using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wastedFader : MonoBehaviour
{

    public YoudiedFader youdiedFader;
    public bool faded;
    public float speed;

    public SpriteRenderer wasted;

    float alpha = 0;


    // Start is called before the first frame update
    void Start()
    {
        wasted.color = new Color(117f, 131f, 174f, alpha);
        faded = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (youdiedFader.faded)
        {
            if (!faded && alpha < 1.0f)
            {
                alpha += Time.deltaTime * speed;
                wasted.color = new Color(117f, 131f, 174f, alpha);

                if (alpha > 1.0f) faded = true;
            }
        }
    }
}
