namespace Dragonfly.Umbraco8FormsMembers.Controllers
{
    using System;
    using System.Linq;
    using System.Web.Http;
    using System.Web.Http.Results;
    using Dragonfly.Umbraco8FormsMembers.Models;
    using Newtonsoft.Json;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Logging;
    using Umbraco.Web.WebApi;

    // /Umbraco/Api/MemberTesterApi <-- UmbracoApiController

    // [IsBackOffice]
    // /Umbraco/backoffice/Api/MemberTesterApi <-- UmbracoAuthorizedApiController

    public class MemberTesterApiController : UmbracoApiController
    {
        private readonly ILogger _logger;

        public MemberTesterApiController(ILogger logger)
        {
            _logger = logger;
        }


        // /Umbraco/Api/MemberTesterApi/TestGet
        [System.Web.Http.AcceptVerbs("GET")]
        public bool TestGet()
        {
            return true;
        }

        // /Umbraco/Api/MemberTesterApi/TestPost
        //[HttpPost]
        //[Route("TestPost")]
        //public IHttpActionResult TestPost()
        //{
        //    return Ok("no data");
        //}

        // /Umbraco/Api/MemberTesterApi/TestPost
        [HttpPost]
        [Route("TestPost")]
        public IHttpActionResult TestPost(MemberAndRecordData Data)
        {
            //var membershipHelper = new Umbraco.Web.Security.MembershipHelper(Umbraco.Web.Composing.Current);
            var memberService = Current.Services.MemberService;

            try
            {
                var msg = "";
                if (Data != null)
                {
                    if (Data.NewMemberId != 0)
                    {
                        var newMember = memberService.GetById(Data.NewMemberId);
                        msg += "New Member=" + newMember.Name;
                    }
                    else
                    {
                        msg += "New Member Id Is Missing";
                    }
                }
                else
                {
                    msg += "Data Is Null";
                }

                return Ok(msg);
            }
            catch (Exception e)
            {
                var msg = $"Error in TestPost(MemberAndRecordData Data)";
                _logger.Error<MemberTesterApiController>(e, msg);
                return InternalServerError(e);
            }
        }

        // /Umbraco/Api/MemberTesterApi/DoPost
        [HttpPost]
        [Route("DoPost")]
        public IHttpActionResult DoPost(MemberAndRecordData Data)
        {
            var memberService = Current.Services.MemberService;

            try
            {
                //Split fullname into F & N fields
                var fName = "";
                var lName = "";
                var names = Data.NewMemberName.Split(' ').ToList();
                if (names.Count == 1)
                {
                    fName = names[0];
                }
                if (names.Count == 2)
                {
                    fName = names[0];
                    lName = names[1];
                }
                else
                {
                    var lastIndex = names.Count - 1;
                    lName = names[lastIndex];

                    names.RemoveAt(lastIndex);
                    fName = string.Join(" ", names);
                }
                var newMember = memberService.GetById(Data.NewMemberId);
                newMember.SetValue("FirstName", fName);
                newMember.SetValue("LastName", lName);
                memberService.Save(newMember);


                //Info for Logging/Return 
                var testFirst = newMember.GetValue<string>("FirstName");
                var testLast = newMember.GetValue<string>("LastName");
                var msgTemplate = "Umbraco8FormsMembers/Member/Test: New Member #{MemberId} updated with First={MemberFN}; Last={MemberLN}";
                var msgConcatd = $"Umbraco8FormsMembers/Member/Test: New Member #{newMember.Id} updated with First={testFirst}; Last={testLast}";

                _logger.Info<MemberController>(msgTemplate, Data.NewMemberId, newMember.Id, testFirst, testLast);
                return Ok(msgConcatd);
            }
            catch (Exception e)
            {
                var memName = Data.NewMemberName;
                _logger.Error<MemberTesterApiController>(e, "Error in 'DoPost'. Member: '{MemberName}", memName);
                return InternalServerError(e);
            }

        }
    }
}
