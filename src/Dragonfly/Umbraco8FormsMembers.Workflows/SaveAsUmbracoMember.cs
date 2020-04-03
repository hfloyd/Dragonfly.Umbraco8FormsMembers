namespace Dragonfly.Umbraco8FormsMembers.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using Dragonfly;
    using Dragonfly.Umbraco8FormsMembers.Models;
    using Newtonsoft.Json;
    using Umbraco.Core;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;
    using Umbraco.Forms.Core;
    using Umbraco.Forms.Core.Attributes;
    using Umbraco.Forms.Core.Enums;
    using Umbraco.Forms.Core.Persistence.Dtos;
    using Umbraco.Web.Security;

    public class SaveAsUmbracoMember : WorkflowType
    {
        public SaveAsUmbracoMember()
        {
            this.Id = new Guid("ad639eeb-5e7a-47cd-9555-31183171956e");
            this.Name = "Save as member";
            this.Description = "Saves the form values as an umbraco member";
        }

        [Setting("Email Field Alias", Description = "Type in the Field Alias for Login/Email. Leave blank if you don't want the email validated against existing Members", View = "textfield")]
        public string EmailFieldAlias { get; set; }

        [Setting("Member Type", Description = "Map member type", View = "~/App_Plugins/Dragonfly.Umbraco8FormsMembers/SettingTypes/membermapper.html")]
        public string Fields { get; set; }

        [Setting("Member Group", Description = "Select the Member Group for this new Member", View = "dropdownlist")]
        public string MemberGroup { get; set; }

        //[Setting("Member Groups", Description = "Select Member Groups to add to this new Member", View = "checkboxlist")]
        //public List<string> MultipleMemberGroups { get; set; }

        [Setting("Log-In Member?", Description = "Automatically log-in the member after successful registration?", View = "checkbox")]
        public string LogInMemberAfterRegister { get; set; }

        //[Setting("POST to Url", Description = "An API url to POST-to after Member creation, passing the form record data as well as the new MemberId (in the form of 'MemberAndRecordData' class).")]
        //public string PostToUrl { get; set; }

        public override Dictionary<string, Setting> Settings()
        {
            // var memberService = Current.Services.MemberService;
            var memberGroupService = Current.Services.MemberGroupService;
            var groups = memberGroupService.GetAll().ToList();
            var groupNames = groups.Select(n => n.Name);

            var settings = base.Settings();
            settings["MemberGroup"].PreValues = string.Join(",", groupNames);

            return settings;
        }

        public override WorkflowExecutionStatus Execute(Record record, RecordEventArgs e)
        {
            var memberTypeService = Current.Services.MemberTypeService;
            var memberService = Current.Services.MemberService;
            var dataTypeService = Current.Services.DataTypeService;
            //var membershipHelper = Current. new MembershipHelper()

            var maps = JsonConvert.DeserializeObject<MemberMapper>(Fields);
            Dictionary<string, string> mappings = new Dictionary<string, string>();
            string nameMapping = "NodeName";
            string usernameMapping = "Login (Email)";
            string emailMapping = "Email";
            string passwordMapping = "Password";


            var mrData = new MemberRecordResult();
            mrData.FormRecord = record;
            mrData.Errors = new Dictionary<Umbraco8FormsMembersHelper.SaveError, string>();

            //Get Form Values for Member
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

            if (Umbraco8FormsMembersHelper.MemberExists(memberService, emailMapping))
            {
                //Provided email is already registered
                Current.Logger.Warn<SaveAsUmbracoMember>(
                    "New member '{MemberName}' cannot be created - email '{MemberEmail}' is already registered",
                    nameMapping, emailMapping);
                e.Form.MessageOnSubmit = "Oops! There was a problem registering you.";
                e.Form.MessageOnSubmit += "\nThat email address is already registered.";
                mrData.Errors.Add(Umbraco8FormsMembersHelper.SaveError.EmailAlreadyRegistered,
                    $"The email address '{emailMapping}' is already registered.");

                //Save Result & Return
                mrData.WorkflowResult = WorkflowExecutionStatus.Failed;
                SaveResultData(mrData);
                return WorkflowExecutionStatus.Failed;

            }
            // Helper.EmailValidate(email: emailMapping)


            MemberType mt = (MemberType)memberTypeService.Get(maps.MemTypeAlias);
            if (mt != null)
            {
                IMember m = null;
                int memberId;
                try
                {
                    m = memberService.CreateMemberWithIdentity(usernameMapping, emailMapping, nameMapping, mt);
                    memberService.SavePassword(m, passwordMapping);

                    memberId = m.Id;
                    mrData.NewMemberName = m.Name;
                    mrData.NewMemberUserName = m.Username;
                    mrData.NewMemberId = memberId;
                }
                catch (Exception ex)
                {
                    Current.Logger.Error<SaveAsUmbracoMember>(ex,
                        "Error while creating new member '{MemberName}' with email '{MemberEmail}'",
                        nameMapping, emailMapping);
                    e.Form.MessageOnSubmit = "Oops! There was a problem registering you.";
                    mrData.Errors.Add(Umbraco8FormsMembersHelper.SaveError.CreateMemberError, ex.Message);

                    if (ex.ToString().StartsWith("System.Web.Security.MembershipPasswordException"))
                    {
                        if (m != null)
                        {
                            memberService.Delete(m);
                        }

                        e.Form.MessageOnSubmit += "\nYour password didn't meet requirements.";
                        mrData.Errors.Add(Umbraco8FormsMembersHelper.SaveError.InvalidPassword, $"Your password didn't meet requirements.");
                    }

                    //Save Result & Return
                    mrData.WorkflowResult = WorkflowExecutionStatus.Failed;
                    SaveResultData(mrData);
                    return WorkflowExecutionStatus.Failed;
                }

                

                try
                {
                    //Add to Groups, if provided
                    //if (this.MultipleMemberGroups.Any())
                    //{
                    //    foreach (var group in this.MultipleMemberGroups)
                    //    {
                    //        memberService.AssignRole(memberId, group);
                    //    }

                    //}
                    if (!string.IsNullOrEmpty(this.MemberGroup))
                    {
                        memberService.AssignRole(memberId, this.MemberGroup);
                    }
                }
                catch (Exception ex)
                {
                    Current.Logger.Error<SaveAsUmbracoMember>(ex,
                        "Error while assigning group '{MemberGroup}' to new member '{MemberName}' with email '{MemberEmail}'",
                        MemberGroup, nameMapping, emailMapping);
                    mrData.Errors.Add(Umbraco8FormsMembersHelper.SaveError.UnableToAddtoMemberGroup, ex.Message);
                }

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
                        Current.Logger.Error<SaveAsUmbracoMember>(ex,
                            "Error while adding custom property data to new member '{MemberName}' with email '{MemberEmail}'",
                            nameMapping, emailMapping);

                        mrData.Errors.Add(Umbraco8FormsMembersHelper.SaveError.CustomPropertyError, $"{p.Alias} - {ex.Message}");
                    }
                }

                memberService.Save(m);
                memberService.DisposeIfDisposable();

                if (this.LogInMemberAfterRegister == true.ToString())
                {
                    //membershipHelper.Login("username", "password");
                    FormsAuthentication.SetAuthCookie(m.Username, false);
                }

                Current.Logger.Info<SaveAsUmbracoMember>(
                                    "Successfully created new member #{MemberId} '{MemberName}' with email '{MemberEmail}'",
                                    memberId, nameMapping, emailMapping);


                //Webapi? //Figure out why this causes a SQL Lock problem
                //if (PostToUrl != "")
                //{
                //    var useASync =
                //        false; //TODO: Figure out why the Async code results in NULL data arriving at the POST Url
                //    try
                //    {
                //        var siteDomain = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);


                //        if (useASync)
                //        {
                //            var t = Task.Run(() => DoPostToUrlAsync(siteDomain, PostToUrl, mrData));
                //            t.Wait();
                //            Current.Logger.Info<SaveAsUmbracoMember>(
                //                "POST Url '{ApiUrl}' for member '{MemberName}' Result =  '{ApiResponse}'",
                //                PostToUrl, nameMapping, t.Result);
                //        }
                //        else
                //        {
                //            var result = DoPostToUrl(siteDomain, PostToUrl, mrData);
                //            Current.Logger.Info<SaveAsUmbracoMember>(
                //                "POST Url '{ApiUrl}' for member '{MemberName}' Result =  '{ApiResponse}'",
                //                PostToUrl, nameMapping, result);
                //        }
                //    }
                //    catch (Exception exception)
                //    {
                //        var msg = $"Error while posting to url '{PostToUrl}'";
                //        Current.Logger.Error<SaveAsUmbracoMember>(exception, msg);
                //    }
                //}

            }

            //Save Result & Return
            mrData.WorkflowResult = WorkflowExecutionStatus.Completed;
            SaveResultData(mrData);
            return WorkflowExecutionStatus.Completed;
        }


        public override List<Exception> ValidateSettings()
        {
            var errors = new List<Exception>();
            return errors;
        }

        static string DoPostToUrl(string Domain, string Url, MemberRecordResult PostData)
        {
            var resultMsg = string.Empty;
            var url = Url.StartsWith("http") ? Url : Domain + Url;
            WebResponse response = null;

            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";

                // Create POST data and convert it to a byte array.  
                string postData = JsonConvert.SerializeObject(PostData);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                request.ContentType = "application/json";
                request.ContentLength = byteArray.Length;

                // Get the request stream.  
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Get the response.  
                response = request.GetResponse();
                // Display the status.  
                var result = ((HttpWebResponse)response);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    // Get the stream containing content returned by the server.  
                    // The using block ensures the stream is automatically closed.
                    using (dataStream = response.GetResponseStream())
                    {
                        // Open the stream using a StreamReader for easy access.  
                        StreamReader reader = new StreamReader(dataStream);
                        string responseFromServer = reader.ReadToEnd();
                        // Display the content.  
                        var responseContent = responseFromServer;

                        if (responseContent != "")
                        {
                            resultMsg = responseContent;
                        }
                        else
                        {
                            resultMsg = result.StatusCode.ToString();
                        }
                    }
                }
                else
                {
                    var resultJson = JsonConvert.SerializeObject(result);
                    resultMsg = $"FAILED: {result.StatusCode} \n{resultJson}";
                }
            }
            catch (Exception e)
            {
                var msg = $"Error in DoPostToUrl()";
                Current.Logger.Error<SaveAsUmbracoMember>(e, msg);
            }
            finally
            {
                // Close the response.  
                if (response != null)
                {
                    response.Close();
                }
            }

            return resultMsg;
        }

        static async Task<string> DoPostToUrlAsync(string Domain, string Url, MemberRecordResult PostData)
        {
            //TODO: Figure out why this Async code results in NULL data arriving at the POST Url

            //var values = new Dictionary<string, string>
            // {
            //     { "MemberAndRecordData", JsonPostData }
            // };
            // var content = new FormUrlEncodedContent(values);

            var json = JsonConvert.SerializeObject(PostData);
            //HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");


            var response = string.Empty;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Domain);

                //HttpResponseMessage result = await client.PostAsync(Url, content).ConfigureAwait(false);
                HttpResponseMessage result = client.PostAsJsonAsync(Url, json).Result;
                if (result.IsSuccessStatusCode)
                {
                    var responseContent = await result.Content.ReadAsStringAsync();

                    if (responseContent != "")
                    {
                        response = responseContent;
                    }
                    else
                    {
                        response = result.StatusCode.ToString();
                    }
                }
                else
                {
                    var resultJson = JsonConvert.SerializeObject(result);
                    response = $"FAILED: {result.StatusCode} \n{resultJson}";
                }
            }
            return response;
        }

        private void SaveResultData(MemberRecordResult Result)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Session.Add("SaveAsUmbracoMemberWorkflowResult", Result);

                Current.Logger.Info<SaveAsUmbracoMember>("Successfully added SaveAsUmbracoMemberWorkflowResult to 'HttpContext.Current.Session'");
            }
            else
            {
                Current.Logger.Info<SaveAsUmbracoMember>("Unable to add SaveAsUmbracoMemberWorkflowResult to 'HttpContext.Current.Session' - HttpContext.Current is NULL");
            }
        }
    }
}
