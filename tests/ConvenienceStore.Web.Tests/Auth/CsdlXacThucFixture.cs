using ConvenienceStore.Web.Tests.HaTang;
using Xunit;

namespace ConvenienceStore.Web.Tests.Auth;

public sealed class CsdlXacThucFixture : CsdlPostgreSqlTam;

[CollectionDefinition(Ten)]
public sealed class CsdlXacThucCollection : ICollectionFixture<CsdlXacThucFixture>
{
    public const string Ten = "XacThuc";
}
