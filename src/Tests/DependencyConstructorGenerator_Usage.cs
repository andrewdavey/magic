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

    partial class MoreComplexExample : IDependOn<IEmailService>
    {
        // Magic will generate new public constructors that call these
        // private constructors, but adding the extra dependency parameters.
        private MoreComplexExample(int value)
        {
            this.value = value;
        }

        private MoreComplexExample(string foo, string bar)
        {
        }

        int value;

        static void Test()
        {
            new MoreComplexExample(1, default(IEmailService));
            new MoreComplexExample("foo", "bar", default(IEmailService));
        }
    }
}

namespace NamespaceTests
{
    using AnotherNamespace;

    partial class RefAnotherNamespace : IDependOn<IBaz>
    {
    }
}

namespace AnotherNamespace
{
    interface IBaz { }
}