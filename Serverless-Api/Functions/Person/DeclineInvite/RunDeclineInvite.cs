using Domain;
using Eveneum;
using CrossCutting;
using Domain.Events;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using static Domain.ServiceCollectionExtensions;
using Domain.Services;

namespace Serverless_Api
{
    public partial class RunDeclineInvite
    {
        private readonly Person _user;
		private readonly IPersonService _personService;
		private readonly IBbqService _bbqService;

		public RunDeclineInvite(Person user,  IPersonService personService, IBbqService bbqService)
        {
            _user = user;
            _personService = personService;
			_bbqService = bbqService;
        }

        [Function(nameof(RunDeclineInvite))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "person/invites/{inviteId}/decline")] HttpRequestData req, string inviteId)
        {
            Person? person = null;
            try
            {
                person = await _personService.DeclineInvite(_user.Id, inviteId);
            }
            catch (Exception e)
            {
				return await req.CreateResponse(System.Net.HttpStatusCode.InternalServerError, new { Message = e.Message, Type = "Error" });
			}


            return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
        }
    }
}
