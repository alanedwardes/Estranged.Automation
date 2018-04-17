using Estranged.Automation.Runner.Reviews;
using Estranged.Automation.Runner.Syndication;
using System.Threading.Tasks;

namespace Estranged.Automation
{
    public class RunnerManager
    {
        private readonly ReviewsRunner reviewsRunner;
        private readonly SyndicationRunner syndicationRunner;

        public RunnerManager(ReviewsRunner reviewsRunner, SyndicationRunner syndicationRunner)
        {
            this.reviewsRunner = reviewsRunner;
            this.syndicationRunner = syndicationRunner;
        }

        public async Task Run()
        {
            await reviewsRunner.GatherReviews("Estranged: Act I", 261820);
            await reviewsRunner.GatherReviews("Estranged: Act II", 582890);
            await syndicationRunner.GatherSyndication("http://feeds.feedburner.com/GamasutraNews");
        }
    }
}
