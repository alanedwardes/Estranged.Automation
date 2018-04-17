using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Automation.Shared
{
    public class SeenItemRepository : ISeenItemRepository
    {
        private readonly IAmazonDynamoDB dynamo;

        private const string StateTableName = "EstrangedAutomationState";
        private const string ItemIdKey = "ItemId";

        public SeenItemRepository(IAmazonDynamoDB dynamo)
        {
            this.dynamo = dynamo;
        }

        public async Task<string[]> GetSeenItems(string[] items, CancellationToken token)
        {
            BatchGetItemResponse response = await dynamo.BatchGetItemAsync(new BatchGetItemRequest
            {
                RequestItems = new Dictionary<string, KeysAndAttributes>
                {
                    {
                        StateTableName,
                        new KeysAndAttributes
                        {
                            Keys = items.Select(x => new Dictionary<string, AttributeValue> { { ItemIdKey, new AttributeValue(x.ToString()) } }).ToList()
                        }
                    }
                }
            }, token);

            return response.Responses[StateTableName].Select(x => x[ItemIdKey].S).ToArray();
        }

        public async Task SetItemSeen(string item, CancellationToken token)
        {
            await dynamo.PutItemAsync(StateTableName, new Dictionary<string, AttributeValue> { { ItemIdKey, new AttributeValue(item) } }, token);
        }
    }
}
