namespace Restaurant.DataAccess;

// Written in the business transaction so a lost commit acknowledgement can be verified.
internal sealed class TransactionReceipt
{
    public Guid Id { get; set; }
}
