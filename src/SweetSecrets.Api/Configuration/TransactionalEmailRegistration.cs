using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Email;
using SweetSecrets.Infrastructure.Services.Email;

namespace SweetSecrets.Api.Configuration;

public static class TransactionalEmailRegistration
{
    public static IServiceCollection AddTransactionalEmailDelivery(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        if (environment.IsDevelopment())
        {
            services.AddScoped<ITransactionalEmailSender, DevelopmentTransactionalEmailSender>();
        }
        else
        {
            services.AddSingleton<ISmtpTransport, MailKitSmtpTransport>();
            services.AddScoped<ITransactionalEmailSender, SmtpTransactionalEmailSender>();
        }
        return services;
    }
}
