using System.Net.Security;
using PixelMate.Api.Mail;

namespace PixelMate.Api.Tests;

public class SmtpCertificateTests
{
    [Fact]
    public void Valid_certificates_are_accepted()
        => Assert.True(SmtpMailSender.ValidateServerCertificate(this, null, null, SslPolicyErrors.None));

    [Theory]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors)]
    public void Name_mismatch_or_missing_certificates_are_rejected(SslPolicyErrors errors)
        => Assert.False(SmtpMailSender.ValidateServerCertificate(this, null, null, errors));

    [Fact]
    public void Chain_errors_without_a_chain_are_rejected()
        => Assert.False(SmtpMailSender.ValidateServerCertificate(this, null, null, SslPolicyErrors.RemoteCertificateChainErrors));
}
