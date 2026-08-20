using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viola.Core.Pack.DataClasses
{
    internal enum CpkListStructure
    {
        None, //stub
        Old, //3 variables, no separate dir/filepath
        New //5 variables, separated dir/file path for both cpk and file
    }

}
