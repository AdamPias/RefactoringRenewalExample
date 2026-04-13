namespace LegacyRenewalApp;

public interface IRenewalRequestValidator
{
    void Validate(int customerId, string planCode, int seatCount, string paymentMethod);
}