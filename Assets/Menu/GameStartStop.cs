using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameStartStop : MonoBehaviour
{

    private bool fadeout = false;
    private bool fadein = false;

    public CanvasGroup canvasGroup;
    public CameraBehaviour camerab;
    public WaterManager water;
    public PlayerManager player;

    public Button startButton;
    public Button creditsButton;
    public Button quitButton;

    public CanvasGroup canvasgCredits;
    public Canvas canvasCredits;
    private bool fadeoutCredits = false;
    private bool fadeinCredits = false;

    public void GameStart()
    {
        fadeout = true;
        camerab.startGame();
        water.startGame();
        player.isDead = false;
        player.playGame();
    }

    public void GameStop()
    {
        fadein = true;
        camerab.stopGame();
        water.stopGame();
        player.playMenu();
    }

    public void GamePause()
    {
        //canvasGroup.enabled = true;
        fadein = true;
        camerab.stopGame();
        water.stopGame();
        player.playMenu();
    }

    public void CreditsStart()
    {
        canvasCredits.enabled = true;
        fadeinCredits = true;
    }

    public void CreditsStop()
    {
        fadeinCredits = false;
        fadeoutCredits = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (canvasCredits != null)
        {
             canvasCredits.enabled = false;
            canvasgCredits.alpha = 0.0f;
        }
    }

    public void setButtonsEnabled(bool value)
    {
        startButton.enabled = value;
        creditsButton.enabled = value;
        quitButton.enabled = value;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Escape) && !fadein)
        {
            GamePause();
        }
        if (fadeout)
        {
            canvasGroup.alpha = canvasGroup.alpha - 0.05f;
            if (canvasGroup.alpha <= 0.0f)
            {
                fadeout = false;
                setButtonsEnabled(false);
            }
        }
        if (fadein)
        {
            canvasGroup.alpha = canvasGroup.alpha + 0.05f;
            if (canvasGroup.alpha >= 1.0f)
            {
                fadein = false;
                setButtonsEnabled(true);
            }
        }
        if (canvasgCredits != null) {
            if (fadeoutCredits)
            {
                canvasgCredits.alpha = canvasgCredits.alpha - 1.0f*Time.deltaTime;
                if (canvasgCredits.alpha <= 0.0f)
                {
                    canvasCredits.enabled = false;
                    fadeoutCredits = false;
                   // setButtonsEnabled(false);
                }
            }
            if (fadeinCredits)
            {
                canvasgCredits.alpha = canvasgCredits.alpha + 1.0f * Time.deltaTime;
                if (canvasgCredits.alpha >= 1.0f)
                {
                    canvasCredits.enabled = true;
                    fadeinCredits = false;
                    //setButtonsEnabled(true);
                }
            }
        }
        if (canvasCredits.enabled && Input.GetMouseButtonDown(0))
        {
            CreditsStop();
        }
    }
}
