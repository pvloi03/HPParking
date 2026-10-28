using FluentValidation.TestHelper;
using HPParking.Api.DTOs.Cards;
using HPParking.Api.Validators.Cards;
using HPParking.Core.Models.Entities;
using Xunit;

namespace HPParking.Tests.Api.Unit
{
    public class CardValidatorTests
    {
        private readonly CreateCardRequestValidator _createValidator = new();
        private readonly UpdateCardRequestValidator _updateValidator = new();

        [Fact]
        public void CreateCardValidator_WhenMonthlyValidDates_PassesValidation()
        {
            var model = new CreateCardRequest
            {
                CardNumber = "1234567890",
                TargetType = CardTargetType.Monthly,
                ValidFrom = "2026-01-01",
                ValidTo = "2026-12-31"
            };

            var result = _createValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CreateCardValidator_WhenMonthlyMissingValidDates_FailsValidation()
        {
            var model = new CreateCardRequest
            {
                CardNumber = "1234567890",
                TargetType = CardTargetType.Monthly,
                ValidFrom = null,
                ValidTo = null
            };

            var result = _createValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ValidFrom);
            result.ShouldHaveValidationErrorFor(x => x.ValidTo);
        }

        [Fact]
        public void CreateCardValidator_WhenMonthlyValidToBeforeValidFrom_FailsValidation()
        {
            var model = new CreateCardRequest
            {
                CardNumber = "1234567890",
                TargetType = CardTargetType.Monthly,
                ValidFrom = "2026-12-31",
                ValidTo = "2026-01-01"
            };

            var result = _createValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void CreateCardValidator_WhenMonthlyInvalidDateFormat_FailsValidation()
        {
            var model = new CreateCardRequest
            {
                CardNumber = "1234567890",
                TargetType = CardTargetType.Monthly,
                ValidFrom = "01/01/2026",
                ValidTo = "2026-02-29" // invalid leap year date
            };

            var result = _createValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ValidFrom);
            result.ShouldHaveValidationErrorFor(x => x.ValidTo);
        }

        [Fact]
        public void UpdateCardValidator_WhenMonthlyValidDates_PassesValidation()
        {
            var model = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ValidFrom = "2026-05-01",
                ValidTo = "2026-05-31"
            };

            var result = _updateValidator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateCardValidator_WhenMonthlyValidToBeforeValidFrom_FailsValidation()
        {
            var model = new UpdateCardRequest
            {
                TargetType = CardTargetType.Monthly,
                ValidFrom = "2026-05-31",
                ValidTo = "2026-05-01"
            };

            var result = _updateValidator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validators_WhenNonMonthly_ValidDatesNotRequired()
        {
            var createModel = new CreateCardRequest
            {
                CardNumber = "1234567890",
                TargetType = CardTargetType.Visitor,
                ValidFrom = null,
                ValidTo = null
            };

            var updateModel = new UpdateCardRequest
            {
                TargetType = CardTargetType.Visitor,
                ValidFrom = null,
                ValidTo = null
            };

            _createValidator.TestValidate(createModel).ShouldNotHaveAnyValidationErrors();
            _updateValidator.TestValidate(updateModel).ShouldNotHaveAnyValidationErrors();
        }
    }
}
