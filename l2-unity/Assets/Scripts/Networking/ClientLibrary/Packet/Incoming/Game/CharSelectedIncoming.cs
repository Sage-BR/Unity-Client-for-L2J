using UnityEngine;

[IncomingGamePacket(GameServerPacketType.CharSelected)]
public sealed class CharSelectedIncoming : IncomingWirePacket<CharSelectedDto>
{
    protected override void OnParsed(CharSelectedDto packet)
    {
        IncomingPacketActions.Game.PlayerInfo = packet.PlayeInfo;
        IncomingPacketActions.Game.SetDataPreparationCompleted(false);
    }

    public override void Apply(CharSelectedDto packet)
    {
        // Appearance and maxima are absent from CharSelected; read the UI only on Apply.
        var selection = CharSelectWindow.Instance.GetSelectChar();
        if (selection != null)
        {
            packet.PlayeInfo.Appearance.HairStyle = selection.HairStyle;
            packet.PlayeInfo.Appearance.HairColor = selection.HairColor;
            packet.PlayeInfo.Appearance.Face = selection.Face;
            packet.PlayeInfo.Stats.MaxHp = (int)selection.MaxHp;
            packet.PlayeInfo.Stats.MaxMp = (int)selection.MaxMp;
        }
        IncomingPacketActions.Game.PlayerInfo = packet.PlayeInfo;
        IncomingPacketActions.Manager.OnCharacterSelect();
    }
}
