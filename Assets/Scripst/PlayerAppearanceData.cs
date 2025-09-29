
using Unity.Netcode;
using Unity.Collections;

public struct PlayerAppearanceData : INetworkSerializable, System.IEquatable<PlayerAppearanceData>
{
    // Guardamos los índices de las skins como un string separado por comas (ej: "1,4,2,0").
    public FixedString128Bytes selectedIndices;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref selectedIndices);
    }

    public bool Equals(PlayerAppearanceData other)
    {
        return selectedIndices.Equals(other.selectedIndices);
    }
}