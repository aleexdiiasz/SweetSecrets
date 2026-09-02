using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SweetSecrets.Api.Configuration;
using SweetSecrets.Application.Common.Email;
using SweetSecrets.Infrastructure.Services.Email;

namespace SweetSecrets.UnitTests;

public sealed class ProductionEmailTests
{
    [Fact]
    public void Development_SelectsFileSender_AndProductionSelectsSmtpSender()
    {
        var configuration = new ConfigurationBuilder().Build();
        var development = new ServiceCollection().AddTransactionalEmailDelivery(configuration,new TestEnvironment(Environments.Development));
        var production = new ServiceCollection().AddTransactionalEmailDelivery(configuration,new TestEnvironment(Environments.Production));
        Assert.Contains(development,d=>d.ServiceType==typeof(ITransactionalEmailSender)&&d.ImplementationType==typeof(DevelopmentTransactionalEmailSender));
        Assert.Contains(production,d=>d.ServiceType==typeof(ITransactionalEmailSender)&&d.ImplementationType==typeof(SmtpTransactionalEmailSender));
        Assert.DoesNotContain(production,d=>d.ImplementationType==typeof(UnconfiguredTransactionalEmailSender));
    }

    [Fact]
    public void SmtpOptionsValidator_AcceptsCompleteConfiguration()
    {
        var result = new SmtpOptionsValidator().Validate(null,ValidOptions());
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("host")]
    [InlineData("from")]
    [InlineData("credentials")]
    public void ProductionValidation_RejectsIncompleteSmtp_WithoutPrintingSecrets(string missing)
    {
        var values=ValidProductionConfiguration();
        if(missing=="host") values["Email:Smtp:Host"]=null;
        if(missing=="from") values["Email:Smtp:FromEmail"]=null;
        if(missing=="credentials") values["Email:Smtp:Username"]="smtp-user";
        var exception=Assert.Throws<InvalidOperationException>(()=>ProductionConfigurationValidator.Validate(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),new TestEnvironment(Environments.Production)));
        Assert.Contains("Email:Smtp",exception.Message);
        Assert.DoesNotContain("secret",exception.Message,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smtp-user",exception.Message,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SmtpSender_BuildsExpectedFromToSubjectAndBody()
    {
        var transport=new RecordingTransport();
        var sender=new SmtpTransactionalEmailSender(Options.Create(ValidOptions()),transport,NullLogger<SmtpTransactionalEmailSender>.Instance);
        await sender.SendAsync(new TransactionalEmailMessage("owner@example.com","Asunto","Cuerpo seguro","password-reset"));
        var message=Assert.IsType<SmtpEnvelope>(transport.Message);
        Assert.Equal("SweetSecrets",message.FromName);
        Assert.Equal("no-reply@example.com",message.FromEmail);
        Assert.Equal("owner@example.com",message.ToEmail);
        Assert.Equal("Asunto",message.Subject); Assert.Equal("Cuerpo seguro",message.TextBody);
    }

    [Fact]
    public async Task SmtpFailure_IsSanitizedAndDoesNotLeakConfiguration()
    {
        var options=ValidOptions();options.Password=Guid.NewGuid().ToString("N");options.Username="test-user";
        var sender=new SmtpTransactionalEmailSender(Options.Create(options),new ThrowingTransport(),NullLogger<SmtpTransactionalEmailSender>.Instance);
        var exception=await Assert.ThrowsAsync<InvalidOperationException>(()=>sender.SendAsync(
            new TransactionalEmailMessage("owner@example.com","Subject","token-sensitive-body","email-confirmation")));
        Assert.Equal("No fue posible entregar el email transaccional.",exception.Message);
        Assert.DoesNotContain(options.Password,exception.ToString()); Assert.DoesNotContain("token-sensitive-body",exception.ToString());
    }

    private static SmtpOptions ValidOptions()=>new(){Host="smtp.example.com",Port=587,UseSsl=false,FromEmail="no-reply@example.com",FromName="SweetSecrets"};
    private static Dictionary<string,string?> ValidProductionConfiguration()=>new()
    {
        ["ConnectionStrings:MasterDatabase"]="Host=db;Database=master",
        ["Cors:AllowedOrigins:0"]="https://app.example.com",
        ["PasswordRecovery:ResetPageBaseUrl"]="https://app.example.com/reset-password",
        ["EmailConfirmation:ConfirmationPageBaseUrl"]="https://app.example.com/confirm-email",
        ["Email:Smtp:Host"]="smtp.example.com",["Email:Smtp:Port"]="587",
        ["Email:Smtp:FromEmail"]="no-reply@example.com",["Email:Smtp:FromName"]="SweetSecrets",
        ["DataProtection:KeysPath"]="/keys",["DataProtection:ApplicationName"]="SweetSecrets",
        ["ForwardedHeaders:KnownNetworks:0"]="172.30.0.0/24",["ForwardedHeaders:ForwardLimit"]="1",
        ["BootstrapAdmin:Email"]="admin@example.com",["BootstrapAdmin:Password"]="external-secret",
        ["BootstrapAdmin:FullName"]="Platform Admin"
    };
    private sealed class RecordingTransport:ISmtpTransport{public SmtpEnvelope? Message{get;private set;}public Task SendAsync(SmtpEnvelope message,SmtpOptions options,CancellationToken cancellationToken=default){Message=message;return Task.CompletedTask;}}
    private sealed class ThrowingTransport:ISmtpTransport{public Task SendAsync(SmtpEnvelope message,SmtpOptions options,CancellationToken cancellationToken=default)=>throw new InvalidOperationException("SMTP rejected");}
    private sealed class TestEnvironment(string name):IHostEnvironment{public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="Tests";public string ContentRootPath{get;set;}=Directory.GetCurrentDirectory();public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
