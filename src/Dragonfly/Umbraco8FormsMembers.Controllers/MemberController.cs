namespace Dragonfly.Umbraco8FormsMembers.Controllers
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Models;
    using Umbraco.Forms.Web.Models.Backoffice;
    using Umbraco.Web.Editors;
    using Umbraco.Web.Mvc;
    using Umbraco.Web.WebApi;
    using System.ComponentModel;
    using System.Web.Mvc;
    using Dragonfly.Umbraco8FormsMembers.Models;
    using Newtonsoft.Json;
    using Umbraco.Core;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Core.Services.Implement;
    using Umbraco.Web;


    // [IsBackOffice]
    // /Umbraco/backoffice/Umbraco8FormsMembers/Member <-- UmbracoAuthorizedApiController

    [IsBackOffice]
    [PluginController("Umbraco8FormsMembers")]
    public class MemberController : UmbracoAuthorizedJsonController
    {

        public IEnumerable<PickerItem> GetAllMemberTypesWithAlias()
        {
            //var memberService = Current.Services.MemberService;
            var memberTypeService = Services.MemberTypeService;
            //var memberShipHelper = new Umbraco.Web.Security.MembershipHelper(Umbraco.Web.Composing.Current);

            var list = new List<PickerItem>();

            var memberTypes = memberTypeService.GetAll();
            foreach (var mt in memberTypes)
            {
                var p = new PickerItem
                {
                    Id = mt.Alias,
                    Value = mt.Name
                };
                list.Add(p);

            }
            return list;
        }

        public IEnumerable<PickerItem> GetAllProperties(string MemberTypeAlias)
        {
            var memberTypeService = Services.MemberTypeService;

            var list = new List<PickerItem>();
            
            var memtype = memberTypeService.Get(MemberTypeAlias);

            foreach (var prop in memtype.PropertyTypes)
            {
                var p = new PickerItem
                {
                    Id = prop.Alias,
                    Value = prop.Name
                };

                if (!list.Contains(p))
                    list.Add(p);
            }

            return list;
        }


    }
}