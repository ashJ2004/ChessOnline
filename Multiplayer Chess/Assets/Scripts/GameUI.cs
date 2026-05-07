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
    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyName;
    [SerializeField] private TMP_Dropdown lobbyTime;
    [SerializeField] private TMP_Dropdown lobbyTeam;
    [SerializeField] private Button ApplySettingsButton;
    [SerializeField] private TMP_Dropdown SkinIndex;
    [SerializeField] private TMP_Dropdown EnvironmentIndex;

    [System.Serializable]
    public class GameObjectArray
    {
        public GameObject[] items;
    }
    [System.Serializable]
    public class MaterialArray
    {
        public Material[] items;
    }
    [SerializeField] private GameObjectArray[] Prefabs;
    [SerializeField] private MaterialArray[] TeamColors;
    public Action<bool> SetLocalGame;
    public Action prematureLobbyDeletion;
    public Action<GameObject[],Material[]> ApplySettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
        server.Init(8004);
        client.Init("127.0.0.1", 8004);
        menuAnimator.SetTrigger("GameMenu");
        //set timer active
    }
    public void OnSettingsButton()
    {
        menuAnimator.SetTrigger("SettingsMenu");
    }
    public void ShowApplyButton()
    {
        ApplySettingsButton.gameObject.SetActive(true);
    }
    public void OnSettingsApplyButton()
    {
        
        //Take Each input field from each of the settings that have changed and apply them

        //Change Skin
        //Change Team Material and Prefabs within the ChessBoard Object
        Debug.Log("Reached Apply Call, attempting to submit a cosmetic change Request." + (Prefabs == null));
        ApplySettings?.Invoke(Prefabs[SkinIndex.value].items, TeamColors[SkinIndex.value].items);
        Debug.Log("Cosmetic Request sent.");


        //Change Environment

        //If More Settings Add logic Here

        ApplySettingsButton.gameObject.SetActive(false);
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
    public async void OnOnlineConnectButton()
    {
        Debug.Log("Attempting to Connect at: " + addressInput.text.ToUpper());
        SetLocalGame?.Invoke(false);
        if(addressInput.text == ""){
            OnConnectionFail();
            return;
        }
        menuAnimator.SetTrigger("GameMenu");
        await client.InitRelayClient(addressInput.text.ToUpper());
        
    }
    public void OnOnlineBackButton()
    {
        menuAnimator.SetTrigger("StartMenu");
    }
    public void OnOnlineCreateButton()
    {
        menuAnimator.SetTrigger("LobbyMenu");
    }

    public void OnHostBackButton()
    {
        menuAnimator.SetTrigger("OnlineMenu");
        prematureLobbyDeletion?.Invoke();

        DeleteLobby();
    }
    public void OnLobbyBackButton()
    {
        menuAnimator.SetTrigger("OnlineMenu");
    }
    public async void OnLobbyCreateButton()
    {
        createButton.interactable = false;
        
        string joinCode = await server.InitRelayHost(2,lobbyTime.value, (lobbyName.text == "")? "My Lobby": lobbyName.text, lobbyTeam.value);
        await client.InitRelayClient(joinCode, lobbyTime.value, lobbyTeam.value);
        joinText.text = joinCode;
        menuAnimator.SetTrigger("HostMenu");
        createButton.interactable = true;
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
