using System;
namespace LegacyRenewalApp{

public class StandardFeeCalculator: IFeeCalculator
{
    public FeeResult CalculateFees(string planCode, bool includePremiumSupport, string paymentMethod, decimal subtotalAfterDiscount)
    {
        decimal supportFee = 0m;
        string notes = string.Empty;
        string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
        string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

        if (includePremiumSupport)
        {
            if (normalizedPlanCode == "START") supportFee = 250m;
            else if (normalizedPlanCode == "PRO") supportFee = 400m;
            else if (normalizedPlanCode == "ENTERPRISE") supportFee = 700m;

            notes += "premium support included; ";
        }

        decimal paymentFee = 0m;
        if (normalizedPaymentMethod == "CARD")
        {
            paymentFee = (subtotalAfterDiscount + supportFee) * 0.02m;
            notes += "card payment fee; ";
        }
        else if (normalizedPaymentMethod == "BANK_TRANSFER")
        {
            paymentFee = (subtotalAfterDiscount + supportFee) * 0.01m;
            notes += "bank transfer fee; ";
        }
        else if (normalizedPaymentMethod == "PAYPAL")
        {
            paymentFee = (subtotalAfterDiscount + supportFee) * 0.035m;
            notes += "paypal fee; ";
        }
        else if (normalizedPaymentMethod == "INVOICE")
        {
            paymentFee = 0m;
            notes += "invoice payment; ";
        }
        else
        {
            throw new ArgumentException("Unsupported payment method");
        }

        return new FeeResult { SupportFee = supportFee, PaymentFee = paymentFee, Notes = notes };
    }
}
}