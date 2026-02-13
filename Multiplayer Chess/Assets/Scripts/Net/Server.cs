using UnityEngine;
using Unity.Collections;
using System;
using System.Threading.Tasks;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

public class Server : MonoBehaviour
{
    public static Server Instance {set; get;}

    private void Awake()
    {
        Instance = this;
    }
    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    private const float keepAliveTickRate = 20f;
    private float lastKeepAlive;

    private Action connectionDropped;


    public void Init(ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = port;

        if (driver.Bind(endpoint) != 0)
        {
            Debug.Log("Unable to Bind to Port " + endpoint.Port);
            return;
        }
        else
        {
            driver.Listen();
            Debug.Log("Currently Listening on Port " + endpoint.Port);
        }

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
        isActive = true;
    }
    public async Task<string> InitRelayHost(int maxConnections = 2)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Allocation allocation =
            await RelayService.Instance.CreateAllocationAsync(maxConnections);

        string joinCode =
            await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log($"Relay Join Code: {joinCode}");

        var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
        var settings = new NetworkSettings();
        settings.WithRelayParameters(ref relayData);

        driver = NetworkDriver.Create(settings);
        var endpoint = NetworkEndpoint.AnyIpv4;
        
        if (driver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind driver");
            return null;
        }

        if (driver.Listen() != 0)
        {
            Debug.LogError("Failed to listen");
            return null;
        }

        connections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
        isActive = true;
        return joinCode;
    }

    public void Shutdown()
    {
        if (isActive)
        {
            driver.Dispose();
            connections.Dispose();
            isActive = false;
        }
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
        KeepAlive();

        driver.ScheduleUpdate().Complete();
        CleanupConnections();
        AcceptNewConnections();
        UpdateMessagePump();   
    }

    private void CleanupConnections()
    {
        for(int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }
    private void AcceptNewConnections()
    {
        NetworkConnection c;
        while((c = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c);
        }
    }
    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        for(int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type cmd;
            while((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if(cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, connections[i], this);
                }
                else if(cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client Disconnected from server");
                    connections[i] = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    Shutdown();
                }
            }
        }
    }

    //Server Specific
    public void Broadcast(NetMessage msg)
    {
        for(int i = 0; i < connections.Length; i++)
        {
            if (connections[i].IsCreated)
            {
                //Debug.Log($"Sending{msg.Code} to : {connections[i].InternalId}");
                SendToClient(connections[i], msg);
            }
        }
    }
    public void SendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }

    private void KeepAlive()
    {
        if(Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }
}
