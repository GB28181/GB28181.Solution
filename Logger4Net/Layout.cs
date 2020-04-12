using System;
using System.Collections.Generic;
using System.Text;

namespace GB28181.Logger4Net
{
    public class Layout
    {
        public interface ILayout
        {

        }


        public class PatternLayout : ILayout
        {
            public  PatternLayout(string patternString)
            {

            }

        }


    }
}
