using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathHandler : MonoBehaviour
{

    public Canvas canvas;
    public PlayerManager playerManager;
    public GameStartStop gameStartStop;

    public float fadeSpeed = 0.1f;

    public Image backImage;
    bool isBackFaded = false;
    float backAlpha = 0f;

    public Image youdiedImage;
    bool isYoudiedFaded = false;
    float youdiedAlpha = 0f;

    public Image wasteImage;
    bool isWasteFaded = false;
    float wasteAlpha = 0f;


    void Start()
    {
        canvas.enabled = false;
        youdiedImage.color = new Color(117f, 171f, 134f, 0);
        wasteImage.color = new Color(117f, 171f, 134f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerManager.isDead) return;

        //gameStartStop.GameStop();

        // enable canvas
        if (!canvas.enabled) canvas.enabled = true;

        // background fade-in
        if (!isBackFaded)
        {
            backAlpha += Time.deltaTime * fadeSpeed;
            backImage.color = new Color(0f, 0f, 0f, backAlpha);

            if (backAlpha >= 1f) isBackFaded = true;
        }

        // "You died" fade-in
        if (isBackFaded && !isYoudiedFaded)
        {
            youdiedAlpha += Time.deltaTime * fadeSpeed;
            youdiedImage.color = new Color(117f, 171f, 134f, youdiedAlpha);

            if (youdiedAlpha >= 1f) isYoudiedFaded = true;
        }

        // "Waste biried" fade-in
        if (isYoudiedFaded && !isWasteFaded)
        {
            wasteAlpha += Time.deltaTime * fadeSpeed;
            wasteImage.color = new Color(117f, 171f, 134f, wasteAlpha);

            if (wasteAlpha >= 1f) isWasteFaded = true;
        }
    }

}
