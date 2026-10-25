using FluentValidation;

namespace PixelMate.Api.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record EmailRequest(string Email);
public sealed record ConfirmEmailRequest(Guid UserId, string Token);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
public sealed record UserDto(Guid UserId, string Email, string DisplayName, bool EmailConfirmed);

internal static class Rules
{
    public static IRuleBuilderOptions<T, string> EmailRule<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().WithMessage("請輸入 Email。").MaximumLength(254).EmailAddress().WithMessage("Email 格式不正確。");

    /// <summary>10+ characters with a letter and a digit; the same limit the Identity options enforce.</summary>
    public static IRuleBuilderOptions<T, string> PasswordRule<T>(this IRuleBuilder<T, string> r) =>
        r.NotEmpty().WithMessage("請輸入密碼。")
            .MinimumLength(10).WithMessage("密碼至少 10 個字元。")
            .MaximumLength(128).WithMessage("密碼太長。")
            .Matches("[A-Za-z]").WithMessage("密碼需要包含英文字母。")
            .Matches("[0-9]").WithMessage("密碼需要包含數字。");
}

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).EmailRule();
        RuleFor(x => x.Password).PasswordRule();
        RuleFor(x => x.DisplayName).NotEmpty().WithMessage("請輸入暱稱。").MaximumLength(20).WithMessage("暱稱最多 20 個字。")
            .Must(n => n.All(c => !char.IsControl(c) && c != '<' && c != '>')).WithMessage("暱稱含有不允許的字元。");
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class EmailValidator : AbstractValidator<EmailRequest>
{
    public EmailValidator() => RuleFor(x => x.Email).EmailRule();
}

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
    }
}

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email).EmailRule();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.NewPassword).PasswordRule();
    }
}
