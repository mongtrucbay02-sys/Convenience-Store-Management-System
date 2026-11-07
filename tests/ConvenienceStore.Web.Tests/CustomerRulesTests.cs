using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ConvenienceStore.Web.Controllers;
using ConvenienceStore.Web.Modules.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Tests;

public sealed class CustomerRulesTests
{
    [Theory]
    [InlineData("0901234123", "090****123")]
    [InlineData(null, "")]
    [InlineData("123", "")]
    public void CashierList_MasksPhone(string? phone, string expected) => Assert.Equal(expected, CustomerRules.MaskPhone(phone));

    [Theory]
    [InlineData(9999, null, true, 0)]
    [InlineData(29999, null, true, 2)]
    [InlineData(50000, 20000, true, 2)]
    [InlineData(50000, 10000, false, 0)]
    public void Loyalty_UsesDefaultFloorAndEffectiveRule(long paid, int? rate, bool enabled, int expected) =>
        Assert.Equal(expected, CustomerRules.EarnedPoints(paid, rate, enabled));

    [Fact]
    public void Adjust_RejectsNegativeZeroAndOverflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomerRules.AdjustedBalance(2, -3));
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomerRules.AdjustedBalance(2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomerRules.AdjustedBalance(uint.MaxValue, 1));
        Assert.Equal(0u, CustomerRules.AdjustedBalance(2, -2));
    }

    [Theory]
    [InlineData("+84901234123", false)]
    [InlineData("0901234123", true)]
    [InlineData("1901234123", false)]
    [InlineData("090123412", false)]
    public void CustomerPhone_RequiresTenAsciiDigitsStartingWithZero(string phone, bool valid)
    {
        var input = new CustomerInput { Name = "Khách test", Phone = phone };
        Assert.Equal(valid, Validator.TryValidateObject(input, new(input), [], true));
    }

    [Theory]
    [InlineData("Delete", "AD")]
    [InlineData("Adjust", "AD")]
    [InlineData("Attach", "TN")]
    public void SensitiveActions_RequireRoleAndCsrf(string action, string role)
    {
        var method = typeof(CustomersController).GetMethod(action)!;
        Assert.Equal(role, method.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
    }
}
