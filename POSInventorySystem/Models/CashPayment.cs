namespace POSInventorySystem.Models;

public class CashPayment : Payment
{
public decimal CashReceived { get; private set; }

public decimal Change
{
    get
    {
        return CashReceived - Amount;
    }
}

public CashPayment(decimal amount, decimal cashReceived)
    : base(amount)
{
    CashReceived = cashReceived;
}

public override bool ProcessPayment()
{
    // Cash is accepted when it covers the amount due
    return CashReceived >= Amount;
}

}