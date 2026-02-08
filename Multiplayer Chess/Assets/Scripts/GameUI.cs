using UnityEngine;
using System;
using TMPro;

public enum CamerAngle
{
    menu =0,
    white = 1,
    black = 2
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance {set; get;}
    public Server server;
    public Client client;


    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    private void Awake()
    {
        Instance = this;
        Application.runInBackground = true;

        RegisterEvents();
    }
    public void ChangeCamera(CamerAngle index)
    {
        for(int i = 0; i < cameraAngles.Length; i++)
        {
            cameraAngles[i].SetActive(false);
        }
        cameraAngles[(int)index].SetActive(true);
    }

    public void OnLocalGameButton()
    {
        SetLocalGame?.Invoke(true);
        server.Init(8010);
        client.Init("127.0.0.1", 8010);
        menuAnimator.SetTrigger("GameMenu");
    }
    public void OnOnlineGameButton()
    {
         menuAnimator.SetTrigger("OnlineMenu");
    }
    public void OnOnlineHostButton()
    {
        SetLocalGame?.Invoke(false);
        server.Init(8010);
        client.Init("127.0.0.1", 8010);
        menuAnimator.SetTrigger("HostMenu");
    }
    public void OnOnlineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        client.Init(addressInput.text, 8010);
        menuAnimator.SetTrigger("GameMenu");
    }
    public void OnOnlineBackButton()
    {
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton()
    {
        server.Shutdown();
        client.Shutdown();
        menuAnimator.SetTrigger("OnlineMenu");
    }
    public void OnLeaveFromGameMenu()
    {
        ChangeCamera(CamerAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }
    public void OnQuitButton()
    {
        Application.Quit();
    }

    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }
    private void UnRegisterEvent()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessage obj)
    {
        menuAnimator.SetTrigger("GameMenu");
    }
}
