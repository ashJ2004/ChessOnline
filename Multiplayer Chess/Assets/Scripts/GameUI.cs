using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
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
    [SerializeField] private TMP_Text joinText;
    [SerializeField] private Button hostButton;

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
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimator.SetTrigger("GameMenu");
        //set timer active
    }
    public void OnOnlineGameButton()
    {
         menuAnimator.SetTrigger("OnlineMenu");
    }
    public async void OnOnlineHostButton()
    {
        hostButton.interactable = false;
        SetLocalGame?.Invoke(false);
        string joinCode = await server.InitRelayHost();
        client.InitRelayClient(joinCode);
        joinText.text = joinCode;
        menuAnimator.SetTrigger("HostMenu");
        hostButton.interactable = true;
    }
    public void OnOnlineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        client.InitRelayClient(addressInput.text);
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
        //set timer active
    }
}
