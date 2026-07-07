using Unity.Netcode;

public struct NetworkPlayerState : INetworkSerializable, System.IEquatable<NetworkPlayerState>
{
    public ulong ClientId;
    public bool IsReady;
    public int Wins;
    public int Losses;

    // 네트워크를 통해 데이터를 주고받기 위한 직렬화/역직렬화 함수
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref Wins);
        serializer.SerializeValue(ref Losses);
    }

    // 두 상태가 같은지 비교하기 위한 함수
    public bool Equals(NetworkPlayerState other)
    {
        return ClientId == other.ClientId &&
               IsReady == other.IsReady &&
               Wins == other.Wins &&
               Losses == other.Losses;
    }
}
