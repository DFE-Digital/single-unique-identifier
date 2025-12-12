using SUI.Find.Domain.ValueObjects;

namespace SUI.Find.Domain.UnitTests.ValueObjects;

public class EncryptedPersonIdTests
{
    [Fact]
    public void ValidId_ContainsValueWhenCreated()
    {
        var encryptedPersonIdResult = EncryptedPersonId.Create("Cy13hyZL-4LSIwVy50p-Hg");

        Assert.True(encryptedPersonIdResult.Success);
        Assert.NotNull(encryptedPersonIdResult.Value);
        Assert.Equal("Cy13hyZL-4LSIwVy50p-Hg", encryptedPersonIdResult.Value.Value);
    }

    [Fact]
    public void InvalidIdReturnsFail_WhenLengthIsTooShort()
    {
        var encryptedPersonIdResult = EncryptedPersonId.Create("blah-blah-blah");

        Assert.False(encryptedPersonIdResult.Success);
        Assert.Null(encryptedPersonIdResult.Value);
        Assert.Contains(
            "EncryptedPersonId must have length of 22 characters.",
            encryptedPersonIdResult.Error
        );
    }

    [Fact]
    public void InvalidIdReturnsFail_WhenIncorrectFormat()
    {
        var encryptedPersonIdResult = EncryptedPersonId.Create("asd-asd-asd-asd-asd-a=");

        Assert.False(encryptedPersonIdResult.Success);
        Assert.Null(encryptedPersonIdResult.Value);
        Assert.Contains(
            "EncryptedPersonId does not match expected format",
            encryptedPersonIdResult.Error
        );
    }
}
