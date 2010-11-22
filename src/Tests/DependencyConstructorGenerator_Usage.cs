using System;
using Magic;

namespace Tests
{
    interface IEmailService
    {
        void SendEmail(string from, string to, string subject, string message);
    }

    interface ICustomerRepository
    {
        object GetCustomer(int id);
        void SaveCustomer(int id, object customer);
    }

    partial class CustomerController : IDependOn<ICustomerRepository, IEmailService>
    {
        // Look Ma, no boilerplate constructor and fields!
        // Thanks Magic!

        public void Test()
        {
            var customer = customerRepository.GetCustomer(1);
            emailService.SendEmail("a@test.com", customer + "@test.com", "test", "hello world");
            customerRepository.SaveCustomer(1, customer);
        }
    }
}
