using FluentEmail.Core;
using PadelMatcherNet.Services;
using PadelMatcherNet.EmailTemplates;
using PadelMatcherNet.Data;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string name);
    Task SendAccountConfirmationEmailAsync(string toEmail, string name, string confirmationLink);
    Task SendPasswordResetLinkAsync(ApplicationUser user, string toEmail, string resetLink);
}

public class EmailService : IEmailService
{
    private readonly IFluentEmail _email;
    private readonly RazorComponentRenderer _renderer;

    public EmailService(IFluentEmail email, RazorComponentRenderer renderer)
    {
        _email = email;
        _renderer = renderer;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string name)
    {
        await SendEmailAsync<WelcomeEmail>(
            toEmail, 
            "Benvenuto in PadelMatcher.net!", 
            new Dictionary<string, object?> { ["Name"] = name },
            "Invio email fallito");
    }

    public async Task SendAccountConfirmationEmailAsync(string toEmail, string name, string confirmationLink)
    {
        await SendEmailAsync<ConfirmationEmail>(
            toEmail, 
            "Conferma il tuo account", 
            new Dictionary<string, object?> 
            { 
                ["Name"] = name,
                ["ConfirmationLink"] = confirmationLink
            },
            "Invio email conferma account fallito");
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string toEmail, string resetLink)
    {
        await SendEmailAsync<PasswordResetEmail>(
            toEmail,
            "Reimposta la tua password",
            new Dictionary<string, object?>
            {
                ["Name"] = user.UserName,
                ["ResetLink"] = resetLink
            },
            "Invio email reimpostazione password fallito");
    }

    private async Task SendEmailAsync<T>(string toEmail, string subject, Dictionary<string, object?> parameters, string errorPrefix)
        where T : Microsoft.AspNetCore.Components.IComponent
    {
        var html = await _renderer.RenderAsync<T>(parameters);

        var result = await _email
            .To(toEmail)
            .Subject(subject)
            .Body(html, isHtml: true)
            .SendAsync();

        if (!result.Successful)
        {
            throw new Exception($"{errorPrefix}: {string.Join(", ", result.ErrorMessages)}");
        }
    }
}