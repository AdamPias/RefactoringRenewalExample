using System;

namespace LegacyRenewalApp
{
    
    public class BillingGatewayAdapter : IBillingGateway
    {
        public void SaveInvoice(RenewalInvoice invoice)
        {
            LegacyBillingGateway.SaveInvoice(invoice);
        }

        public void SendEmail(string email, string subject, string body)
        {
            LegacyBillingGateway.SendEmail(email, subject, body);
        }
    }
    
    public class SubscriptionRenewalService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IBillingGateway _billingGateway;
        private readonly IRenewalRequestValidator _validator;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IFeeCalculator _feeCalculator;
        
        public SubscriptionRenewalService() 
            : this(new CustomerRepository(), new SubscriptionPlanRepository(), 
                new BillingGatewayAdapter(), new RenewalRequestValidator(), 
                new StandardDiscountCalculator(), new RegionalTaxCalculator(),
                new StandardFeeCalculator())
        {
        }
        
        public SubscriptionRenewalService(
            ICustomerRepository customerRepository,
            ISubscriptionPlanRepository planRepository,
            IBillingGateway billingGateway,
            IRenewalRequestValidator validator,
            IDiscountCalculator discountCalculator,
            ITaxCalculator taxCalculator,
            IFeeCalculator feeCalculator)
        {
            _customerRepository = customerRepository;
            _planRepository = planRepository;
            _billingGateway = billingGateway;
            _validator = validator;
            _discountCalculator = discountCalculator;
            _taxCalculator = taxCalculator;
            _feeCalculator = feeCalculator;
        }
        
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            
            _validator.Validate(customerId, planCode, seatCount, paymentMethod);


            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();


            var customer = _customerRepository.GetById(customerId);
            var plan = _planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            
            var discountResult = _discountCalculator.CalculateDiscount(customer, plan, seatCount, baseAmount, useLoyaltyPoints);
            
            decimal discountAmount = discountResult.Amount;
            string notes = discountResult.Notes;

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }
            
            var feeResult = _feeCalculator.CalculateFees(normalizedPlanCode, includePremiumSupport, normalizedPaymentMethod, subtotalAfterDiscount);
            notes += feeResult.Notes;
            
            decimal taxBase = subtotalAfterDiscount + feeResult.SupportFee + feeResult.PaymentFee;
            
            decimal taxAmount = _taxCalculator.CalculateTaxAmount(customer.Country, taxBase);
    
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                
                SupportFee = Math.Round(feeResult.SupportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(feeResult.PaymentFee, 2, MidpointRounding.AwayFromZero),
                
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            LegacyBillingGateway.SaveInvoice(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                LegacyBillingGateway.SendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
    }
}
