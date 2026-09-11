using System.ComponentModel.DataAnnotations;
using API.DTOs.Requests;
using API.Entities;

namespace API.Tests.Dtos;

/// Couvre les règles portées par les Data Annotations des DTOs de requête (§6,
/// §7 règle 15/16 : la seule validation d'entrée de ce projet, jamais dupliquée
/// ailleurs). Ces DTOs ne sont normalement validés que par [ApiController] au
/// runtime ASP.NET Core ; Validator.TryValidateObject reproduit ce comportement
/// hors contexte HTTP.
public class RequestDtoValidationTests
{
    private static List<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TransactionRequestDto_AmountNotStrictlyPositive_FailsValidation(decimal amount)
    {
        // §3.C règle du signe : Amount est toujours positif, validé une seule fois
        // ici — jamais 0 ni négatif, quel que soit Type.
        var dto = new TransactionRequestDto
        {
            Amount = amount, Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1), Label = "x", CategoryId = Guid.NewGuid(),
        };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(TransactionRequestDto.Amount)));
    }

    [Fact]
    public void TransactionRequestDto_PositiveAmount_PassesValidation()
    {
        var dto = new TransactionRequestDto
        {
            Amount = 0.01m, Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1), Label = "x", CategoryId = Guid.NewGuid(),
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void TransactionRequestDto_MissingLabel_FailsValidation()
    {
        var dto = new TransactionRequestDto
        {
            Amount = 10, Type = TransactionType.Expense,
            Date = new DateOnly(2026, 9, 1), Label = null, CategoryId = Guid.NewGuid(),
        };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(TransactionRequestDto.Label)));
    }

    [Theory]
    [InlineData("2026-9")]
    [InlineData("2026/09")]
    [InlineData("26-09")]
    [InlineData("2026-13")]
    public void BudgetRequestDto_MonthNotInYyyyMmFormat_FailsValidation(string month)
    {
        var dto = new BudgetRequestDto { Month = month, AmountLimit = 100, AutoRenew = false };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(BudgetRequestDto.Month)));
    }

    [Fact]
    public void BudgetRequestDto_NegativeAmountLimit_FailsValidation()
    {
        var dto = new BudgetRequestDto { Month = "2026-09", AmountLimit = -1, AutoRenew = false };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(BudgetRequestDto.AmountLimit)));
    }

    [Fact]
    public void BudgetRequestDto_ZeroAmountLimit_PassesValidation()
    {
        // §Budgets — decision : AmountLimit accepte 0 (placeholder de budget global),
        // seule une valeur négative est refusée.
        var dto = new BudgetRequestDto { Month = "2026-09", AmountLimit = 0, AutoRenew = false };

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData("#3b82f6")]
    [InlineData("#FFFFFF")]
    public void CategoryRequestDto_ValidHexColor_PassesValidation(string color)
    {
        var dto = new CategoryRequestDto { Name = "Groceries", Icon = "pi-cart", Color = color, Type = TransactionType.Expense };

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData("3b82f6")]
    [InlineData("#3b82f")]
    [InlineData("blue")]
    public void CategoryRequestDto_InvalidHexColor_FailsValidation(string color)
    {
        var dto = new CategoryRequestDto { Name = "Groceries", Icon = "pi-cart", Color = color, Type = TransactionType.Expense };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(CategoryRequestDto.Color)));
    }

    [Fact]
    public void CategoryRequestDto_NameTooShort_FailsValidation()
    {
        var dto = new CategoryRequestDto { Name = "A", Icon = "pi-cart", Color = "#3b82f6", Type = TransactionType.Expense };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(CategoryRequestDto.Name)));
    }
}
