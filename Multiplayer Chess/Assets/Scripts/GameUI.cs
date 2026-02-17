using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using TMPro;

using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;



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
    [SerializeField] private TMP_Text invalidCodeText;
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
        server.Init(8003);
        client.Init("127.0.0.1", 8003);
        menuAnimator.SetTrigger("GameMenu");
        //set timer active
    }
    public void OnOnlineGameButton()
    {
         menuAnimator.SetTrigger("OnlineMenu");
         SetLocalGame?.Invoke(false);
    }
    public async void OnOnlineHostButton()
    {
        hostButton.interactable = false;
        string joinCode = await server.InitRelayHost();
        await client.InitRelayClient(joinCode);
        joinText.text = joinCode;
        menuAnimator.SetTrigger("HostMenu");
        hostButton.interactable = true;
    }
    public async Task OnOnlineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        if(addressInput.text == ""){
            OnConnectionFail();
            return;
        }
        await client.InitRelayClient(addressInput.text.ToUpper());
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

        DeleteLobby();

        menuAnimator.SetTrigger("OnlineMenu");


    }
    private async void DeleteLobby()
    {
        if (!string.IsNullOrEmpty(server.lobbyID))
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(server.lobbyID);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public void OnLeaveFromGameMenu()
    {
        addressInput.text = "";
        ChangeCamera(CamerAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }
    public void OnQuitButton()
    {
        Application.Quit();
    }
    private void OnConnectionFail()
    {
        menuAnimator.SetTrigger("OnlineMenu");
        StartCoroutine(DisplayInvalidCode());

    }

    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;

        client.connectionFailed += OnConnectionFail;
    }
    private void UnRegisterEvent()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;

        client.connectionFailed -= OnConnectionFail;
    }
    private void OnStartGameClient(NetMessage obj)
    {
        menuAnimator.SetTrigger("GameMenu");
        //set timer active
    }
    IEnumerator DisplayInvalidCode()
    {
        invalidCodeText.gameObject.SetActive(true);
        invalidCodeText.text = "Join Code \""+ addressInput.text.ToUpper() + "\" does not exist in current available session, try to join from lobby menu or try a different join code";
        
        yield return new WaitForSeconds(5f);
        invalidCodeText.gameObject.SetActive(false);
    }
    
}
