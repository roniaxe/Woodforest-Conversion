namespace WoodForestConversion.API.Conversion.Base
{
    public interface IAgentConverter<in TSource, TTarget> : IConverter<TTarget>
    {
        void ConvertAgentDetails(TSource sourceAgent, TTarget targetAgent);
    }
}