using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSIApp
{
    public static class MessageErrorGenerator
    {
        static Random random;

        static MessageErrorGenerator()
        {
            random = new Random(DateTime.Now.Second);
        }


        public static byte[] ProccessMessage(byte[] original, double error, double drop)
        {
            bool do_error = (random.NextDouble() < error);
            bool do_drop = (random.NextDouble() < drop);

            if (do_drop)
                return null;

            if (do_error)
            {
                byte[] cpy = new byte[original.Length];
                original.CopyTo(cpy, 0);
                //byte[] error_arr = new byte[original.Length / 4];
                //random.NextBytes(error_arr);

                byte[] err = new byte[Math.Min(100, original.Length)];
                random.NextBytes(err);

                err.CopyTo(cpy, 0);
                return cpy;
            }

            return original;
        }


    }
}
