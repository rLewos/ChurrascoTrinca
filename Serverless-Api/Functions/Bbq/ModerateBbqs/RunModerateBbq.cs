using CrossCutting;
using Domain.Entities;
using Domain.Events;
using Domain.Repositories;
using Domain.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Serverless_Api
{
    public partial class RunModerateBbq
    {
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunModerateBbq(IBbqService bbqService, IPersonService personService)
        {
			_bbqService = bbqService;
			_personService = personService;
        }

        [Function(nameof(RunModerateBbq))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "churras/{id}/moderar")] HttpRequestData req, string id)
        {
            try
            {
                var moderationRequest = await req.Body<ModerateBbqRequest>();
				Bbq? bbq = await _bbqService.Moderate(id, moderationRequest.GonnaHappen, moderationRequest.TrincaWillPay);
                return await req.CreateResponse(System.Net.HttpStatusCode.OK, bbq.TakeSnapshot());
			}
			catch (Exception e)
            {
				return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
			}
        }
    }
}
