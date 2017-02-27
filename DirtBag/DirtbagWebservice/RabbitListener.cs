using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirtBagWebservice
{
    public class RabbitListener
    {
        private System.IServiceProvider provider;
        private IBus rabbitBus;
        public RabbitListener(System.IServiceProvider provider, IBus rabbitBus ) {
            this.provider = provider;
            this.rabbitBus = rabbitBus;
        }

        public async Task Subscribe(Models.RabbitAnalysisRequestMessage request ) {
            var analysisBLL = (BLL.IAnalyzePostBLL) provider.GetService( typeof( BLL.IAnalyzePostBLL ) );
            try {
                var results = await analysisBLL.AnalyzePost( request.Subreddit, request );
                await rabbitBus.PublishAsync( results );
            }
            catch(Exception ex) {
                throw new EasyNetQ.EasyNetQException( "Failed to analyze..", ex );
            }
        }
    }
}
