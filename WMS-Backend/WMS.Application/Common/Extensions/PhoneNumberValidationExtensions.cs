namespace WMS.Application.Common.Extensions;

public static class PhoneNumberValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilderInitial<T, string> rule)
    {
        return rule
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Phone number is required.")
            .MaximumLength(50)
            .Matches(@"\A\+?[0-9]+\z").WithMessage("Phone number may contain digits and a leading + only.");
    }
}
