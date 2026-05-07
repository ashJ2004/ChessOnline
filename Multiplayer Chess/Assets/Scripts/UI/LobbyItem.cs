using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text lobbyPlayerText;


    private Lobby lobby;
    private LobbyLists lobbiesList;

    public void Initialize(LobbyLists lobbiesList, Lobby lobby)
    {
        this.lobbiesList = lobbiesList;
        this.lobby = lobby;

        lobbyNameText.text = lobby.Name;
        lobbyPlayerText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
        
    }

    public void OnJoinButton()
    {
        GameUI.Instance.gameObject.transform.GetChild(8).gameObject.SetActive(false);
        lobbiesList.JoinAsync(lobby);
    }
    
}
