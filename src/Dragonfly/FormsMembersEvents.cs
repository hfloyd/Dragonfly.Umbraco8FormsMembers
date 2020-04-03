namespace Dragonfly
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Umbraco.Core;
    using Umbraco.Core.Composing;
    using Umbraco.Forms.Core.Data.Storage;
    using Umbraco.Forms.Core.Interfaces;
    using Umbraco.Forms.Core.Models;
    using Umbraco.Web;

    public class Startup : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<FormsMembersEvents>();
        }

    }

    public class FormsMembersEvents : IComponent
    {
        #region Implementation of IComponent

        public void Initialize()
        {
          Umbraco.Forms.Web.Controllers.UmbracoFormsController.FormValidate += UmbracoFormsController_FormValidate;
        }

        public void Terminate()
        {
        }

        #endregion


        void UmbracoFormsController_FormValidate(object sender, Umbraco.Forms.Mvc.FormValidationEventArgs e)
        {
            IWorkflowStorage workflowStorage = Current.Factory.GetInstance<IWorkflowStorage>(); 

            var workflowSaveAsUmbracoMemberId = new Guid("ad639eeb-5e7a-47cd-9555-31183171956e");

            // Get the Form workflows
            List<IWorkflow> formWorkflows = new List<IWorkflow>();
            foreach (var workflowId in e.Form.WorkflowIds)
            {
                formWorkflows.Add(workflowStorage.GetWorkflow(workflowId));
            }

            var matches = formWorkflows.Where(w => w.WorkflowTypeId == workflowSaveAsUmbracoMemberId).ToList();
            
            //Any form which uses the 'SaveAsUmbracoMember' Workflow
            if (matches.Any())
            {
                var emailFieldAlias = matches.First().Settings["EmailFieldAlias"];

                //If an EmailField Alias was provided, we will validate against existing Members
                if (emailFieldAlias != "")
                {
                    var s = sender as Controller;

                    if (s != null)
                    {
                        //Get email field, check against members db
                        var memberService = Current.Services.MemberService;

                        var emailField = e.Form.AllFields.SingleOrDefault(f => f.Alias == emailFieldAlias).Id
                            .ToString();
                        var emailValue = e.Context.Request[emailField];

                        if (Umbraco8FormsMembersHelper.MemberExists(memberService, emailValue))
                        {
                            s.ModelState.AddModelError(emailField,
                                $"The email address '{emailValue}' is already registered.");
                        }

                    }
                }
            }
        }
    }
}


