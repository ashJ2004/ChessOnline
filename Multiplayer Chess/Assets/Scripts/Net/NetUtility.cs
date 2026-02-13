using UnityEngine;
using System;
using Unity.Networking.Transport;

public enum OpCode
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5,
    DRAW = 6,
    RESIGN = 7
}

public class NetUtility
{
    public static void OnData(Unity.Collections.DataStreamReader stream, NetworkConnection con, Server server = null)
    {
        NetMessage msg = null;
        var OpCode = (OpCode)stream.ReadByte();
        switch (OpCode)
        {
            case OpCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); break;
            case OpCode.WELCOME: msg = new NetWelcome(stream); break;
            case OpCode.START_GAME: msg = new NetStartGame(stream); break;
            case OpCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
            case OpCode.REMATCH: msg = new NetRematch(stream); break;
            case OpCode.DRAW: msg = new NetDraw(stream); break;
            case OpCode.RESIGN: msg = new NetResign(stream); break;
            default:
                Debug.LogError("Message receive has no assigned OpCode");
                break;
        }
        if(server != null)
        {
            msg.ReceivedOnServer(con);
        }
        else
        {
            msg.ReceivedOnClient();
        }
    }

    //net messages
    public static Action<NetMessage> C_KEEP_ALIVE;
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;
    public static Action<NetMessage> C_DRAW;
    public static Action<NetMessage> C_RESIGN;
    public static Action<NetMessage, NetworkConnection> S_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> S_WELCOME;
    public static Action<NetMessage, NetworkConnection> S_START_GAME;
    public static Action<NetMessage, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> S_REMATCH;
    public static Action<NetMessage, NetworkConnection> S_DRAW;
    public static Action<NetMessage, NetworkConnection> S_RESIGN;
}
