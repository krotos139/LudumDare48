using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameStartStop gameStartStop;
    public Image btn_image;
    public AudioSource on_hover;

    public void onStart()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (gameStartStop != null)
        {
            gameStartStop.GameStart();
        }
    }

    public void onCredits()
    {

    }

    public void onQuit()
    {
    #if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
    // Start is called before the first frame update
    void Start()
    {
        btn_image.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        btn_image.enabled = true;
        if (on_hover != null)
        {
            on_hover.Play();
        }
        //Debug.Log("Mouse enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        btn_image.enabled = false;
        //Debug.Log("Mouse exit");
    }
}
