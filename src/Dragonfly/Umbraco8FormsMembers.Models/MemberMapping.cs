namespace Dragonfly.Umbraco8FormsMembers.Models
{
    using System.Runtime.Serialization;

    [DataContract(Name = "memberMapping")]
    public class MemberMapping
    {
        [DataMember(Name = "id")]
        public string Alias { get; set; }
        [DataMember(Name = "field")]
        public string Field { get; set; }

        [DataMember(Name = "staticValue")]
        public string StaticValue { get; set; }

        public bool HasValue()
        {
            return !string.IsNullOrEmpty(Field) || !string.IsNullOrEmpty(StaticValue);
        }
    }
}