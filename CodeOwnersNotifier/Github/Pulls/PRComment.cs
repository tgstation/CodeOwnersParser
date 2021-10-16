using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOwnersParser.Github.Pulls
{
    class PRComment
    {
        public User user { get; set; }
        public string body { get; set; }
    }
}
