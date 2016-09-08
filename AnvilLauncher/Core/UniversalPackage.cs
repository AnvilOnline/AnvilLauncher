using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace AnvilLauncher.Core
{
    public class UniversalPackage
    {
        public string Name { get; protected set; }
        public string FullName { get; protected set; }
        public string FamilyName { get; protected set; }
        public string Version { get; protected set; }
        public string Publisher { get; protected set; }
        public string PublisherId { get; protected set; }
        public string Location { get; protected set; }
        public string Architecture { get; protected set; }
        public bool IsFramework { get; protected set; }
        public IEnumerable<string> Accounts { get; protected set; }

        public UniversalPackage(Package p_Package, PackageManager p_Manager)
        {
            var s_Id = p_Package.Id;
            Name = s_Id.Name;
            FullName = s_Id.FullName;
            FamilyName = s_Id.FamilyName;

            var s_Version = s_Id.Version;
            Version = $"{s_Version.Major}.{s_Version.Minor}.{s_Version.Build}.{s_Version.Revision}";
            
            Publisher = s_Id.Publisher;
            PublisherId = s_Id.PublisherId;
            Location = p_Package.InstalledLocation.Path;
            Architecture = s_Id.Architecture.ToString();
            IsFramework = p_Package.IsFramework;

            Accounts = p_Manager.FindUsers(FullName).Select(p_User => SidToAccountName(p_User.UserSecurityId)).ToList();
        }

        private static string SidToAccountName(string p_SidString)
        {
            var s_Sid = new SecurityIdentifier(p_SidString);
            try
            {
                var s_Account = (NTAccount) s_Sid.Translate(typeof(NTAccount));
                return s_Account.ToString();
            }
            catch (IdentityNotMappedException)
            {
                return p_SidString;
            }
        }
    }
}
