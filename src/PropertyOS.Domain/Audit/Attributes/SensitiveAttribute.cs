using System;

namespace PropertyOS.Domain.Audit.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute
{
}
