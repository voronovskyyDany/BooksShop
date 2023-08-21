using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public class SendGridSettings
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string EmailName { get; set; }
    }
}
