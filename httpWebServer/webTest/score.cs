using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace httpWebServer
{
    public class score
    {
        public string name;
        public string scores;
        public int value;

        public score(string a, string b, int c)
        {
            name = a;
            scores = b;
            value = c;
        }
    }
}
