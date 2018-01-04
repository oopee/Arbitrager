namespace GDAXClient.Services.Orders
{
    public enum TimeInForce
    {
        /// <summary>
        /// Good till cancelled.
        /// </summary>
        GTC,

        /// <summary>
        /// Good till time. Requires cancel_after attribute to be set also.
        /// </summary>
        GTT,

        /// <summary>
        /// Immediate or cancel.
        /// </summary>
        IOC,

        /// <summary>
        /// Fill or kill.
        /// </summary>
        FOK
    }
}
