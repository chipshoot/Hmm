using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using System.Linq.Expressions;

namespace Hmm.Core.Map.Tests;

/// <summary>
/// Tests for ExpressionHelper class that verifies expression caching and combination functionality.
/// </summary>
public class ExpressionHelperTests
{
    [Fact]
    public void GetIsActivatedExpression_Returns_Valid_Expression_For_AuthorDao()
    {
        // Act
        var expr = ExpressionHelper.GetIsActivatedExpression<AuthorDao>();

        // Assert
        Assert.NotNull(expr);

        // Test with activated author
        var activatedAuthor = new AuthorDao { Id = 1, AccountName = "test", IsActivated = true };
        var compiledExpr = expr.Compile();
        Assert.True(compiledExpr(activatedAuthor));

        // Test with deactivated author
        var deactivatedAuthor = new AuthorDao { Id = 2, AccountName = "test2", IsActivated = false };
        Assert.False(compiledExpr(deactivatedAuthor));
    }

    [Fact]
    public void GetIsActivatedExpression_Returns_Same_Cached_Instance()
    {
        // Act
        var expr1 = ExpressionHelper.GetIsActivatedExpression<AuthorDao>();
        var expr2 = ExpressionHelper.GetIsActivatedExpression<AuthorDao>();

        // Assert - should return same cached instance
        Assert.Same(expr1, expr2);
    }

    [Fact]
    public void GetIsActivatedExpression_Returns_Different_Expressions_For_Different_Types()
    {
        // Act
        var authorExpr = ExpressionHelper.GetIsActivatedExpression<AuthorDao>();
        var tagExpr = ExpressionHelper.GetIsActivatedExpression<TagDao>();

        // Assert - should be different expressions for different types
        Assert.NotSame(authorExpr, tagExpr);
    }

    [Fact]
    public void GetIsActivatedExpression_Throws_For_Type_Without_IsActivated()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ExpressionHelper.GetIsActivatedExpression<HmmNoteDao>());
    }

    [Fact]
    public void CombineWithIsActivated_Returns_IsActivated_When_Query_Is_Null()
    {
        // Act
        var result = ExpressionHelper.CombineWithIsActivated<Author, AuthorDao>(null);

        // Assert
        Assert.NotNull(result);

        var compiledExpr = result.Compile();
        var activatedAuthor = new AuthorDao { Id = 1, AccountName = "test", IsActivated = true };
        var deactivatedAuthor = new AuthorDao { Id = 2, AccountName = "test2", IsActivated = false };

        Assert.True(compiledExpr(activatedAuthor));
        Assert.False(compiledExpr(deactivatedAuthor));
    }

    [Fact]
    public void CombineWithIsActivated_Combines_Query_With_IsActivated()
    {
        // Arrange
        Expression<Func<Author, bool>> query = a => a.AccountName == "test";

        // Act
        var result = ExpressionHelper.CombineWithIsActivated<Author, AuthorDao>(query);

        // Assert
        Assert.NotNull(result);

        var compiledExpr = result.Compile();

        // Should match: correct account name AND activated
        var matchingActivated = new AuthorDao { Id = 1, AccountName = "test", IsActivated = true };
        Assert.True(compiledExpr(matchingActivated));

        // Should not match: correct account name but NOT activated
        var matchingDeactivated = new AuthorDao { Id = 2, AccountName = "test", IsActivated = false };
        Assert.False(compiledExpr(matchingDeactivated));

        // Should not match: activated but wrong account name
        var nonMatchingActivated = new AuthorDao { Id = 3, AccountName = "other", IsActivated = true };
        Assert.False(compiledExpr(nonMatchingActivated));
    }

    [Fact]
    public void CombineExpressions_Combines_Two_Expressions_With_AndAlso()
    {
        // Arrange
        Expression<Func<AuthorDao, bool>> expr1 = a => a.AccountName == "test";
        Expression<Func<AuthorDao, bool>> expr2 = a => a.IsActivated;

        // Act
        var combined = ExpressionHelper.CombineExpressions(expr1, expr2);

        // Assert
        Assert.NotNull(combined);

        var compiledExpr = combined.Compile();

        // Both conditions true
        var bothTrue = new AuthorDao { AccountName = "test", IsActivated = true };
        Assert.True(compiledExpr(bothTrue));

        // First true, second false
        var firstTrueOnly = new AuthorDao { AccountName = "test", IsActivated = false };
        Assert.False(compiledExpr(firstTrueOnly));

        // First false, second true
        var secondTrueOnly = new AuthorDao { AccountName = "other", IsActivated = true };
        Assert.False(compiledExpr(secondTrueOnly));

        // Both false
        var bothFalse = new AuthorDao { AccountName = "other", IsActivated = false };
        Assert.False(compiledExpr(bothFalse));
    }

    [Fact]
    public void CombineExpressions_Throws_On_Null_First_Expression()
    {
        // Arrange
        Expression<Func<AuthorDao, bool>> expr2 = a => a.IsActivated;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ExpressionHelper.CombineExpressions<AuthorDao>(null!, expr2));
    }

    [Fact]
    public void CombineExpressions_Throws_On_Null_Second_Expression()
    {
        // Arrange
        Expression<Func<AuthorDao, bool>> expr1 = a => a.AccountName == "test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ExpressionHelper.CombineExpressions(expr1, null!));
    }

    [Fact]
    public void CombineWithIsActivated_Works_For_Tag_Entities()
    {
        // Arrange
        Expression<Func<Tag, bool>> query = t => t.Name == "Important";

        // Act
        var result = ExpressionHelper.CombineWithIsActivated<Tag, TagDao>(query);

        // Assert
        Assert.NotNull(result);

        var compiledExpr = result.Compile();

        var matchingActivated = new TagDao { Id = 1, Name = "Important", IsActivated = true };
        Assert.True(compiledExpr(matchingActivated));

        var matchingDeactivated = new TagDao { Id = 2, Name = "Important", IsActivated = false };
        Assert.False(compiledExpr(matchingDeactivated));
    }

    [Fact]
    public void CombineWithIsActivated_Returns_IsActivated_For_Contact_When_Query_Is_Null()
    {
        // Arrange - ContactDao stores contact info as JSON, so we can only test with null query
        // which returns just the IsActivated filter

        // Act
        var result = ExpressionHelper.CombineWithIsActivated<Contact, ContactDao>(null);

        // Assert
        Assert.NotNull(result);

        var compiledExpr = result.Compile();
        var activatedContact = new ContactDao { Id = 1, Contact = "{}", IsActivated = true };
        var deactivatedContact = new ContactDao { Id = 2, Contact = "{}", IsActivated = false };

        Assert.True(compiledExpr(activatedContact));
        Assert.False(compiledExpr(deactivatedContact));
    }
}
