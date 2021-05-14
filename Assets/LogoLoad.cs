using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoLoad : MonoBehaviour
{
    private bool ready = false;
    public float prevTime = 0.0f;
    CanvasGroup cv;
    public bool fadeIn = true;
    public bool fadeOut = false;

    // Start is called before the first frame update
    void Start()
    {
        prevTime = Time.time;
        cv = GetComponent<CanvasGroup>();
        if (cv != null)
        {
            cv.alpha = 0.0f;
        }
        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return null;

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("SampleScene");
        asyncOperation.allowSceneActivation = false;
        Debug.Log("Pro :" + asyncOperation.progress);
        while (!asyncOperation.isDone)
        {
            //m_Text.text = "Loading progress: " + (asyncOperation.progress * 100) + "%";
            if (asyncOperation.progress >= 0.9f && !fadeOut && !fadeIn)
            {
                //m_Text.text = "Press the space bar to continue";
                //if (Input.GetKeyDown(KeyCode.Space))
                //    asyncOperation.allowSceneActivation = true;
                fadeOut = true;
                prevTime = Time.time;
            }
            if (ready)
            {
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.time - prevTime;

        //if (dt > 2.0f && Input.GetMouseButtonDown(0))
        //{
        //    SceneManager.LoadScene("SampleScene");
        //}

        if (cv != null)
        {
            if (fadeIn)
            {
                if (dt > 1.0f)
                {
                    dt = 1.0f;
                    fadeIn = false;
                    prevTime = Time.time;
                }
                cv.alpha = dt;
            }
            //if ((!fadeIn) && (!fadeOut) && dt > 1.0f)
            //{
            //    fadeOut = true;
            //    prevTime = Time.time;
            //    dt = 0.0f;
            //}
            if (fadeOut)
            {
                if (dt > 1.0f)
                {
                    dt = 1.0f;
                    fadeOut = false;
                    prevTime = Time.time;
                    //SceneManager.LoadScene("SampleScene");
                    ready = true;
                    
                }
                cv.alpha = 1.0f - dt;
            }
        }
    }
}
