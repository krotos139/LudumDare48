using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CounterBehaviour : MonoBehaviour
{
    public CameraBehaviour globalCamera; // will stick to camera
    public ObjectPool pool;
    public GameObject delayer;

    private DelayHandler dh;

    List<GameObject> score = new List<GameObject>();
    List<GameObject> bestScore = new List<GameObject>();

    Tile[] numbers = new Tile[10]; // for loading
    bool cameraInited = false;
    float cameraVertSize = 0.0f;

    double bestScoreValue = 0.0;
    double lastSavedScore = 0.0;

    private void loadNumbersAssets()
    {
        string number_common_name = "numbers ";
        for (int i = 0; i < 10; i++)
        {
            numbers[i] = Resources.Load<Tile>(number_common_name + $"{i}");
        }
    }

    public void ShowNumber(double value, Vector2 pos, bool best = false, int alignment = 0, float depth = 9999.0f)
    {
        int curValue = (int)System.Math.Round(value);
        if (best)
        {
            ClearBestScore();
        }
        else
        {
            ClearScore();
        }
        bool atLeastOne = true;
        List<int> digitsList = new List<int>();
        while(atLeastOne || curValue > 0)
        {
            atLeastOne = false;
            int curDigit = curValue % 10;
            curValue /= 10;
            digitsList.Add(curDigit);
        }
        float xshift = 0.0f;
        switch(alignment)
        {
            case 0:
                // left
                break;
            case 1:
                // right
                xshift = -0.625f * digitsList.Count;
                break;
            case 2:
                // center
                xshift = -0.625f * digitsList.Count / 2.0f;
                break;
        }
        xshift += 0.625f / 2.0f;
        float xpos = pos.x; // 5.5f;
        float ypos = pos.y;
        for (int i = digitsList.Count - 1; i >= 0; i--)
        {
            GameObject digit = pool.GetFromPool();
            SpriteRenderer rend = digit.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                Color col = rend.color;
                col.a = 1.0f;
                rend.color = col;

                digit.SetActive(true);
                rend.sprite = numbers[digitsList[i]].sprite;
                if (depth > 9998.0f)
                {
                    digit.transform.position = new Vector3(xpos + xshift, ypos, digit.transform.position.z);
                }
                else
                {
                    digit.transform.position = new Vector3(xpos + xshift, ypos, depth);
                }
                xshift += 0.625f; // because size of digit sprite is 30x48, but cell is 48x48
            }
            if (best)
            {
                bestScore.Add(digit);
            }
            else
            {
                score.Add(digit);
            }
        }
    }

    public void ShowWaterBuried(Vector2 pos, int alignment = 0, float depth = 9999.0f)
    {
        double curVolume = globalCamera.player.water.waterVolume;
        if (curVolume > bestScoreValue)
        {
            bestScoreValue = curVolume;
            PlayerPrefs.SetString("bs", bestScoreValue.ToString());
        }
        ShowNumber(curVolume, pos, false, alignment, depth);
    }

    public void ShowWaterBuriedBest(Vector2 pos, int alignment = 0, float depth = 9999.0f)
    {
        ShowNumber(bestScoreValue, pos, true, alignment, depth);
        SavePrefs();
    }

    public void SetAlpha(float alpha)
    {
        foreach(GameObject go in score)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            Color col = sr.color;
            col.a = alpha;
            sr.color = col;
        }
    }

    public void SetAlphaBest(float alpha)
    {
        foreach (GameObject go in bestScore)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            Color col = sr.color;
            col.a = alpha;
            sr.color = col;
        }
    }

    public void createDigit(int value, Vector2Int position)
    {
        // pass
    }

    private void ClearScore()
    {
        foreach(GameObject go in score)
        {
            pool.RevertToPool(go);
        }
        score.Clear();
    }

    private void ClearBestScore()
    {
        foreach (GameObject go in bestScore)
        {
            pool.RevertToPool(go);
        }
        bestScore.Clear();
    }

    public void ClearAll()
    {
        //
        // save score somewhere
        //
        foreach (GameObject go in score)
        {
            pool.RevertToPool(go);
        }
        foreach (GameObject go in bestScore)
        {
            pool.RevertToPool(go);
        }
        score.Clear();
        bestScore.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        bestScoreValue = System.Convert.ToDouble(PlayerPrefs.GetString("bs", "0.0"));
        lastSavedScore = bestScoreValue;
        dh = delayer.GetComponent<DelayHandler>();
        dh.SetLimit(60.0f);

        loadNumbersAssets();
    }

    void SavePrefs()
    {
        double dif = System.Math.Abs(lastSavedScore - bestScoreValue);
        if (dif > 1e-5)
        {
            lastSavedScore = bestScoreValue;
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        if (!cameraInited)
        {
            Camera cam = globalCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cameraVertSize = cam.orthographicSize;
                cameraInited = true;
            }
        }

        if (dh.Finished())
        {
            SavePrefs();
        }
        else
        {
            if (!dh.Alive())
            {
                dh.StartTimer();
            }
        }

        if (globalCamera.player.water.isPlaying())
        {
            ShowWaterBuried(new Vector2(13.0f, globalCamera.transform.position.y + cameraVertSize - 1.5f));
        }
    }
}
