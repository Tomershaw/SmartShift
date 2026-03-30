using Microsoft.Extensions.Configuration;
using Oci.Common.Auth;
using Oci.SecretsService;
using Oci.SecretsService.Models;
using Oci.SecretsService.Requests;
using System.Text;

namespace SmartShift.Infrastructure.Vault;

public class OciVaultConfigurationSource : IConfigurationSource
{
    public required string VaultOcid { get; set; }
    public required IEnumerable<string> SecretNames { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new OciVaultConfigurationProvider(this);
}

public class OciVaultConfigurationProvider : ConfigurationProvider
{
    private readonly OciVaultConfigurationSource _source;

    public OciVaultConfigurationProvider(OciVaultConfigurationSource source)
    {
        _source = source;
    }

    public override void Load()
    {
        var auth = new InstancePrincipalsAuthenticationDetailsProvider();
        using var client = new SecretsClient(auth);

        foreach (var secretName in _source.SecretNames)
        {
            try
            {
                var request = new GetSecretBundleByNameRequest
                {
                    VaultId = _source.VaultOcid,
                    SecretName = secretName
                };

                var response = client.GetSecretBundleByName(request).GetAwaiter().GetResult();

                if (response.SecretBundle.SecretBundleContent is Base64SecretBundleContentDetails content)
                {
                    var value = Encoding.UTF8.GetString(Convert.FromBase64String(content.Content));
                    var configKey = secretName.Replace("--", ":");
                    Data[configKey] = value;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OCI Vault] Failed to load secret '{secretName}': {ex.Message}");
            }
        }
    }
}

public static class OciVaultExtensions
{
    public static IConfigurationBuilder AddOciVault(
        this IConfigurationBuilder builder,
        string vaultOcid,
        IEnumerable<string> secretNames)
    {
        return builder.Add(new OciVaultConfigurationSource
        {
            VaultOcid = vaultOcid,
            SecretNames = secretNames
        });
    }
}
