namespace LegacyRenewalApp
{

    public interface ITaxCalculator
    {
        decimal CalculateTaxAmount(string country, decimal taxBase);
    }
}