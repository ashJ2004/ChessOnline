using Unity.Networking.Transport;
public class NetDraw : NetMessage
{
    public int teamID;
    public byte wantDraw;
    public NetDraw()
    {
        Code = OpCode.DRAW;
    }
    public NetDraw(Unity.Collections.DataStreamReader reader)
    {
        Code = OpCode.DRAW;
        Deserialize(reader);
    }

    public override void Serialize(ref Unity.Collections.DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(teamID);
        writer.WriteByte(wantDraw);
    }
    public override void Deserialize(Unity.Collections.DataStreamReader reader)
    {
        teamID = reader.ReadInt();
        wantDraw = reader.ReadByte();
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_DRAW?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection con)
    {
        NetUtility.S_DRAW?.Invoke(this, con);
    }
}