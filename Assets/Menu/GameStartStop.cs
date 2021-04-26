using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStartStop : MonoBehaviour
{

    private bool fadeout = false;
    private bool fadein = false;

    public CanvasGroup canvasGroup;
    public CameraBehaviour camerab;
    public WaterManager water;
    public PlayerManager player;

    public void GameStart()
    {
        fadeout = true;
        camerab.startGame();
        water.startGame();
    }

    public void GameStop()
    {
        fadein = true;
        camerab.stopGame();
        water.stopGame();
    }

    public void GamePause()
    {
        //canvasGroup.enabled = true;
        fadein = true;
        camerab.stopGame();
        water.stopGame();
    }

    // Start is called before the first frame update
    void Start()
    {
        
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
            }
        }
        if (fadein)
        {
            canvasGroup.alpha = canvasGroup.alpha + 0.05f;
            if (canvasGroup.alpha >= 1.0f)
            {
                fadein = false;
            }
        }
    }
}
