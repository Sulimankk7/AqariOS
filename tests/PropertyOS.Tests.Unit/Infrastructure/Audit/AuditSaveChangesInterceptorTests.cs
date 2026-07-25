using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using PropertyOS.Domain.Audit.Enums;
using PropertyOS.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Audit;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public void GetSafeAuditValue_Should_Convert_IPv4_Address_To_String()
    {
        var ip = IPAddress.Parse("127.0.0.1");
        var result = AuditSaveChangesInterceptor.GetSafeAuditValue(ip);

        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void GetSafeAuditValue_Should_Convert_IPv6_Address_To_String()
    {
        var ip = IPAddress.Parse("::1");
        var result = AuditSaveChangesInterceptor.GetSafeAuditValue(ip);

        result.Should().Be("::1");
    }

    [Fact]
    public void GetSafeAuditValue_Should_Return_Null_For_Null_Or_DBNull()
    {
        AuditSaveChangesInterceptor.GetSafeAuditValue(null).Should().BeNull();
        AuditSaveChangesInterceptor.GetSafeAuditValue(DBNull.Value).Should().BeNull();
    }

    [Fact]
    public void GetSafeAuditValue_Should_Convert_Guid_To_String()
    {
        var guid = Guid.NewGuid();
        var result = AuditSaveChangesInterceptor.GetSafeAuditValue(guid);

        result.Should().Be(guid.ToString("D"));
    }

    [Fact]
    public void GetSafeAuditValue_Should_Convert_Enum_To_String()
    {
        var action = AuditAction.Create;
        var result = AuditSaveChangesInterceptor.GetSafeAuditValue(action);

        result.Should().Be("Create");
    }

    [Fact]
    public void GetSafeAuditValue_Should_Convert_DateTime_And_DateTimeOffset_To_Iso_String()
    {
        var dt = new DateTime(2026, 7, 24, 1, 30, 0, DateTimeKind.Utc);
        var dto = new DateTimeOffset(2026, 7, 24, 1, 30, 0, TimeSpan.Zero);

        AuditSaveChangesInterceptor.GetSafeAuditValue(dt).Should().Be(dt.ToString("o"));
        AuditSaveChangesInterceptor.GetSafeAuditValue(dto).Should().Be(dto.ToString("o"));
    }

    [Fact]
    public void GetSafeAuditValue_Should_Preserve_Primitives_And_Strings()
    {
        AuditSaveChangesInterceptor.GetSafeAuditValue("hello").Should().Be("hello");
        AuditSaveChangesInterceptor.GetSafeAuditValue(123).Should().Be(123);
        AuditSaveChangesInterceptor.GetSafeAuditValue(true).Should().Be(true);
        AuditSaveChangesInterceptor.GetSafeAuditValue(45.67m).Should().Be(45.67m);
    }

    [Fact]
    public void AuditDictionary_With_IPAddress_Should_Serialize_Without_SocketException()
    {
        var values = new Dictionary<string, object?>
        {
            ["IpAddress"] = AuditSaveChangesInterceptor.GetSafeAuditValue(IPAddress.Parse("127.0.0.1")),
            ["UserAgent"] = "Mozilla/5.0",
            ["ExpiresAt"] = AuditSaveChangesInterceptor.GetSafeAuditValue(DateTimeOffset.UtcNow)
        };

        Action act = () => JsonSerializer.Serialize(values);

        act.Should().NotThrow();
        var json = JsonSerializer.Serialize(values);
        json.Should().Contain("\"IpAddress\":\"127.0.0.1\"");
    }
}
