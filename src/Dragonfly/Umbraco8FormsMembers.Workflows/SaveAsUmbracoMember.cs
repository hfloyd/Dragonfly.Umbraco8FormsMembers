namespace Dragonfly.Umbraco8FormsMembers.Workflows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
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

    public class SaveAsUmbracoMember : WorkflowType
    {
        public SaveAsUmbracoMember()
        {
            this.Id = new Guid("ad639eeb-5e7a-47cd-9555-31183171956e");
            this.Name = "Save as member";
            this.Description = "Saves the form values as an umbraco member";
        }

        [Setting("Member Type", Description = "Map member type", View = "~/App_Plugins/Dragonfly.Umbraco8FormsMembers/SettingTypes/membermapper.html")]
        public string Fields { get; set; }

        [Setting("POST to Url", Description = "An API url to POST-to after Member creation, passing the form record data as well as the new MemberId (in the form of 'MemberAndRecordData' class).")]
        public string PostToUrl { get; set; }

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
                IMember m;
                int memberId;
                try
                {
                    m = memberService.CreateMemberWithIdentity(usernameMapping, emailMapping, nameMapping, mt);
                    memberService.SavePassword(m, passwordMapping);
                    memberId = m.Id;
                }
                catch (Exception ex)
                {
                    Current.Logger.Error<SaveAsUmbracoMember>(ex, "Error while creating new member '{MemberName}' with email '{MemberEmail}'", nameMapping, emailMapping);
                    e.Form.MessageOnSubmit = "Oops! There was a problem registering you.";

                    if (ex.ToString().StartsWith("System.Web.Security.MembershipPasswordException"))
                    {
                        e.Form.MessageOnSubmit += "\nYour password didn't meet requirements.";
                    }
                    return WorkflowExecutionStatus.Failed;
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
                        Current.Logger.Error<SaveAsUmbracoMember>(ex, "Error while adding custom property data to new member '{MemberName}' with email '{MemberEmail}'", nameMapping, emailMapping);
                    }
                }

                memberService.Save(m);
                memberService.DisposeIfDisposable();
                Current.Logger.Info<SaveAsUmbracoMember>("Successfully created new member #{MemberId} '{MemberName}' with email '{MemberEmail}'", memberId, nameMapping, emailMapping);

                //Webapi?
                if (PostToUrl != "")
                {
                    var useASync = false; //TODO: Figure out why the Async code results in NULL data arriving at the POST Url
                    try
                    {
                        var siteDomain = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                        var mrData = new MemberAndRecordData();
                        mrData.FormRecord = record;
                        mrData.NewMemberName = m.Name;
                        mrData.NewMemberUserName = m.Username;
                        mrData.NewMemberId = memberId;

                        if (useASync)
                        {
                            var t = Task.Run(() => DoPostToUrlAsync(siteDomain, PostToUrl, mrData));
                            t.Wait();
                            Current.Logger.Info<SaveAsUmbracoMember>(
                                "POST Url '{ApiUrl}' for member '{MemberName}' Result =  '{ApiResponse}'",
                                PostToUrl, nameMapping, t.Result);
                        }
                        else
                        {
                            var result = DoPostToUrl(siteDomain, PostToUrl, mrData);
                            Current.Logger.Info<SaveAsUmbracoMember>(
                                "POST Url '{ApiUrl}' for member '{MemberName}' Result =  '{ApiResponse}'",
                                PostToUrl, nameMapping, result);
                        }
                    }
                    catch (Exception exception)
                    {
                        var msg = $"Error while posting to url '{PostToUrl}'";
                        Current.Logger.Error<SaveAsUmbracoMember>(exception, msg);
                    }
                }


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

        static string DoPostToUrl(string Domain, string Url, MemberAndRecordData PostData)
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

        static async Task<string> DoPostToUrlAsync(string Domain, string Url, MemberAndRecordData PostData)
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


    }
}
