[IncomingGamePacket(GameServerPacketType.ChangeMoveType)]
public sealed class ChangeMoveTypeIncoming : IncomingWirePacket<ChangeMoveTypeDto>
{
    public override void Apply(ChangeMoveTypeDto packet)
    {
        IncomingPacketActions.ApplyWorld(apply => apply.ChangeMoveType(packet));
    }
}
