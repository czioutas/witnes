using Api.Application.Services;
using Api.Product.Actions.Entities;
using Api.Product.BusinessTemplate.Entities;

namespace Api.Tests.UnitTests;

[TestClass]
public sealed class CacheKeyGeneratorServiceTests
{
    /// <summary>
    /// Helper method to compare collections that may contain null values
    /// </summary>
    private static bool AreCollectionsEqual(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        if (expected == null && actual == null)
            return true;
        if (expected == null || actual == null)
            return false;

        using var expectedEnumerator = expected.GetEnumerator();
        using var actualEnumerator = actual.GetEnumerator();

        while (true)
        {
            bool hasExpectedNext = expectedEnumerator.MoveNext();
            bool hasActualNext = actualEnumerator.MoveNext();

            // If one collection ends before the other, they're not equal
            if (hasExpectedNext != hasActualNext)
                return false;

            // If both collections ended, they're equal
            if (!hasExpectedNext)
                return true;

            // Compare current items (handling null cases)
            if (expectedEnumerator.Current == null && actualEnumerator.Current == null)
                continue;
            if (expectedEnumerator.Current == null || actualEnumerator.Current == null)
                return false;
            if (!expectedEnumerator.Current.Equals(actualEnumerator.Current))
                return false;
        }
    }

    [TestMethod]
    public void GenerateCacheKey_TrailingEmptyValues_AreRemoved()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("string1", "string2", "");

        // Assert
        Assert.AreEqual("businesstemplateentity:string1:string2", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["string1", "string2", ""], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_MultipleStrings_JoinedWithHyphens()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("string1", "string2");

        // Assert
        Assert.AreEqual("businesstemplateentity:string1:string2", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["string1", "string2"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_InvalidCharacters_AreSanitized()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("string:with:colons");

        // Assert
        Assert.AreEqual("businesstemplateentity:string_with_colons", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["string:with:colons"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_LeadingNullValues_ArePreserved()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>(null, "string1", "string2");

        // Assert
        Assert.AreEqual("businesstemplateentity::string1:string2", result.Key);
        Assert.IsTrue(AreCollectionsEqual([null, "string1", "string2"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_LeadingEmptyValues_ArePreserved()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("", "string1", "string2");

        // Assert
        Assert.AreEqual("businesstemplateentity::string1:string2", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["", "string1", "string2"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_MixedCaseInput_ConvertedToLowercase()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("StRiNg1", "STRING2");

        // Assert
        Assert.AreEqual("businesstemplateentity:string1:string2", result.Key);
        Assert.IsTrue(AreCollectionsEqual(new[] { "StRiNg1", "STRING2" }, result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_SingleIdentifier_UsedWithType()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("singlestring");

        // Assert
        Assert.AreEqual("businesstemplateentity:singlestring", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["singlestring"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_MultipleSanitizationPoints_AllSanitized()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("id:123", "date:2023:05:15");

        // Assert
        Assert.AreEqual("businesstemplateentity:id_123:date_2023_05_15", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["id:123", "date:2023:05:15"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        Exception expectedException = null;

        // Act
        try
        {
            CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>(Array.Empty<string>());
        }
        catch (Exception ex)
        {
            expectedException = ex;
        }

        // Assert
        Assert.IsNotNull(expectedException);
    }

    [TestMethod]
    public void GenerateCacheKey_NullArray_ThrowsArgumentException()
    {
        // Arrange
        Exception expectedException = null;

        // Act
        try
        {
            CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>(null);
        }
        catch (Exception ex)
        {
            expectedException = ex;
        }

        // Assert
        Assert.IsNotNull(expectedException);
    }

    [TestMethod]
    public void GenerateCacheKey_DifferentTypes_UseCorrectTypeNames()
    {
        // Arrange & Act
        var flightResult = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("identifier");
        var planeResult = CacheKeyGeneratorService.GenerateCacheKey<ActionEntity>("identifier");

        // Assert
        Assert.AreEqual("businesstemplateentity:identifier", flightResult.Key);
        Assert.AreEqual("actionentity:identifier", planeResult.Key);
    }

    [TestMethod]
    public void GenerateCacheKey_MiddleEmptyValues_ArePreserved()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("string1", "", "string3");

        // Assert
        Assert.AreEqual("businesstemplateentity:string1::string3", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["string1", "", "string3"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_MiddleNullValues_ArePreserved()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("string1", null, "string3");

        // Assert
        Assert.AreEqual("businesstemplateentity:string1::string3", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["string1", null, "string3"], result.Identifiers));
    }

    [TestMethod]
    public void GenerateCacheKey_OnlyNullAndEmptyValues_RemovesTrailingOnly()
    {
        // Arrange & Act
        var result = CacheKeyGeneratorService.GenerateCacheKey<BusinessTemplateEntity>("", null, "", null);

        // Assert
        Assert.AreEqual("businesstemplateentity", result.Key);
        Assert.IsTrue(AreCollectionsEqual(["", null, "", null], result.Identifiers));
    }
}
