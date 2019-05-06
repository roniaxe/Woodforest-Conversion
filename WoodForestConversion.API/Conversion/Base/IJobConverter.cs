using System;
using System.Collections.Generic;
using Job = WoodForestConversion.Data.Job;

namespace WoodForestConversion.API.Conversion.Base
{
    public interface IJobConverter<in TSource, TTarget> : IConverter<TTarget>
    {
        void ConvertJobDetails(TSource sourceJob, TTarget targetJob);

        void ConvertJobConditions(object conditions, TTarget targetJob);
    }
}