using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NichanUrlParserUi
{
    class common
    {
        public static string ConvertEncoding(string src, System.Text.Encoding destEnc)
        {
            byte[] src_temp = System.Text.Encoding.ASCII.GetBytes(src);
            byte[] dest_temp = System.Text.Encoding.Convert(System.Text.Encoding.ASCII, destEnc, src_temp);
            string ret = destEnc.GetString(dest_temp);
            return ret;
        }
    }
}
