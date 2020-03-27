using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Umbraco8FormsMembers.Models
{
    using Umbraco.Core.Models;
    using Umbraco.Forms.Core.Persistence.Dtos;

    public class MemberAndRecordData
    {
        public int NewMemberId { get; set; }
        public string NewMemberName { get; set; }
        public string NewMemberUserName { get; set; }
        public Record FormRecord { get; set; }

    }
}
