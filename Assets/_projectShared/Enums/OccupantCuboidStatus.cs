namespace KitchenDesign.Shared.Enums
{
    public enum OccupantCuboidStatus
    {
        Fits = 0,
        CannotContainProduct = 1,
        OverlapsAnotherOccupant = 2,
        OverlapsBacking = 3,
        TooFarLeft = 4,
        TooFarRight = 5,
        TooFarDown = 6,
        TooFarUp = 7,
        TooFarBack = 8,
        TooFarForward = 9,
        CannotResizeCabinet = 10,
        NotCompatible = 11,
        DataError = 12,
        InstantiateError = 13
    }
}
