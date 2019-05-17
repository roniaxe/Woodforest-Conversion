using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoodForestConversion.API.Conversion.Base
{
    public interface IEntityToConvert<TTarget>
    {
        ICollection<TTarget> Convert();
    }
}
