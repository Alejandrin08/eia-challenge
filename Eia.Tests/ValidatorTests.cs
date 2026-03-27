using Eia.Connector.Models;
using Eia.Connector.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eia.Tests;

[TestClass]
public class ValidatorTests
{
    [TestMethod]
    public void IsValid_WithPeriodAndCapacity()
    {
        var r = new NuclearOutageRecord
        { Period = "2026-03-25", Capacity = 100_000, Outage = 20_000 };

        Assert.IsTrue(NuclearOutageValidator.IsValid(r, NullLogger.Instance));
    }

    [TestMethod]
    public void IsValid_WithNullPeriod()
    {
        var r = new NuclearOutageRecord { Period = null, Capacity = 1000 };
        Assert.IsFalse(NuclearOutageValidator.IsValid(r, NullLogger.Instance));
    }
}