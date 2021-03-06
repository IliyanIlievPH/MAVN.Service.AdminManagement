using Lykke.SettingsReader.Attributes;

namespace MAVN.Service.AdminManagement.Client 
{
    /// <summary>
    /// AdminManagement client settings.
    /// </summary>
    public class AdminManagementServiceClientSettings 
    {
        /// <summary>Service url.</summary>
        [HttpCheck("api/isalive")]
        public string ServiceUrl {get; set;}
    }
}
