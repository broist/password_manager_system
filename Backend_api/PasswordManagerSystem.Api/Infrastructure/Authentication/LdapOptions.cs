namespace PasswordManagerSystem.Api.Infrastructure.Authentication;

public class LdapOptions
{
    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; } = false;

    public string Domain { get; set; } = string.Empty;

    public string BaseDn { get; set; } = string.Empty;

    public string UserSearchFilter { get; set; } = "(&(objectClass=user)(sAMAccountName={0}))";

    public string GroupAttribute { get; set; } = "memberOf";
}