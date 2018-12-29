using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Estranged.Automation.Shared
{
    public class RateLimitingRepository : IRateLimitingRepository
    {
        private readonly IAmazonDynamoDB dynamo;

        private const string StateTableName = "EstrangedAutomationState";
        private const string ItemIdKey = "ItemId";
        private const string LimitKey = "Limit";

        public RateLimitingRepository(IAmazonDynamoDB dynamo)
        {
            this.dynamo = dynamo;
        }

        public async Task<bool> IsWithinLimit(string resourceId, int limit)
        {
            var response = await dynamo.UpdateItemAsync(new UpdateItemRequest(StateTableName, new Dictionary<string, AttributeValue>
            {
                { ItemIdKey, new AttributeValue("rate-limit-" + resourceId) }
            }, new Dictionary<string, AttributeValueUpdate>
            {
                { LimitKey, new AttributeValueUpdate { Action = "ADD", Value = new AttributeValue { N = "1" } } }
            }, ReturnValue.ALL_NEW));

            return int.Parse(response.Attributes[LimitKey].N) <= limit + 1;
        }
    }
}
