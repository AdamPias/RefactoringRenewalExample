namespace LegacyRenewalApp;

public interface IFeeCalculator
{

        FeeResult CalculateFees(string planCode, bool includePremiumSupport, string paymentMethod, decimal subtotalAfterDiscount);
    
}