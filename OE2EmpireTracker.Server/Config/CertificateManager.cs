using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OE2EmpireTracker.Server.Config;

/// <summary>
/// Manages TLS certificates for the server.
/// Generates a self-signed certificate on first run if none is configured.
/// </summary>
public static class CertificateManager
{
    /// <summary>
    /// Gets or creates a certificate for HTTPS.
    /// If a custom certificate path is configured, loads it.
    /// Otherwise generates a self-signed cert and persists it.
    /// </summary>
    public static X509Certificate2 GetOrCreateCertificate(
        IConfiguration configuration,
        ILogger logger)
    {
        var certPath = configuration["Server:CertificatePath"];
        var certPassword = configuration["Server:CertificatePassword"];

        if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
        {
            var cert = new X509Certificate2(certPath, certPassword);
            logger.LogInformation(
                "Loaded custom certificate: Subject={Subject}, Thumbprint={Thumbprint}",
                cert.Subject,
                cert.Thumbprint);
            return cert;
        }

        // Generate or load self-signed certificate
        var dataPath = configuration["Storage:DataPath"] ?? "./data";
        var selfSignedPath = Path.Combine(dataPath, "server-cert.pfx");

        if (File.Exists(selfSignedPath))
        {
            var existing = new X509Certificate2(selfSignedPath, "oe2-server");
            logger.LogInformation(
                "Loaded existing self-signed certificate: Thumbprint={Thumbprint}",
                existing.Thumbprint);
            return existing;
        }

        // Generate new self-signed certificate
        Directory.CreateDirectory(dataPath);
        var generated = GenerateSelfSignedCertificate();
        var pfxBytes = generated.Export(X509ContentType.Pfx, "oe2-server");
        File.WriteAllBytes(selfSignedPath, pfxBytes);

        logger.LogInformation(
            "Generated new self-signed certificate: Thumbprint={Thumbprint}",
            generated.Thumbprint);
        logger.LogWarning(
            "Share this thumbprint with clients for certificate pinning: {Thumbprint}",
            generated.Thumbprint);

        return generated;
    }

    private static X509Certificate2 GenerateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=OE2 Empire Tracker Server",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                false));

        // Add SAN for localhost
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        // Export and re-import to make the private key persistable on Windows
        return new X509Certificate2(
            cert.Export(X509ContentType.Pfx, "temp"),
            "temp",
            X509KeyStorageFlags.Exportable);
    }
}
