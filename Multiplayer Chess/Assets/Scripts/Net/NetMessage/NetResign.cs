using Unity.Networking.Transport;
public class NetResign : NetMessage
{
    public int resigningTeam;
    public NetResign()
    {
        Code = OpCode.RESIGN;
    }
    public NetResign(Unity.Collections.DataStreamReader reader)
    {
        Code = OpCode.RESIGN;
        Deserialize(reader);
    }

    public override void Serialize(ref Unity.Collections.DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(resigningTeam);
    }
    public override void Deserialize(Unity.Collections.DataStreamReader reader)
    {
        resigningTeam = reader.ReadInt();
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_RESIGN?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection con)
    {
        NetUtility.S_RESIGN?.Invoke(this, con);
    }
}
