namespace Dragonfly.Umbraco8FormsMembers.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract(Name = "memberMapper")]
    public class MemberMapper
    {
        [DataMember(Name = "memtype")]
        public string MemTypeAlias { get; set; }

        [DataMember(Name = "nameField")]
        public string NameField { get; set; }

        [DataMember(Name = "nameStaticValue")]
        public string NameStaticValue { get; set; }

        [DataMember(Name = "loginField")]
        public string LoginField { get; set; }

        [DataMember(Name = "loginStaticValue")]
        public string LoginStaticValue { get; set; }

        [DataMember(Name = "emailField")]
        public string EmailField { get; set; }

        [DataMember(Name = "emailStaticValue")]
        public string EmailStaticValue { get; set; }

        [DataMember(Name = "passwordField")]
        public string PasswordField { get; set; }

        [DataMember(Name = "passwordStaticValue")]
        public string PasswordStaticValue { get; set; }

        [DataMember(Name = "properties")]
        public IEnumerable<MemberMapping> Properties { get; set; }
    }
}