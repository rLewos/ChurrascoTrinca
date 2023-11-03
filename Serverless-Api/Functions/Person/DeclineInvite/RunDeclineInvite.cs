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
            var person = await _personService.GetAsync(_user.Id);

            if (person == null)
                return req.CreateResponse(System.Net.HttpStatusCode.NoContent);

            person.Apply(new InviteWasDeclined { InviteId = inviteId, PersonId = person.Id });
            await _personService.SaveAsync(person);

            //Implementar impacto da recusa do convite no churrasco caso ele já tivesse sido aceito antes
            Bbq? bbq = await _bbqService.GetAsync(inviteId);
            var @event = new InviteWasDeclined() { InviteId = inviteId, PersonId = person.Id};
            bbq.Apply(@event);
            await _bbqService.SaveAsync(bbq);

            return await req.CreateResponse(System.Net.HttpStatusCode.OK, person.TakeSnapshot());
        }
    }
}
