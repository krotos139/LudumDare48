using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CounterBehaviour : MonoBehaviour
{
    public CameraBehaviour globalCamera; // will stick to camera
    public GameObject digitPrefab; // for instancing

    List<GameObject> digitsPool = new List<GameObject>();

    List<GameObject> score = new List<GameObject>();
    List<GameObject> bestScore = new List<GameObject>();

    Tile[] numbers = new Tile[10]; // for loading
    bool cameraInited = false;
    float cameraVertSize = 0.0f;

    private GameObject GetFromPool()
    {
        if (digitsPool.Count != 0)
        {
            GameObject go = digitsPool[0];
            digitsPool.RemoveAt(0);
            return go;
        }
        else
        {
            GameObject go = Instantiate(digitPrefab);
            return go;
        }
    }

    private void RevertToPool(GameObject go)
    {
        go.SetActive(false);
        digitsPool.Add(go);
    }

    private void loadNumbersAssets()
    {
        string number_common_name = "numbers ";
        for (int i = 0; i < 10; i++)
        {
            numbers[i] = Resources.Load<Tile>(number_common_name + $"{i}");
        }
    }

    public void ShowNumber(float value, Vector2 pos)
    {
        int curValue = (int)Mathf.Round(value);
        ClearScore();
        bool atLeastOne = true;
        List<int> digitsList = new List<int>();
        while(atLeastOne || curValue > 0)
        {
            atLeastOne = false;
            int curDigit = curValue % 10;
            curValue /= 10;
            digitsList.Add(curDigit);
        }
        float xpos = pos.x; // 5.5f;
        float ypos = globalCamera.transform.position.y + pos.y;
        for(int i = digitsList.Count - 1; i >= 0; i--)
        {
            GameObject digit = GetFromPool();
            SpriteRenderer rend = digit.GetComponent<SpriteRenderer>();
            if (rend != null)
            {
                digit.SetActive(true);
                rend.sprite = numbers[digitsList[i]].sprite;
                digit.transform.position = new Vector3(xpos, ypos, digit.transform.position.z);
                xpos += 0.625f; // because size of digit sprite is 30x48, but cell is 48x48
            }
            score.Add(digit);
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
            RevertToPool(go);
        }
        score.Clear();
    }

    public void ClearAll()
    {
        //
        // save score somewhere
        //
        foreach (GameObject go in score)
        {
            RevertToPool(go);
        }
        foreach (GameObject go in bestScore)
        {
            RevertToPool(go);
        }
        score.Clear();
        bestScore.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        loadNumbersAssets();
    }

    // Update is called once per frame
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
        if (!globalCamera.player.water.isPlaying())
        {
            // something outside of gameplay
        }
        else
        {
            ShowNumber(globalCamera.player.water.waterVolume, new Vector2(13.0f, cameraVertSize - 1.5f));
        }
    }
}
