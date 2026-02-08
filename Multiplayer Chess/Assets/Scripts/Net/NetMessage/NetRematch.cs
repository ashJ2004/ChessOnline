using Unity.Networking.Transport;
public class NetRematch : NetMessage
{
    public int teamID;
    public byte wantRematch;
    public NetRematch()
    {
        Code = OpCode.REMATCH;
    }
    public NetRematch(Unity.Collections.DataStreamReader reader)
    {
        Code = OpCode.REMATCH;
        Deserialize(reader);
    }

    public override void Serialize(ref Unity.Collections.DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(teamID);
        writer.WriteByte(wantRematch);
    }
    public override void Deserialize(Unity.Collections.DataStreamReader reader)
    {
        teamID = reader.ReadInt();
        wantRematch = reader.ReadByte();
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection con)
    {
        NetUtility.S_REMATCH?.Invoke(this, con);
    }
}
