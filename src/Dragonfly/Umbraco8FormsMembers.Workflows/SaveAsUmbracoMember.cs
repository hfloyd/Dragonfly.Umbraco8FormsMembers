namespace Dragonfly.Umbraco8FormsMembers.Workflows
{
    using System;
    using System.Collections.Generic;
    using Dragonfly.Umbraco8FormsMembers.Models;
    using Newtonsoft.Json;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;
    using Umbraco.Forms.Core;
    using Umbraco.Forms.Core.Attributes;
    using Umbraco.Forms.Core.Enums;
    using Umbraco.Forms.Core.Persistence.Dtos;

    public class SaveAsUmbracoMember : WorkflowType
    {
        private readonly ILogger _logger;
        public SaveAsUmbracoMember()
        {
            this.Id = new Guid("ad639eeb-5e7a-47cd-9555-31183171956e");
            this.Name = "Save as member";
            this.Description = "Saves the form values as an umbraco member";

            _logger = Current.Logger;
        }


        [Setting("Member Type", Description = "Map member type", View = "~/App_Plugins/Dragonfly.Umbraco8FormsMembers/SettingTypes/membermapper.html")]
        public string Fields { get; set; }

        public override WorkflowExecutionStatus Execute(Record record, RecordEventArgs e)
        {
            var memberTypeService = Current.Services.MemberTypeService;
            var memberService = Current.Services.MemberService;
            var dataTypeService = Current.Services.DataTypeService;

            var maps = JsonConvert.DeserializeObject<MemberMapper>(Fields);

            Dictionary<string, string> mappings = new Dictionary<string, string>();

            string nameMapping = "NodeName";
            string usernameMapping = "Login (Email)";
            string emailMapping = "Email";
            string passwordMapping = "Password";

            if (!string.IsNullOrEmpty(maps.NameStaticValue))
                nameMapping = maps.NameStaticValue;
            else if (!string.IsNullOrEmpty(maps.NameField))
                nameMapping = record.RecordFields[new Guid(maps.NameField)].ValuesAsString(false);

            if (!string.IsNullOrEmpty(maps.LoginStaticValue))
                usernameMapping = maps.LoginStaticValue;
            else if (!string.IsNullOrEmpty(maps.LoginField))
                usernameMapping = record.RecordFields[new Guid(maps.LoginField)].ValuesAsString(false);

            if (!string.IsNullOrEmpty(maps.EmailStaticValue))
                emailMapping = maps.EmailStaticValue;
            else if (!string.IsNullOrEmpty(maps.EmailField))
                emailMapping = record.RecordFields[new Guid(maps.EmailField)].ValuesAsString(false);

            if (!string.IsNullOrEmpty(maps.PasswordStaticValue))
                passwordMapping = maps.PasswordStaticValue;
            else if (!string.IsNullOrEmpty(maps.PasswordField))
                passwordMapping = record.RecordFields[new Guid(maps.PasswordField)].ValuesAsString(false);
            
            foreach (var map in maps.Properties)
            {
                if (map.HasValue())
                {
                    var val = map.StaticValue;
                    if (!string.IsNullOrEmpty(map.Field))
                        val = record.RecordFields[new Guid(map.Field)].ValuesAsString(false);
                    mappings.Add(map.Alias, val);
                }
            }

            MemberType mt = (MemberType)memberTypeService.Get(maps.MemTypeAlias);

            if (mt != null)
            {
                IMember m = memberService.CreateMemberWithIdentity(usernameMapping, emailMapping, nameMapping, mt);
                memberService.SavePassword(m, passwordMapping);

                foreach (Property p in m.Properties)
                {
                    try
                    {
                        if (mappings.ContainsKey(p.PropertyType.Alias))
                        {
                            var dataType = dataTypeService.GetDataType(p.PropertyType.DataTypeKey);
                            var formValue = mappings[p.PropertyType.Alias];

                            switch (dataType.DatabaseType)
                            {
                                case ValueStorageType.Date:
                                    DateTime dateValue;
                                    var isDate = DateTime.TryParse(formValue, out dateValue);
                                    if (isDate)
                                    {
                                        p.SetValue(dateValue);
                                    }
                                    break;

                                case ValueStorageType.Integer:
                                    if (formValue.ToLower() == "true" || formValue.ToLower() == "false")
                                    {
                                        p.SetValue(bool.Parse(formValue));
                                    }
                                    else
                                    {
                                        p.SetValue(int.Parse(formValue));
                                    }
                                    break;

                                //case ValueStorageType.Ntext:
                                //    break;
                                //case ValueStorageType.Nvarchar:
                                //    break;
                                //case ValueStorageType.Decimal:
                                //    break;

                                default:
                                    p.SetValue(formValue);
                                    break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.Error<SaveAsUmbracoMember>(ex);
                    }
                }

                memberService.Save(m);


                //store record id and member id

                //SettingsStorage ss = new SettingsStorage();
                //ss.InsertSetting(record.Id, "SaveAsUmbracoMemberCreatedMemberID", m.Id.ToString());
                //ss.Dispose();

            }

            return WorkflowExecutionStatus.Completed;
        }

        public override List<Exception> ValidateSettings()
        {
            return new List<Exception>();
        }
    }
}