using OpenActive.NET.Rpde.Version1;
using System;
using System.Threading.Tasks;

namespace OpenActive.Server.NET.OpenBookingHelper
{

    public interface IRpdeFeedIncrementingUniqueChangeNumber : IRpdeFeedGenerator
    {
        Task<RpdePage> GetRpdePage(long? afterChangeNumber);
    }

    public interface IRpdeFeedModifiedTimestampAndIdLong : IRpdeFeedGenerator
    {
        Task<RpdePage> GetRpdePage(long? afterTimestamp, long? afterId);
    }

    public interface IRpdeFeedModifiedTimestampAndIdString : IRpdeFeedGenerator
    {
        Task<RpdePage> GetRpdePage(long? afterTimestamp, string afterId);
    }

    public interface IRpdeOrdersFeedIncrementingUniqueChangeNumber : IRpdeFeedGenerator
    {
        Task<RpdePage> GetOrdersRpdePage(string clientId, long? afterChangeNumber);
    }

    public interface IRpdeOrdersFeedModifiedTimestampAndIdString : IRpdeFeedGenerator
    {
        Task<RpdePage> GetOrdersRpdePage(string clientId, long? afterTimestamp, string afterId);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "<Pending>")]
    // This interface exists to provide the extension method below for all RPDE feeds
    public interface IRpdeFeedGenerator { }

    public static class RpdeOrderingStrategyRouter
    {
        /// <summary>
        /// This method provides simple routing for the RPDE generator based on the subclasses defined
        /// </summary>
        /// <param name="feedidentifier"></param>
        /// <param name="generator"></param>
        /// <param name="afterTimestamp"></param>
        /// <param name="afterId"></param>
        /// <param name="afterChangeNumber"></param>
        /// <returns></returns>
        public async static Task<RpdePage> GetRpdePage(this IRpdeFeedGenerator generator, string feedidentifier, long? afterTimestamp, string afterId, long? afterChangeNumber)
        {
            switch (generator)
            {
                case IRpdeFeedIncrementingUniqueChangeNumber changeNumberGenerator:
                    return await changeNumberGenerator.GetRpdePage(afterChangeNumber);

                case IRpdeFeedModifiedTimestampAndIdLong timestampAndIdGeneratorLong:
                    if (long.TryParse(afterId, out long afterIdLong))
                    {
                        return await timestampAndIdGeneratorLong.GetRpdePage(afterTimestamp, afterIdLong);
                    }
                    else if (string.IsNullOrWhiteSpace(afterId))
                    {
                        return await timestampAndIdGeneratorLong.GetRpdePage(afterTimestamp, null);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(afterId), "afterId must be numeric");
                    }

                case IRpdeFeedModifiedTimestampAndIdString timestampAndIdGeneratorString:
                    return await timestampAndIdGeneratorString.GetRpdePage(afterTimestamp, afterId);

                case IRpdeOrdersFeedIncrementingUniqueChangeNumber ordersFeedIncrementingUniqueChangeNumber:
                    return await ordersFeedIncrementingUniqueChangeNumber.GetOrdersRpdePage(feedidentifier, afterChangeNumber);

                case IRpdeOrdersFeedModifiedTimestampAndIdString ordersFeedModifiedTimestampAndIdString:
                    return await ordersFeedModifiedTimestampAndIdString.GetOrdersRpdePage(feedidentifier, afterTimestamp, afterId);

                default:
                    throw new InvalidCastException($"RPDEFeedGenerator for '{feedidentifier}' not recognised - check the generic template for RPDEFeedModifiedTimestampAndID uses either <string> or <long?>");
            }
        }

        public static long? ConvertStringToLongOrThrow(string argumentValue, string argumentName)
        {
            if (long.TryParse(argumentValue, out long result))
            {
                return result;
            }
            else if (!string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentOutOfRangeException($"{argumentName}", $"{argumentName} must be numeric");
            }
            else
            {
                return null;
            }
        }
    }
}
