// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Coherence.Utils;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="ErrorResponse"/>.
    /// </summary>
    public class ErrorResponseTests
    {
        [Test, Description("Make sure that deserializing each enum member works, and that there are no duplicates.")]
        public void Deserializing_ErrorCode_Works()
        {
            var validated = new HashSet<string>();
            foreach (var member in typeof(ErrorCode).GetFields())
            {
                if (member.GetCustomAttribute<EnumMemberAttribute>() is { } attribute)
                {
                    var json = "{\"error_code\":\"" + attribute.Value + "\"}}";
                    var deserialized = CoherenceJson.DeserializeObject<ErrorResponse>(json);
                    var isUnique = validated.Add(attribute.Value);

                    Assert.That(isUnique, Is.True, $"ErrorCode contains two members with the same EnumMember.Value: \"{attribute.Value}\".");
                    Assert.That(deserialized.ErrorCode, Is.EqualTo(member.GetValue(null)), $"Enum member {member.Name} deserialized to {deserialized.ErrorCode} instead of {member.GetValue(null)}");
                    Assert.That(deserialized.ErrorCode, Is.Not.EqualTo(ErrorCode.Unknown), $"Enum member {member.Name} deserialized to Unknown.");
                }
            }

            Assert.That(validated.Count, Is.GreaterThan(0));
        }
    }
}
