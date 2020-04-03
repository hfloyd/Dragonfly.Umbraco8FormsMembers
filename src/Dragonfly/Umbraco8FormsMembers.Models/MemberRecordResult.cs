namespace Dragonfly.Umbraco8FormsMembers.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Umbraco.Core.Models;
    using Umbraco.Forms.Core.Enums;
    using Umbraco.Forms.Core.Persistence.Dtos;

    public class MemberRecordResult
    {
        public int NewMemberId { get; set; }
        public string NewMemberName { get; set; }
        public string NewMemberUserName { get; set; }
        public Record FormRecord { get; set; }

        public WorkflowExecutionStatus WorkflowResult { get; set; }

        public IDictionary<Umbraco8FormsMembersHelper.SaveError, string> Errors { get; set; }
    }
}
