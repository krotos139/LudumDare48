using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathHandler : MonoBehaviour
{
    public CameraBehaviour globalCamera;
    public CounterBehaviour globalCounter;

    public GameStartStop gameStartStop;

    public ObjectPool delayPool;

    public float idleTime = 1.25f;
    public float fadeTime = 0.5f;
    public float youDiedAppear = 0.5f;
    public float scoreAppear = 0.5f;
    public float keyUnlock = 0.5f;
    public float menuAppear = 0.5f;

    public SpriteRenderer bgRend;
    public SpriteRenderer youDiedRend;
    public SpriteRenderer wasteBuriedRend;
    public SpriteRenderer wasteBuriedBestRend;

    private GameObject idleDelayObj;
    private GameObject fadeDelayObj;
    private GameObject youDiedDelayObj;
    private GameObject scoreAppearDelayObj;
    private GameObject keyUnlockDelayObj;
    private GameObject menuAppearDelayObj;

    private DelayHandler idleDelayObjDH;
    private DelayHandler fadeDelayObjDH;
    private DelayHandler youDiedDelayObjDH;
    private DelayHandler scoreAppearDelayObjDH;
    private DelayHandler keyUnlockDelayObjDH;
    private DelayHandler menuAppearDelayObjDH;

    bool init = false;

    void DisableRenders()
    {
        // make all images transparent

        Color col = bgRend.color;
        col.a = 0.0f;
        bgRend.color = col;

        col = youDiedRend.color;
        col.a = 0.0f;
        youDiedRend.color = col;

        col = wasteBuriedRend.color;
        col.a = 0.0f;
        wasteBuriedRend.color = col;

        col = wasteBuriedBestRend.color;
        col.a = 0.0f;
        wasteBuriedBestRend.color = col;

        globalCounter.SetAlpha(0.0f);
        globalCounter.SetAlphaBest(0.0f);
    }

    void Init()
    {
        DisableRenders();
        // set delays options
        // actions will be set later

        idleDelayObj = delayPool.GetFromPool();
        idleDelayObjDH = idleDelayObj.GetComponent<DelayHandler>();
        idleDelayObjDH.SetLimit(idleTime);

        fadeDelayObj = delayPool.GetFromPool();
        fadeDelayObjDH = fadeDelayObj.GetComponent<DelayHandler>();
        fadeDelayObjDH.SetLimit(fadeTime);

        youDiedDelayObj = delayPool.GetFromPool();
        youDiedDelayObjDH = youDiedDelayObj.GetComponent<DelayHandler>();
        youDiedDelayObjDH.SetLimit(youDiedAppear);

        scoreAppearDelayObj = delayPool.GetFromPool();
        scoreAppearDelayObjDH = scoreAppearDelayObj.GetComponent<DelayHandler>();
        scoreAppearDelayObjDH.SetLimit(scoreAppear);

        keyUnlockDelayObj = delayPool.GetFromPool();
        keyUnlockDelayObjDH = keyUnlockDelayObj.GetComponent<DelayHandler>();
        keyUnlockDelayObjDH.SetLimit(keyUnlock);

        menuAppearDelayObj = delayPool.GetFromPool();
        menuAppearDelayObjDH = menuAppearDelayObj.GetComponent<DelayHandler>();
        menuAppearDelayObjDH.SetLimit(menuAppear);
    }

    void Start()
    {
        if (!init)
        {
            init = true;
            Init();
        }
    }

    private void ShowBackgroundWithAlpha(float alpha)
    {
        // complete varies from 0.0f to 1.0f
        Vector3 cameraPos = globalCamera.transform.position;
        Vector3 rendPos = bgRend.transform.position;
        bgRend.transform.position = new Vector3(cameraPos.x, cameraPos.y, rendPos.z);
        Color col = bgRend.color;
        col.a = alpha;
        bgRend.color = col;
    }

    private void FadeInUpdate(float complete)
    {
        ShowBackgroundWithAlpha(complete);
    }

    private void ShowYouDiedWithAlpha(float alpha)
    {
        Vector3 cameraPos = globalCamera.transform.position;
        Vector3 rendPos = youDiedRend.transform.position;
        youDiedRend.transform.position = new Vector3(cameraPos.x, cameraPos.y + 2.0f, rendPos.z);

        Color col = youDiedRend.color;
        col.a = alpha;
        youDiedRend.color = col;
    }

    private void YouDiedShowUpdate(float complete)
    {
        ShowBackgroundWithAlpha(1.0f);
        float curAlpha = complete * 2.0f;
        ShowYouDiedWithAlpha(curAlpha);
    }

    private void ShowScoresWithAlpha(float alpha)
    {
        Vector3 cameraPos = globalCamera.transform.position;
        Vector3 rendPos = wasteBuriedRend.transform.position;

        wasteBuriedRend.transform.position = new Vector3(cameraPos.x, cameraPos.y, rendPos.z);
        Color col = wasteBuriedRend.color;
        col.a = alpha;
        wasteBuriedRend.color = col;

        globalCounter.ShowWaterBuried(new Vector2(cameraPos.x, cameraPos.y - 1.0f), 2, rendPos.z - 0.05f);
        globalCounter.SetAlpha(alpha);

        wasteBuriedBestRend.transform.position = new Vector3(cameraPos.x, cameraPos.y - 2.0f, rendPos.z);
        col = wasteBuriedBestRend.color;
        col.a = alpha;
        wasteBuriedBestRend.color = col;

        globalCounter.ShowWaterBuriedBest(new Vector2(cameraPos.x, cameraPos.y - 3.0f), 2, rendPos.z - 0.05f);
        globalCounter.SetAlphaBest(alpha);
    }

    private void ScoreShowUpdate(float complete)
    {
        ShowBackgroundWithAlpha(1.0f);
        ShowYouDiedWithAlpha(1.0f);
        ShowScoresWithAlpha(complete);
    }

    void BackToMenuUpdate(float complete)
    {
        float inverseAlpha = 1.0f - complete;

        ShowBackgroundWithAlpha(inverseAlpha);
        ShowYouDiedWithAlpha(inverseAlpha * 0.5f);
        ShowScoresWithAlpha(inverseAlpha * 0.5f);

        gameStartStop.MenuAppearanceWithAlpha(complete);
    }

    void BackToMenuEnd()
    {
        // finally

        SceneManager.LoadScene("SampleScene");
        DisableRenders();
    }

    void Update()
    {
        if (!init)
        {
            init = true;
            Init();
        }

        if (!globalCamera.player.isDead) return;

        globalCamera.startWaiting();

        if (!idleDelayObjDH.Finished())
        {
            if (!idleDelayObjDH.Alive())
            {
                idleDelayObjDH.StartTimer(null, null, null); // just wait for a while
            }
        }
        else
        {
            gameStartStop.GameStop(); // freeze water

            if (!fadeDelayObjDH.Finished())
            {
                if (!fadeDelayObjDH.Alive())
                {
                    fadeDelayObjDH.StartTimer(null, FadeInUpdate, null); // start fading
                }
            }
            else
            {
                // fading is finished

                globalCamera.stopWaiting();
                globalCamera.ReturnToStart();

                if (!youDiedDelayObjDH.Finished())
                {
                    if (!youDiedDelayObjDH.Alive())
                    {
                        youDiedDelayObjDH.StartTimer(null, YouDiedShowUpdate, null); // start "You died" message unfading
                    }
                }
                else
                {
                    // "You died" was shown

                    // start "Waste buried" and score unfading

                    if (!scoreAppearDelayObjDH.Finished())
                    {
                        if (!scoreAppearDelayObjDH.Alive())
                        {
                            scoreAppearDelayObjDH.StartTimer(null, ScoreShowUpdate, null);
                        }
                    }
                    else
                    {
                        // time for score showing, keys are locked for some time, nothing happens

                        if (!keyUnlockDelayObjDH.Finished())
                        {
                            if (!keyUnlockDelayObjDH.Alive())
                            {
                                keyUnlockDelayObjDH.StartTimer(null, null, null);
                            }
                        }

                        else
                        {
                            bool clicked = Input.GetMouseButtonDown(0);

                            if (clicked)
                            {
                                globalCamera.player.playMenu();

                                // go back to menu

                                if (!menuAppearDelayObjDH.Finished())
                                {
                                    if (!menuAppearDelayObjDH.Alive())
                                    {
                                        menuAppearDelayObjDH.StartTimer(null, BackToMenuUpdate, BackToMenuEnd);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
