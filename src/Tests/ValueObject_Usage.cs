using Magic;
using Xunit;

namespace Tests
{
    partial class Money : IValueObject
    {
        string currency;
        decimal amount;
    }

    public class MoneyTest
    {
        [Fact]
        public void Create_money_value_object_assigns_properties()
        {
            var m = new Money("GBP", 12.34m);
            Assert.Equal(12.34m, m.Amount);
            Assert.Equal("GBP", m.Currency);
        }

        [Fact]
        public void Money_has_value_based_equality()
        {
            var m1 = new Money("GBP", 12.34m);
            var m2 = new Money("GBP", 12.34m);
            Assert.False(object.ReferenceEquals(m1, m2));
            Assert.True(m1.Equals(m2));
            Assert.True(m1.GetHashCode() == m2.GetHashCode());
        }

        [Fact]
        public void Money_has_equality_operator()
        {
            var m1 = new Money("GBP", 12.34m);
            var m2 = new Money("GBP", 12.34m);
            Assert.True(m1 == m2);
        }

        [Fact]
        public void Money_has_inequality_operator()
        {
            var m1 = new Money("GBP", 12.34m);
            var m2 = new Money("USD", 12.34m);
            Assert.True(m1 != m2);
        }

        [Fact]
        public void Two_null_money_variables_are_equal()
        {
            Money m1 = null, m2 = null;
            Assert.True(m1 == m2);
        }

        [Fact]
        public void Money_instance_not_null()
        {
            var m = new Money("GBP", 1m);
            Assert.False(m == null);
            Assert.False(null == m);
            Assert.True(null != m);
            Assert.True(m != null);
        }
    }
}
