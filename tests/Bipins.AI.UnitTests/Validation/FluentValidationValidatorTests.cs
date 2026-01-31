using Bipins.AI.Validation;
using Bipins.AI.Validation.FluentValidation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Bipins.AI.UnitTests.Validation;

public class FluentValidationValidatorTests
{
    private readonly Mock<ILogger<FluentValidationValidator<TestRequest>>> _mockLogger;

    public FluentValidationValidatorTests()
    {
        _mockLogger = new Mock<ILogger<FluentValidationValidator<TestRequest>>>();
    }

    [Fact]
    public async Task ValidateAsync_WhenValid_ReturnsValidResult()
    {
        var validator = new TestRequestValidator();
        var fluentValidator = new FluentValidationValidator<TestRequest>(validator, _mockLogger.Object);
        var request = new TestRequest { Name = "Test", Age = 25 };

        var result = await fluentValidator.ValidateAsync(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenInvalid_ReturnsErrors()
    {
        var validator = new TestRequestValidator();
        var fluentValidator = new FluentValidationValidator<TestRequest>(validator, _mockLogger.Object);
        var request = new TestRequest { Name = "", Age = -1 };

        var result = await fluentValidator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Age");
    }
}

// Test models
public class TestRequest
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0");
    }
}
