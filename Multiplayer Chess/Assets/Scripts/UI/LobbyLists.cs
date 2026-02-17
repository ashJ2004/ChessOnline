using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Core;
using Unity.Services.Authentication;

using System.Collections.Generic;

public class LobbyLists : MonoBehaviour
{
    [SerializeField] private Transform lobbyItemParent;
    [SerializeField] private LobbyItem lobbyItemPrefab;
    private bool isRefreshing;
    private bool isJoining;
    private void OnEnable()
    {
        
        Debug.Log("LOBBY LIST OPENED, REFRESHING LOBBY LIST");
        RefreshList();
    }
    public async void RefreshList()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        if(isRefreshing){ Debug.Log("Already Refreshing, exiting"); return;}
        isRefreshing = true;
        Debug.Log("Refresh List Function Activated");
        try
        {
            var options = new QueryLobbiesOptions();
            options.Count = 25;

            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"
                ),
                new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked,
                    op: QueryFilter.OpOptions.EQ,
                    value: "0"
                )
            };
            var lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log("Lobbies found during Refresh: " + lobbies.Results.Count);

            foreach(Transform child in lobbyItemParent)
            {
                Destroy(child.gameObject);
            }
            foreach(Lobby lobby in lobbies.Results)
            {
                var lobbyInstance = Instantiate(lobbyItemPrefab, lobbyItemParent);
                lobbyInstance.Initialize(this, lobby);

            }
                
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
            isRefreshing = false;
            throw;
        }
        
        Debug.Log("End Execution of Refresh Reached with no Exception");
        isRefreshing = false;
        
    }
    public async void JoinAsync(Lobby lobby)
    {
        if(isJoining == true) return;
        isJoining = true;
        try
        {
            var joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;

            await Client.Instance.InitRelayClient(joinCode);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
            isJoining = false;
            throw;
        }
        isJoining = false;
        
    }
}
