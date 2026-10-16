using System;

public sealed class ChangeMoveTypeDto : IWireDto
{
    public int ObjectId { get; private set; }
    public bool Running { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        if (reader.Remaining != 12)
            throw new FormatException("ChangeMoveType requires three integers.");
        ObjectId = reader.ReadI();
        int moveType = reader.ReadI();
        if (moveType != 0 && moveType != 1)
            throw new FormatException("Invalid ChangeMoveType gait.");
        Running = moveType == 1;
        reader.ReadI(); // Interlude C2 reserved field.
    }
}
