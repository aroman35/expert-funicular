using System;
using System.Text;
using ExpertFunicular.Common.Messaging;
using Shouldly;
using Xunit;

namespace ExpertFunicular.UnitTests;

public class HeaderTests
{
    [Fact]
    public unsafe void Tests()
    {
        var testHeader = "CONTENT_LENGTH=314".AsSpan();
        Span<byte> testHeaderBytes = stackalloc byte[testHeader.Length];
        Encoding.UTF8.GetBytes(testHeader, testHeaderBytes);
        var header = Header.Parse(testHeaderBytes);
        header.ToString().ShouldBe("CONTENT_LENGTH=314");
    }
}