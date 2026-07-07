using System;
using PageWeaver;
using Xunit;

public class ClientTests
{
    [Fact]
    public void RequiresApiKey()
    {
        Assert.Throws<ArgumentException>(() => new PageWeaverClient(""));
    }

    [Fact]
    public void ConstructsWithApiKey()
    {
        var client = new PageWeaverClient("pk_test_x");
        Assert.NotNull(client);
    }

    [Fact]
    public void ExceptionCarriesStatusAndBody()
    {
        var e = new PageWeaverException("boom", 402, "quota");
        Assert.Equal(402, e.StatusCode);
        Assert.Equal("quota", e.Body);
    }
}
