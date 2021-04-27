using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    bool fadedOut = false;
    float fadeOutAlpha = 1f;

    bool clicked = false;


    float delay = 0.0f;


    void Start()
    {
        canvas.enabled = false;
        youdiedImage.color = new Color(117f, 171f, 134f, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerManager.isDead) return;

        if (delay < 500.0f)
        {
            delay++;
            return;
        }
        // enable canvas
        if (!canvas.enabled) canvas.enabled = true;

        // background fade-in
        if (!isBackFaded)
        {
            backAlpha += Time.deltaTime * fadeSpeed;
            backImage.color = new Color(0f, 0f, 0f, backAlpha);

            if (backAlpha >= 1f) isBackFaded = true;

            return;
        }

        if (!fadedOut) gameStartStop.GameStop();


        // "You died" fade-in
        if (isBackFaded && !isYoudiedFaded)
        {
            youdiedAlpha += Time.deltaTime * fadeSpeed;
            youdiedImage.color = new Color(117f, 171f, 134f, youdiedAlpha);

            if (youdiedAlpha >= 1f) isYoudiedFaded = true;
        }

        //// "Waste biried" fade-in
        //if (isYoudiedFaded && !isWasteFaded)
        //{
        //    wasteAlpha += Time.deltaTime * fadeSpeed;
        //    wasteImage.color = new Color(117f, 171f, 134f, wasteAlpha);

        //    if (wasteAlpha >= 1f)
        //    {
        //        isWasteFaded = true;
        //    }
        //}

        if (isBackFaded && isYoudiedFaded && Input.GetMouseButtonDown(0)) clicked = true;

        if (clicked)
        {
            fadeOutAlpha -= Time.deltaTime * fadeSpeed;

            backImage.color = new Color(0f, 0f, 0f, fadeOutAlpha);
            youdiedImage.color = new Color(117f, 171f, 134f, fadeOutAlpha);

            if (fadeOutAlpha <= 0f) fadedOut = true;
        }

        if (fadedOut && clicked) SceneManager.LoadScene("SampleScene");
    }

}
