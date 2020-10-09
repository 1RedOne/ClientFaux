namespace ClientFaux.Models
{
    using System.ComponentModel;

    public enum StatusEnum
    {
        [Description("Creating Certificate")]
        CreateCert = 1,

        [Description("Registering Client")]
        RegisteringClient = 2,

        [Description("Sending Discovery")]
        SendingDiscovery = 3
    }
}