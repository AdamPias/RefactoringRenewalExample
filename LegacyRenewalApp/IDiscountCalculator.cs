namespace LegacyRenewalApp
{

    public interface IDiscountCalculator
    {
        DiscountResult CalculateDiscount(
            Customer customer, 
            SubscriptionPlan plan, 
            int seatCount, 
            decimal baseAmount, 
            bool useLoyaltyPoints);
    }
}