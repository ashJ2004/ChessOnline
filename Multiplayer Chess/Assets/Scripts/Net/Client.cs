using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class Client : MonoBehaviour
{
    public static Client Instance {set; get;}

    private void Awake()
    {
        Instance = this;
    }

    public NetworkDriver driver;
    public int gameTime;
    public int otherTeam;
    private NetworkConnection connection;

    private bool isActive = false;

    private Action connectionDropped;
    public Action connectionFailed;


    public void Init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(ip, port);

        connection = driver.Connect(endpoint);

        Debug.Log("Attempting to connect to server on " + endpoint.Address);
        
        gameTime = 600;

        isActive = true;


        RegisterToEvent();
         
    }

    public async Task InitRelayClient(string joinCode, int timeValue = 3, int team = 1)
    {

        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            var relayData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref relayData);

            driver = NetworkDriver.Create(settings);
            connection = driver.Connect(NetworkEndpoint.AnyIpv4);

            switch (timeValue)
            {
                case 0:
                    gameTime = 60;
                    break;
                case 1:
                    gameTime = 180;
                    break;
                case 2:
                    gameTime = 300;
                    break;
                case 3:
                    gameTime = 600;
                    break;
                case 4:
                    gameTime = 1800;
                    break;
                case 5:
                    gameTime = 3600;
                    break;
                default:
                    gameTime = 600;
                    break;
            }
            otherTeam = team;

            isActive = true;
            RegisterToEvent();
        }
        catch(Exception e)
        {
            connectionFailed?.Invoke();
        }
        
    }

    public void Shutdown()
    {
        if (!isActive) return;

        UnRegistertoEvent();

        if (connection.IsCreated)
            connection.Disconnect(driver);

        driver.Dispose();
        connection = default(NetworkConnection);
        isActive = false;
    }

    public void OnDestroy()
    {
        Shutdown();
    }

     public void Update()
    {
        if (!isActive)
        {
            return;
        }

        driver.ScheduleUpdate().Complete();
        CheckAlive();
        UpdateMessagePump();   
    }

    private void CheckAlive()
    {
        if(!connection.IsCreated && isActive)
        {
            Debug.Log("Something went wrong, lost connection to Server");
            connectionDropped?.Invoke();
            Shutdown();
        }
    }
    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                SendToServer(new NetWelcome());
                Debug.Log("Connected to Server, welcome to Chess!");
            }
            else if(cmd == NetworkEvent.Type.Data)
            {
                NetUtility.OnData(stream, connection);
            }
            else if(cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client Disconnected from server");
                connectionDropped?.Invoke();
                Shutdown();
            }
        }
    }


    public void SendToServer(NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    private void RegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnRegistertoEvent()
    {
        NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }
    private void OnKeepAlive(NetMessage msg)
    {
        SendToServer(msg);
    }
}
