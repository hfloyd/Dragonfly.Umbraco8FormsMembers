namespace Dragonfly
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Web;
    using Dragonfly.Umbraco8FormsMembers.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Services.Implement;
    //using Umbraco.Web.Composing;
    using Umbraco.Core.Composing;
    using Umbraco.Core.Logging;
    using Umbraco.Forms.Core.Services;

    /// <summary>
    /// Helper class for MemberForms WorkflowType
    /// </summary>
    public static class Umbraco8FormsMembersHelper
    {
        public enum SaveError
        {
            EmailAlreadyRegistered,
            CreateMemberError,
            InvalidPassword,
            UnableToAddtoMemberGroup,
            CustomPropertyError
        }

        public static MemberRecordResult GetWorkflowResult()
        {
            var data = HttpContext.Current.Session["SaveAsUmbracoMemberWorkflowResult"];
            if (data != null)
            {
                var result = data as MemberRecordResult;

                return result;
            }
            else
            {
                return null;
            }
        }

        //public static string GetWorkflowId()
        //{
        //    var workflowService = Current.Factory.GetInstance(typeof(IWorkflowService)) as IWorkflowService;

        //    var x = workflowService.
        //}

        /// <summary>
        /// Checks if a Member already exists with this email
        /// </summary>
        /// <param name="email">Email to check</param>
        public static bool MemberExists(IMemberService memberService, string email)
        {
            try
            {
                return (memberService.GetByEmail(email: email) != null);
            }
            catch (Exception exception)
            {
                Current.Logger.Error<bool>(exception,
                    "Error in 'FormsMembersHelpers.MemberExists' for Email '{MemberEmail}'", email);
                throw new Exception(exception.Message);
            }
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/01escwtf%28v=vs.110%29.aspx
        /// </summary>
        public static bool EmailValidate(string email)
        {
            try
            {
                // Use IdnMapping class to convert Unicode domain names.
                email = Regex.Replace(
                    input: email,
                    pattern: @"(@)(.+)$",
                    evaluator: DomainMapper,
                    options: RegexOptions.None,
                    matchTimeout: TimeSpan.FromMilliseconds(200));

                // Return true if email is in valid e-mail format.
                return Regex.IsMatch(
                    input: email,
                    pattern:
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    options: RegexOptions.IgnoreCase,
                    matchTimeout: TimeSpan.FromMilliseconds(250));
            }
            catch (Exception exception)
            {
                Current.Logger.Error<string>(exception,
                    "Error in 'FormsMembersHelpers.EmailValidate' for Email '{MemberEmail}'", email);
                return false;
            }
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/01escwtf%28v=vs.110%29.aspx
        /// </summary>
        static string DomainMapper(Match match)
        {
            try
            {
                // IdnMapping class with default property values.
                var idn = new IdnMapping();

                var domainName = match.Groups[2].Value;
                domainName = idn.GetAscii(unicode: domainName);

                return match.Groups[1].Value + domainName;
            }
            catch (Exception exception)
            {
                Current.Logger.Error<string>(exception,
                    $"Error in 'FormsMembersHelpers.DomainMapper' for match '{match.Value}'");
                throw new Exception(exception.Message);
            }
        }

    }
}
